using eSyncMate.Processor.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Shopify Admin API token via the client credentials grant (Task 00006):
    ///   POST https://{shop}.myshopify.com/admin/oauth/access_token
    ///   grant_type=client_credentials&client_id=...&client_secret=...
    /// Works only when the app and the store are in the same Shopify organization. The token lasts
    /// 24 hours (expires_in 86399); it is cached per shop + client id and renewed a minute before expiry.
    /// </summary>
    public class ShopifyConnector
    {
        private class CachedToken
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime ExpiresUtc { get; set; }
        }

        private static readonly ConcurrentDictionary<string, CachedToken> Tokens = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

        // baseUrl = https://{shop}.myshopify.com/admin/api/{version}/ ; the token endpoint is not versioned
        public static async Task<string> GetAccessToken(string baseUrl, string clientId, string clientSecret)
        {
            Uri l_BaseUri = new Uri(baseUrl);
            string l_Host = l_BaseUri.Authority;
            string l_Key = l_Host + "|" + clientId;

            if (Tokens.TryGetValue(l_Key, out CachedToken? l_Cached) && l_Cached.ExpiresUtc > DateTime.UtcNow.AddMinutes(1))
                return l_Cached.AccessToken;

            SemaphoreSlim l_Lock = Locks.GetOrAdd(l_Key, _ => new SemaphoreSlim(1, 1));

            await l_Lock.WaitAsync();

            try
            {
                // another thread may have fetched it while this one waited
                if (Tokens.TryGetValue(l_Key, out l_Cached) && l_Cached.ExpiresUtc > DateTime.UtcNow.AddMinutes(1))
                    return l_Cached.AccessToken;

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
                    throw new InvalidOperationException($"Shopify token request failed for [{l_Host}]: HTTP {(int)l_Response.StatusCode} {l_Body}");

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

        // Called on a 401 so the next call fetches a fresh token (app reinstalled, secret rotated)
        public static void ClearToken(string baseUrl, string clientId)
        {
            Tokens.TryRemove(new Uri(baseUrl).Authority + "|" + clientId, out _);
        }
    }
}
