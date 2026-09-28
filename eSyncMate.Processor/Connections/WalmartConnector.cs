using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators;
using RestSharp;
using eSyncMate.DB;
using Nancy;
using Microsoft.IdentityModel.Tokens;
using Intercom.Data;

namespace eSyncMate.Processor.Connections
{
    public class WalmartConnector : RestConnector,IConnector
    {
        public static string Token = string.Empty;

        public async Task GetApiToken(string BaseURL,string clientId, string clientSecret, string ApplicationID, string RefereshToken, string GrantType)
        {
            await GetAccessToken(BaseURL, clientId, clientSecret, null);
        }

        // Returns the token instead of relying on the shared static, so two Walmart customers
        // running at the same time cannot pick up each other's token. When the connector has a
        // WM_MARKET header (Walmart Canada = "ca") it is sent on the token call too - without it
        // Walmart checks the keys as US keys and rejects them. No WM_MARKET = unchanged US call.
        public async Task<string> GetAccessToken(string BaseURL, string clientId, string clientSecret, List<ConnectorHeader> headers)
        {
            RestClient client = new RestClient();
            RestRequest request = new RestRequest();
            RestResponse response = new RestResponse();
            Guid guid = Guid.NewGuid();
            try
            {
                var authenticationString = $"{clientId}:{clientSecret}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));

                request = new RestRequest(BaseURL + "token", eSyncMate.Processor.Models.CommonUtils.GetRequestMethod("POST"));
                
                request.AddHeader("Authorization", "Basic " + base64EncodedAuthenticationString);
                request.AddHeader("WM_SVC.NAME", "WalmartAPI");
                request.AddHeader("WM_QOS.CORRELATION_ID", guid.ToString());
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                ConnectorHeader marketHeader = headers?.FirstOrDefault(h => string.Equals(h?.Name?.Trim(), "WM_MARKET", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(marketHeader?.Value))
                    request.AddHeader("WM_MARKET", marketHeader.Value.Trim());

                request.AddParameter("grant_type", "client_credentials");


                //var options = new RestClientOptions("https://sandbox.walmartapis.com")
                //{
                //    MaxTimeout = -1,
                //};
                //var client = new RestClient(options);
                //var request = new RestRequest("/v3/token", Method.Post);
                //request.AddHeader("WM_QOS.CORRELATION_ID", "85906b86-8e86-47c0-a599-5209de9cc694");
                //request.AddHeader("WM_SVC.NAME", "WalmartAPI");
                //request.AddHeader("Accept", "application/json");
                //request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                //request.AddHeader("Authorization", "Basic Mjc5N2Y3NjEtMzFlZi00Y2VhLTlhMDMtM2FiOTc0YjNlNTI0OkRyWnI0VjB5NjFQQlRvOXBKT0FmcHpkR21BdkxsQjNuMXFuQ3EzYV9icWwzUmVGR3BiS09HTmFPcU5maVBPQ1JtaXItN2t5MXBEVElqVGpWTFdYanZB");
                //request.AddParameter("grant_type", "client_credentials");
                //RestResponse response = await client.ExecuteAsync(request);
                //Console.WriteLine(response.Content);




                response = await client.ExecuteAsync(request);

                var tokenInfoDefinition = new
                {
                    access_token     = "",
                    token_type = "",
                    expires_in = ""
                };

                var tokenInfo = JsonConvert.DeserializeAnonymousType(response.Content, tokenInfoDefinition);
                WalmartConnector.Token = tokenInfo.access_token;
                return tokenInfo.access_token;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}