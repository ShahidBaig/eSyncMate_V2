using eSyncMate.Processor.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Shopify Admin API token (Task 00006). Two ways, tried in this order:
    ///
    /// 1. The token selected on the customer (Customers > Edit > Tokens = Customers.IntegrationTokenId ->
    ///    IntegrationTokens). The store installed the app and authorized it once (authorization code grant,
    ///    ShopifyController: api/shopify/oauth/start -> api/shopify/oauth/callback); the token it handed over is in the list.
    ///    This is the way for a store in ANOTHER organisation (a client's store). A customer can have several stores
    ///    (demo, UAT, production): the selected token and the connector BaseUrl must be the same store.
    ///
    /// 2. Client credentials grant - when the customer has no token selected; a store in OUR OWN organisation only:
    ///      POST https://{shop}.myshopify.com/admin/oauth/access_token
    ///      grant_type=client_credentials&client_id=...&client_secret=...
    ///    On a client's store Shopify answers "shop_not_permitted". The token lasts 24 hours (expires_in 86399).
    ///
    /// The token is cached per shop + client id + customer.
    /// </summary>
    public class ShopifyConnector
    {
        private class CachedToken
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime ExpiresUtc { get; set; }
        }

        // A selected token is read from the database again after this time, so a new selection / authorization done
        // on the API reaches a running RouteWorker without a restart.
        private static readonly TimeSpan LinkedTokenCacheTime = TimeSpan.FromMinutes(10);

        private static readonly ConcurrentDictionary<string, CachedToken> Tokens = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

        // baseUrl = https://{shop}.myshopify.com/admin/api/{version}/ ; the token endpoint is not versioned
        // customerId = ERP customer id of the connector (ConnectorDataModel.CustomerID)
        public static async Task<string> GetAccessToken(string baseUrl, string clientId, string clientSecret, string? customerId = null)
        {
            Uri l_BaseUri = new Uri(baseUrl);
            string l_Host = l_BaseUri.Authority;
            string l_Key = CacheKey(l_Host, clientId, customerId);

            if (Tokens.TryGetValue(l_Key, out CachedToken? l_Cached) && l_Cached.ExpiresUtc > DateTime.UtcNow.AddMinutes(1))
                return l_Cached.AccessToken;

            SemaphoreSlim l_Lock = Locks.GetOrAdd(l_Key, _ => new SemaphoreSlim(1, 1));

            await l_Lock.WaitAsync();

            try
            {
                // another thread may have fetched it while this one waited
                if (Tokens.TryGetValue(l_Key, out l_Cached) && l_Cached.ExpiresUtc > DateTime.UtcNow.AddMinutes(1))
                    return l_Cached.AccessToken;

                // 1. Token selected on the customer
                string? l_LinkedToken = await GetLinkedToken(l_BaseUri, l_Host, clientId, clientSecret, customerId, l_Key);

                if (!string.IsNullOrEmpty(l_LinkedToken))
                    return l_LinkedToken;

                // 2. Client credentials (own organisation)
                using HttpRequestMessage l_Request = new HttpRequestMessage(HttpMethod.Post, $"{l_BaseUri.Scheme}://{l_Host}/admin/oauth/access_token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "client_credentials" },
                        { "client_id", clientId ?? string.Empty },
                        { "client_secret", clientSecret ?? string.Empty }
                    })
                };

                using HttpResponseMessage l_Response = await SharedHttpClientFactory.Default.SendAsync(l_Request);
                string l_Body = await l_Response.Content.ReadAsStringAsync();

                if (!l_Response.IsSuccessStatusCode)
                {
                    string l_Hint = string.Empty;

                    if (l_Body.Contains("shop_not_permitted", StringComparison.OrdinalIgnoreCase))
                    {
                        l_Hint = IntegrationTokenStore.GetByAccount(IntegrationTokenStore.PlatformShopify, l_Host, clientId ?? string.Empty) != null
                            ? $" A token of this store is in the Tokens list but customer [{customerId}] has no token selected: select it in Customers > Edit > Tokens."
                            : $" The store is not in the app's organisation and has not authorized the app yet: a store user who may manage apps opens the app once (or <eSyncMate API>/api/shopify/oauth/start?shop={l_Host}), then the token is selected in Customers > Edit > Tokens.";
                    }

                    throw new InvalidOperationException($"Shopify token request failed for [{l_Host}]: HTTP {(int)l_Response.StatusCode} {l_Body}{l_Hint}");
                }

                JObject l_Json = JObject.Parse(l_Body);
                string l_Token = l_Json["access_token"]?.ToString() ?? string.Empty;
                int l_ExpiresIn = l_Json["expires_in"]?.Value<int>() ?? 3600;

                if (string.IsNullOrEmpty(l_Token))
                    throw new InvalidOperationException($"Shopify token response for [{l_Host}] has no access_token.");

                Tokens[l_Key] = new CachedToken { AccessToken = l_Token, ExpiresUtc = DateTime.UtcNow.AddSeconds(l_ExpiresIn) };

                return l_Token;
            }
            finally
            {
                l_Lock.Release();
            }
        }

        private static string CacheKey(string host, string? clientId, string? customerId)
        {
            return host + "|" + clientId + "|" + (customerId ?? string.Empty).Trim().ToUpperInvariant();
        }

        // The token selected on the customer; refreshed first when it is an expiring one that is about to run out.
        private static async Task<string?> GetLinkedToken(Uri baseUri, string host, string clientId, string clientSecret, string? customerId, string cacheKey)
        {
            IntegrationToken? l_Token = IntegrationTokenStore.GetForCustomer(customerId ?? string.Empty, IntegrationTokenStore.PlatformShopify);

            if (l_Token == null || string.IsNullOrEmpty(l_Token.AccessToken))
                return null;

            // A token only works on the store that issued it
            if (!string.Equals(l_Token.AccountKey, host, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Customer [{customerId}] has the token [{l_Token.Name}] selected, but the connector points at [{host}]. " +
                                                    "Select the token of that store in Customers > Edit > Tokens, or change the connector BaseUrl to the token's store.");

            if (l_Token.AccessTokenExpiryUtc.HasValue && l_Token.AccessTokenExpiryUtc.Value <= DateTime.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(l_Token.RefreshToken))
                    throw new InvalidOperationException($"The Shopify token [{l_Token.Name}] has expired and there is no refresh token. The store must authorize the app again (api/shopify/oauth/start?shop={host}).");

                l_Token = await RefreshLinkedToken(baseUri, host, clientId, clientSecret, l_Token);
            }

            DateTime l_CacheUntil = DateTime.UtcNow.Add(LinkedTokenCacheTime);

            if (l_Token.AccessTokenExpiryUtc.HasValue && l_Token.AccessTokenExpiryUtc.Value < l_CacheUntil)
                l_CacheUntil = l_Token.AccessTokenExpiryUtc.Value;

            Tokens[cacheKey] = new CachedToken { AccessToken = l_Token.AccessToken, ExpiresUtc = l_CacheUntil };

            return l_Token.AccessToken;
        }

        // grant_type=refresh_token: Shopify returns a new access token AND a new refresh token; both are saved.
        private static async Task<IntegrationToken> RefreshLinkedToken(Uri baseUri, string host, string clientId, string clientSecret, IntegrationToken token)
        {
            using HttpRequestMessage l_Request = new HttpRequestMessage(HttpMethod.Post, $"{baseUri.Scheme}://{host}/admin/oauth/access_token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", clientId ?? string.Empty },
                    { "client_secret", clientSecret ?? string.Empty },
                    { "refresh_token", token.RefreshToken }
                })
            };

            using HttpResponseMessage l_Response = await SharedHttpClientFactory.Default.SendAsync(l_Request);
            string l_Body = await l_Response.Content.ReadAsStringAsync();

            if (!l_Response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Shopify token refresh failed for [{host}]: HTTP {(int)l_Response.StatusCode} {l_Body}");

            IntegrationToken l_New = FromTokenResponse(host, token.ClientId, l_Body);

            IntegrationTokenStore.Save(l_New);

            return l_New;
        }

        /// <summary>
        /// Builds the row to store from a token endpoint answer (authorization code or refresh token grant).
        /// No expires_in = a token that does not expire.
        /// </summary>
        public static IntegrationToken FromTokenResponse(string host, string clientId, string body)
        {
            JObject l_Json = JObject.Parse(body);
            string l_Token = l_Json["access_token"]?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(l_Token))
                throw new InvalidOperationException($"Shopify token response for [{host}] has no access_token.");

            int l_ExpiresIn = l_Json["expires_in"]?.Value<int?>() ?? 0;
            int l_RefreshExpiresIn = l_Json["refresh_token_expires_in"]?.Value<int?>() ?? 0;

            return new IntegrationToken
            {
                Platform = IntegrationTokenStore.PlatformShopify,
                Name = IntegrationTokenStore.PlatformShopify + " - " + host,
                AccountKey = host,
                ClientId = clientId,
                AccessToken = l_Token,
                Scope = l_Json["scope"]?.ToString() ?? string.Empty,
                AccessTokenExpiryUtc = l_ExpiresIn > 0 ? DateTime.UtcNow.AddSeconds(l_ExpiresIn) : null,
                RefreshToken = l_Json["refresh_token"]?.ToString() ?? string.Empty,
                RefreshTokenExpiryUtc = l_RefreshExpiresIn > 0 ? DateTime.UtcNow.AddSeconds(l_RefreshExpiresIn) : null
            };
        }

        // Called on a 401 so the next call fetches a fresh token (app reinstalled, secret rotated, new authorization)
        public static void ClearToken(string baseUrl, string clientId)
        {
            string l_Prefix = new Uri(baseUrl).Authority + "|" + clientId + "|";

            foreach (string l_Key in Tokens.Keys.Where(k => k.StartsWith(l_Prefix, StringComparison.Ordinal)).ToList())
                Tokens.TryRemove(l_Key, out _);
        }
    }
}
