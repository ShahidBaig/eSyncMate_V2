using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Text;

namespace eSyncMate.Processor.Connections
{
    public class TargetConnector
    {
        /// <summary>
        /// True only when the customer exists and its UseNewAuthentication flag is ON.
        /// Used to gate the OAuth path (flag OFF → legacy header auth).
        /// </summary>
        public static bool IsNewAuthEnabled(string erpCustomerID)
        {
            if (string.IsNullOrEmpty(erpCustomerID))
            {
                return false;
            }

            Customers l_Customer = new Customers();
            l_Customer.UseConnection(eSyncMate.Processor.Models.CommonUtils.ConnectionString);

            return l_Customer.LoadOAuthByERP(erpCustomerID) && l_Customer.UseNewAuthentication;
        }

        /// <summary>
        /// Returns a valid Target access token for the given ERP customer.
        /// - Only works when the customer's UseNewAuthentication flag is ON (else returns empty → legacy auth).
        /// - Reuses the DB access token while it is not expired.
        /// - When expired/missing, refreshes via grant_type=refresh_token, saves the new token + expiry
        ///   (and rotated refresh token) back to the Customers row, then returns it.
        /// </summary>
        public async Task<string> GetAccessToken(string erpCustomerID)
        {
            if (string.IsNullOrEmpty(erpCustomerID))
            {
                throw new Exception("Target OAuth: ERP CustomerID is empty — set CustomerID on the Target REST connector.");
            }

            Customers l_Customer = new Customers();
            l_Customer.UseConnection(eSyncMate.Processor.Models.CommonUtils.ConnectionString);

            if (!l_Customer.LoadOAuthByERP(erpCustomerID))
            {
                throw new Exception($"Target OAuth: customer '{erpCustomerID}' not found.");
            }

            // 1) New authentication not enabled for this customer → caller uses legacy auth.
            if (!l_Customer.UseNewAuthentication)
            {
                return string.Empty;
            }

            // 2) DB access token still valid → reuse it.
            if (!string.IsNullOrEmpty(l_Customer.OAuthAccessToken)
                && l_Customer.OAuthAccessTokenExpiry.HasValue
                && DateTime.UtcNow < l_Customer.OAuthAccessTokenExpiry.Value)
            {
                return l_Customer.OAuthAccessToken;
            }

            // 3) Expired/missing → get a new token. Values are stored plain.
            string l_ClientId = l_Customer.OAuthClientId;
            string l_ClientSecret = l_Customer.OAuthClientSecret;
            string l_RefreshToken = l_Customer.OAuthRefreshToken;

            if (string.IsNullOrEmpty(l_RefreshToken))
            {
                throw new Exception($"Target OAuth: no refresh token stored for '{erpCustomerID}'. Run the one-time authorize first.");
            }

            RestClient client = new RestClient();
            RestRequest request = new RestRequest(l_Customer.OAuthTokenUrl, eSyncMate.Processor.Models.CommonUtils.GetRequestMethod("POST"));

            var l_BasicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{l_ClientId}:{l_ClientSecret}"));
            request.AddHeader("Authorization", $"Basic {l_BasicAuth}");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", l_RefreshToken);

            RestResponse response = await client.ExecuteAsync(request);

            // expires_in / refresh_token_expires_in are nullable so a missing OR null value in the
            // response cannot break deserialization (a non-nullable int throws on null).
            var l_TokenDefinition = new { access_token = "", refresh_token = "", token_type = "", expires_in = (int?)null, refresh_token_expires_in = (int?)null };
            var l_TokenInfo = l_TokenDefinition;

            try
            {
                l_TokenInfo = JsonConvert.DeserializeAnonymousType(response.Content ?? string.Empty, l_TokenDefinition);
            }
            catch (Exception l_Ex)
            {
                throw new Exception($"Target OAuth token refresh returned an unreadable response for '{erpCustomerID}'. Status: {response.StatusCode}, Error: {l_Ex.Message}, Response: {response.Content}");
            }

            if (l_TokenInfo == null || string.IsNullOrEmpty(l_TokenInfo.access_token))
            {
                throw new Exception($"Target OAuth token refresh failed for '{erpCustomerID}'. Status: {response.StatusCode}, Response: {response.Content}");
            }

            int l_ExpiresIn = l_TokenInfo.expires_in.GetValueOrDefault();

            if (l_ExpiresIn <= 0)
            {
                l_ExpiresIn = 3600;
            }

            DateTime l_AccessExpiry = DateTime.UtcNow.AddSeconds(l_ExpiresIn - 120); // 2-min safety buffer

            // Refresh window: only Target can extend it. When the response carries
            // refresh_token_expires_in, that is the new window; when it is missing/null/zero, keep
            // the stored expiry — never blank it out.
            int l_RefreshExpiresIn = l_TokenInfo.refresh_token_expires_in.GetValueOrDefault();

            DateTime? l_RefreshExpiry = l_RefreshExpiresIn > 0
                ? DateTime.UtcNow.AddSeconds(l_RefreshExpiresIn)
                : l_Customer.OAuthRefreshTokenExpiry;

            bool l_RefreshRotated = !string.IsNullOrEmpty(l_TokenInfo.refresh_token)
                                    && l_TokenInfo.refresh_token != l_RefreshToken;

            // 4) Save back to the Customers row (plain), rotated refresh token + new window if returned.
            if (l_RefreshRotated || l_RefreshExpiresIn > 0)
            {
                l_Customer.SaveOAuthTokens(l_Customer.Id,
                    l_RefreshRotated ? l_TokenInfo.refresh_token : l_RefreshToken,
                    l_RefreshExpiry, l_TokenInfo.access_token, l_AccessExpiry);
            }
            else
            {
                l_Customer.SaveOAuthAccessToken(l_Customer.Id, l_TokenInfo.access_token, l_AccessExpiry);
            }

            return l_TokenInfo.access_token;
        }
    }
}
