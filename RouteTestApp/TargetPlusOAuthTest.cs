using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace RouteTestApp
{
    // ============================================================================
    //  Target Plus (Target+) OAuth 2.0 - standalone test
    //  Flow: refresh_token grant  ->  access_token  ->  call Target Seller API
    //  (No browser needed - refresh_token grant is non-interactive.)
    //
    //  RUN: from Program.cs top-level region:
    //       TargetPlusOAuthTest.Run().GetAwaiter().GetResult();
    //       return;
    //
    //  !!! SECRETS BELOW - DO NOT COMMIT this file with real values. !!!
    // ============================================================================
    public static class TargetPlusOAuthTest
    {
        // ---- OAuth (portal + Postman token dialog) ----
        const string TokenUrl     = "https://oauth.plus.iam.partnersonline.com/auth/oauth/v2/token";
        const string ClientId     = "partnerportal_oa2_5d949496fcd4b70097dfad5e_fe5b_prod_ac";
        const string ClientSecret = "HKfSoj75VvgV60ZjFLdvsoON9nb6YpNeBeEcjSU84YXDIgvh0KkA0rBWL8aZXqNc";
        const string RefreshToken = "ENT.bc64e8d46bea492f9f9a0509279e2b8e-l";

        // ---- Target Seller API test call (endpoint change as needed) ----
        const string ApiUrl = "https://api.target.com/seller_orders/v1/orders";
        const string ApiKey = "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8"; // TAR6266P x-api-key (from DownloadTargetItemsRoute)

        public static async Task Run()
        {
            Console.WriteLine("========== Target Plus OAuth 2.0 Test ==========");
            try
            {
                // 1) get a fresh access token (refresh_token grant, no browser)
                string accessToken = await GetAccessToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("\n!!! No access_token returned - token step failed. Check output above.");
                    return;
                }

                Console.WriteLine("\n--- ACCESS TOKEN (first 40 chars) ---");
                Console.WriteLine(accessToken.Substring(0, Math.Min(40, accessToken.Length)) + "...");

                // 2) call an actual Target API with Bearer + x-api-key
                await CallTargetApi(accessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n!!! EXCEPTION: " + ex.Message);
                Console.WriteLine(ex);
            }
            Console.WriteLine("\n========== Test Completed ==========");
        }

        // -------- STEP 1: refresh_token -> access_token --------
        static async Task<string> GetAccessToken()
        {
            using var http = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);

            // Client ID/Secret as HTTP Basic header (config supports client_secret_basic)
            string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["refresh_token"] = RefreshToken
            });

            var response = await http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"\n[Token endpoint] {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine("[Token response]\n" + body);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            JObject json = JObject.Parse(body);

            // rotating check: did Target return a NEW refresh token?
            string newRefresh = json.Value<string>("refresh_token");
            if (!string.IsNullOrEmpty(newRefresh))
            {
                Console.WriteLine("\n>>> refresh_token IS present in the response:");
                Console.WriteLine(newRefresh == RefreshToken
                    ? ">>> SAME as before  => FIXED (non-rotating). Safe to fetch every call."
                    : ">>> DIFFERENT       => ROTATING. Must save the new refresh_token each time.");
            }
            else
            {
                Console.WriteLine("\n>>> No refresh_token in refresh response (old one stays valid).");
            }

            int expiresIn = json.Value<int?>("expires_in") ?? 0;
            Console.WriteLine($">>> access_token expires_in = {expiresIn} sec (~{expiresIn / 3600.0:0.0} hours)");

            return json.Value<string>("access_token");
        }

        // -------- STEP 2: call Target API with Bearer + x-api-key --------
        static async Task CallTargetApi(string accessToken)
        {
            using var http = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-api-key", ApiKey);

            var response = await http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"\n--- TARGET API CALL ---");
            Console.WriteLine($"GET {ApiUrl}");
            Console.WriteLine($"[Status] {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine("[Response]\n" + (body.Length > 2000 ? body.Substring(0, 2000) + " ...(truncated)" : body));
        }
    }
}
