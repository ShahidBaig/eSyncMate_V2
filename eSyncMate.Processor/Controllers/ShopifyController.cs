using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Helpers;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace eSyncMate.Processor.Controllers
{
    /// <summary>
    /// Shopify app authorization (Task 00006) - the authorization code grant for a store that is NOT in our own
    /// Shopify organisation (a client's store). The client credentials grant is refused there ("shop_not_permitted").
    ///
    ///   1. api/shopify/oauth/start     the app's application_url. Shopify sends the browser here when a store user opens
    ///                            or installs the app; it is sent on to the store's authorize page.
    ///   2. api/shopify/oauth/callback  the app's redirect URL. Shopify sends the browser back with a one-time code; it is
    ///                            checked (state, HMAC, shop), exchanged for the store's access token, and the token
    ///                            is saved encrypted in IntegrationTokens.
    ///
    /// The token is NOT tied to a customer when it arrives. The user selects it on the customer
    /// (Customers > Edit > Tokens -> Customers.IntegrationTokenId); ShopifyConnector then uses it for that
    /// customer's routes. One customer can so be pointed at a demo, UAT or production store.
    ///
    /// Client ID / secret are read from the Shopify connectors: the one whose BaseUrl is the store, otherwise the
    /// app all Shopify connectors share (a store nobody has set up a connector for yet).
    /// Both addresses must be in shopify.app.toml exactly as they are reached from the internet.
    /// </summary>
    [Route("api/shopify")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class ShopifyController : ControllerBase
    {
        // As in shopify.app.toml [access_scopes]
        private const string DefaultScopes = "read_orders,write_orders,read_products,read_inventory,write_inventory,read_locations," +
                                             "read_merchant_managed_fulfillment_orders,write_merchant_managed_fulfillment_orders,write_order_edits";

        private static readonly Regex ShopPattern = new Regex(@"^[a-z0-9][a-z0-9\-]*\.myshopify\.com$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

        // state (nonce) -> the store it was issued for
        private static readonly ConcurrentDictionary<string, (string Shop, DateTime ExpiresUtc)> States = new();

        private readonly ILogger<ShopifyController> _logger;
        private readonly IConfiguration _config;

        public ShopifyController(ILogger<ShopifyController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        private class AppCredentials
        {
            public string ClientId { get; set; } = string.Empty;
            public string ClientSecret { get; set; } = string.Empty;
        }

        /// <summary>
        /// Start: sends the browser to the store's authorize page.
        /// AllowAnonymous: a top-level browser navigation from the Shopify admin, there is no JWT.
        /// </summary>
        [HttpGet]
        [Route("oauth/start")]
        [Route("install")]            // first name of this endpoint, kept so old links keep working
        [AllowAnonymous]
        public IActionResult Install([FromQuery] string shop)
        {
            string l_Shop = NormalizeShop(shop);

            if (l_Shop.Length == 0)
                return Content(BuildPage(false, "The address needs ?shop=<store>.myshopify.com."), "text/html");

            AppCredentials? l_App = FindCredentials(l_Shop);

            if (l_App == null)
                return Content(BuildPage(false, "The Shopify app is not set up in eSyncMate (no Shopify connector with a Client ID and secret)."), "text/html");

            // Shopify signs the request when it comes from the admin. A link opened by hand has no hmac.
            if (Request.Query.ContainsKey("hmac") && !IsValidHmac(Request.Query, l_App.ClientSecret))
                return Content(BuildPage(false, "The request signature (hmac) is not valid."), "text/html");

            foreach (KeyValuePair<string, (string Shop, DateTime ExpiresUtc)> l_Old in States.Where(s => s.Value.ExpiresUtc < DateTime.UtcNow).ToList())
                States.TryRemove(l_Old.Key, out _);

            string l_State = Guid.NewGuid().ToString("N");
            States[l_State] = (l_Shop, DateTime.UtcNow.Add(StateLifetime));

            string l_Url = $"https://{l_Shop}/admin/oauth/authorize" +
                           "?client_id=" + Uri.EscapeDataString(l_App.ClientId) +
                           "&scope=" + Uri.EscapeDataString(_config["Shopify:Scopes"] ?? DefaultScopes) +
                           "&redirect_uri=" + Uri.EscapeDataString(BuildCallbackUri()) +
                           "&state=" + l_State;

            return Redirect(l_Url);
        }

        /// <summary>
        /// Shopify sends the browser here with code, hmac, shop, state, timestamp.
        /// AllowAnonymous: a top-level browser navigation, there is no JWT; state + HMAC are the protection.
        /// </summary>
        [HttpGet]
        [Route("oauth/callback")]
        [Route("callback")]           // redirect URL of app version 1.1.0, kept so it keeps working
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string shop, [FromQuery] string state)
        {
            string l_Shop = NormalizeShop(shop);

            if (l_Shop.Length == 0)
                return Content(BuildPage(false, "Shopify did not send a valid shop."), "text/html");

            if (string.IsNullOrEmpty(state) || !States.TryRemove(state, out (string Shop, DateTime ExpiresUtc) l_State)
                || l_State.ExpiresUtc < DateTime.UtcNow || !string.Equals(l_State.Shop, l_Shop, StringComparison.OrdinalIgnoreCase))
                return Content(BuildPage(false, "Invalid or expired state. Open the app from the store again."), "text/html");

            AppCredentials? l_App = FindCredentials(l_Shop);

            if (l_App == null)
                return Content(BuildPage(false, "The Shopify app is not set up in eSyncMate (no Shopify connector with a Client ID and secret)."), "text/html");

            if (!IsValidHmac(Request.Query, l_App.ClientSecret))
                return Content(BuildPage(false, "The request signature (hmac) is not valid."), "text/html");

            if (string.IsNullOrEmpty(code))
                return Content(BuildPage(false, "Shopify did not return an authorization code."), "text/html");

            try
            {
                IntegrationToken l_Token = await ExchangeAndSave(l_Shop, l_App, code);

                return Content(BuildPage(true, $"eSyncMate received the access token of {l_Shop} and saved it as [{l_Token.Name}]. " +
                                               $"Permissions: {l_Token.Scope}. " +
                                               "Next step in eSyncMate: Customers > Edit > Tokens - select this token for the customer. You can close this page."), "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyController.Callback] - token exchange failed for {Shop}", l_Shop);

                return Content(BuildPage(false, "The token could not be obtained: " + ex.Message), "text/html");
            }
        }

        /// <summary>
        /// Manual way (JWT): exchange a code that someone copied from the address bar, when the store's browser could
        /// not reach api/shopify/oauth/callback (for example a local development database).
        /// Body: { "shop": "devshop-we.myshopify.com", "code": "..." } or { "redirectedUrl": "https://...?code=...&shop=..." }.
        /// </summary>
        [HttpPost]
        [Route("exchangeCode")]
        public async Task<IActionResult> ExchangeCode([FromBody] ShopifyExchangeCodeModel model)
        {
            string l_Shop = model?.Shop ?? string.Empty;
            string l_Code = model?.Code ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(model?.RedirectedUrl) && Uri.TryCreate(model.RedirectedUrl.Trim(), UriKind.Absolute, out Uri? l_Uri))
            {
                Dictionary<string, Microsoft.Extensions.Primitives.StringValues> l_Query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(l_Uri.Query);

                if (l_Query.TryGetValue("shop", out Microsoft.Extensions.Primitives.StringValues l_QShop)) l_Shop = l_QShop.ToString();
                if (l_Query.TryGetValue("code", out Microsoft.Extensions.Primitives.StringValues l_QCode)) l_Code = l_QCode.ToString();
            }

            l_Shop = NormalizeShop(l_Shop);

            if (l_Shop.Length == 0 || string.IsNullOrWhiteSpace(l_Code))
                return BadRequest(new { message = "shop (<store>.myshopify.com) and code are required." });

            AppCredentials? l_App = FindCredentials(l_Shop);

            if (l_App == null)
                return BadRequest(new { message = "The Shopify app is not set up in eSyncMate (no Shopify connector with a Client ID and secret)." });

            try
            {
                IntegrationToken l_Token = await ExchangeAndSave(l_Shop, l_App, l_Code.Trim());

                return Ok(new { shop = l_Shop, saved = true, tokenId = l_Token.Id, tokenName = l_Token.Name, scope = l_Token.Scope, expiresUtc = l_Token.AccessTokenExpiryUtc });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyController.ExchangeCode] - token exchange failed for {Shop}", l_Shop);

                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Is there a stored token for the store? (JWT; the token itself is never returned)
        /// </summary>
        [HttpGet]
        [Route("status")]
        public IActionResult Status([FromQuery] string shop)
        {
            string l_Shop = NormalizeShop(shop);

            if (l_Shop.Length == 0)
                return BadRequest(new { message = "shop (<store>.myshopify.com) is required." });

            AppCredentials? l_App = FindCredentials(l_Shop);

            if (l_App == null)
                return Ok(new { shop = l_Shop, configured = false, authorized = false });

            IntegrationToken? l_Token = IntegrationTokenStore.GetByAccount(IntegrationTokenStore.PlatformShopify, l_Shop, l_App.ClientId);

            return Ok(new
            {
                shop = l_Shop,
                configured = true,
                authorized = l_Token != null,
                tokenId = l_Token?.Id,
                tokenName = l_Token?.Name,
                scope = l_Token?.Scope,
                expiresUtc = l_Token?.AccessTokenExpiryUtc,
                savedOn = l_Token?.ModifiedDate,
                installUrl = BuildBaseUri() + "api/shopify/oauth/start?shop=" + l_Shop
            });
        }

        // POST /admin/oauth/access_token with the code; the answer is saved and the cached token dropped
        private async Task<IntegrationToken> ExchangeAndSave(string shop, AppCredentials app, string code)
        {
            Dictionary<string, string> l_Form = new Dictionary<string, string>
            {
                { "client_id", app.ClientId },
                { "client_secret", app.ClientSecret },
                { "code", code }
            };

            // Shopify:ExpiringTokens = true asks for an expiring token + refresh token (required for public apps only)
            if (_config.GetValue<bool>("Shopify:ExpiringTokens"))
                l_Form["expiring"] = "1";

            using HttpRequestMessage l_Request = new HttpRequestMessage(HttpMethod.Post, $"https://{shop}/admin/oauth/access_token")
            {
                Content = new FormUrlEncodedContent(l_Form)
            };

            l_Request.Headers.Accept.ParseAdd("application/json");

            using HttpResponseMessage l_Response = await SharedHttpClientFactory.Default.SendAsync(l_Request);
            string l_Body = await l_Response.Content.ReadAsStringAsync();

            if (!l_Response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Shopify answered HTTP {(int)l_Response.StatusCode}: {(l_Body.Length > 300 ? l_Body.Substring(0, 300) : l_Body)}");

            IntegrationToken l_Token = ShopifyConnector.FromTokenResponse(shop, app.ClientId, l_Body);

            IntegrationTokenStore.Save(l_Token);
            ShopifyConnector.ClearToken($"https://{shop}/", app.ClientId);

            return l_Token;
        }

        // Client ID / secret: the Shopify connector whose BaseUrl is this store; when no connector points at the store
        // yet (the token arrives before anybody knows the customer), the app that ALL Shopify connectors share.
        private static AppCredentials? FindCredentials(string shop)
        {
            const string l_Query = @"SELECT c.Data
                                       FROM dbo.Connectors c WITH (NOLOCK)
                                            INNER JOIN dbo.ConnectorTypes t WITH (NOLOCK) ON t.Id = c.TypeId
                                      WHERE t.Name = 'Shopify'
                                      ORDER BY c.Id";

            Dictionary<string, AppCredentials> l_Apps = new Dictionary<string, AppCredentials>(StringComparer.OrdinalIgnoreCase);

            using SqlConnection l_Connection = new SqlConnection(CommonUtils.ConnectionString);
            using SqlCommand l_Command = new SqlCommand(l_Query, l_Connection);

            l_Connection.Open();

            using SqlDataReader l_Reader = l_Command.ExecuteReader();

            while (l_Reader.Read())
            {
                try
                {
                    string l_Data = EncryptionHelper.DecryptConnectorData(Convert.ToString(l_Reader[0]) ?? string.Empty, CommonUtils.EncryptionKey);

                    if (string.IsNullOrWhiteSpace(l_Data))
                        continue;

                    JObject l_Json = JObject.Parse(l_Data);
                    string l_BaseUrl = l_Json.Value<string>("BaseUrl") ?? string.Empty;
                    string l_ClientId = l_Json.Value<string>("ConsumerKey") ?? string.Empty;
                    string l_Secret = l_Json.Value<string>("ConsumerSecret") ?? string.Empty;

                    if (l_ClientId.Length == 0 || l_Secret.Length == 0)
                        continue;

                    AppCredentials l_App = new AppCredentials { ClientId = l_ClientId, ClientSecret = l_Secret };

                    if (Uri.TryCreate(l_BaseUrl, UriKind.Absolute, out Uri? l_Uri) && string.Equals(l_Uri.Host, shop, StringComparison.OrdinalIgnoreCase))
                        return l_App;

                    l_Apps[l_ClientId] = l_App;
                }
                catch (Exception)
                {
                    // a connector that cannot be read is not the one we look for
                }
            }

            // exactly one app in use = that app; several apps = we cannot know which one the store installed
            return l_Apps.Count == 1 ? l_Apps.Values.First() : null;
        }

        // HMAC-SHA256 over the other query parameters, sorted, joined with & (Shopify's rule), keyed with the secret
        private static bool IsValidHmac(IQueryCollection query, string secret)
        {
            string l_Hmac = query["hmac"].ToString();

            if (string.IsNullOrEmpty(l_Hmac) || string.IsNullOrEmpty(secret))
                return false;

            string l_Message = string.Join("&", query
                .Where(q => q.Key != "hmac" && q.Key != "signature")
                .OrderBy(q => q.Key, StringComparer.Ordinal)
                .Select(q => q.Key + "=" + q.Value.ToString()));

            using HMACSHA256 l_Sha = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            string l_Calculated = Convert.ToHexString(l_Sha.ComputeHash(Encoding.UTF8.GetBytes(l_Message))).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(l_Calculated), Encoding.UTF8.GetBytes(l_Hmac.ToLowerInvariant()));
        }

        private static string NormalizeShop(string? shop)
        {
            string l_Shop = (shop ?? string.Empty).Trim().ToLowerInvariant();

            return ShopPattern.IsMatch(l_Shop) ? l_Shop : string.Empty;
        }

        // Shopify:PublicApiBaseUrl when the API is reached under another address than this request shows (proxy)
        private string BuildBaseUri()
        {
            string l_Configured = _config["Shopify:PublicApiBaseUrl"] ?? string.Empty;

            return l_Configured.Length > 0 ? l_Configured.TrimEnd('/') + "/" : $"{Request.Scheme}://{Request.Host}/";
        }

        // Must EXACTLY match a redirect URL in shopify.app.toml
        private string BuildCallbackUri()
        {
            return BuildBaseUri() + "api/shopify/oauth/callback";
        }

        private static string BuildPage(bool success, string message)
        {
            string l_Color = success ? "#166534" : "#991b1b";
            string l_Title = success ? "Connected" : "Not connected";

            return "<!doctype html><html><head><meta charset='utf-8'><title>eSyncMate - Shopify</title></head>" +
                   "<body style='font-family:Segoe UI,Arial,sans-serif;background:#f8fafc;margin:0;padding:40px;'>" +
                   "<div style='max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;padding:28px 32px;'>" +
                   $"<h2 style='margin:0 0 12px 0;color:{l_Color};'>{l_Title}</h2>" +
                   $"<p style='margin:0;color:#334155;line-height:1.5;'>{WebUtility.HtmlEncode(message)}</p>" +
                   "</div></body></html>";
        }
    }

    public class ShopifyExchangeCodeModel
    {
        public string? Shop { get; set; }
        public string? Code { get; set; }
        public string? RedirectedUrl { get; set; }
    }
}
