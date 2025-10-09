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
    public class AmazonConnector : RestConnector,IConnector
    {
        public static string Token = string.Empty;

        public async Task GetApiToken(string BaseURL,string clientId, string clientSecret, string ApplicationID, string RefereshToken,string GrantType)
        {
            RestClient client = new RestClient();
            RestRequest request = new RestRequest();
            RestResponse response = new RestResponse();
            Guid guid = Guid.NewGuid();
            try
            {
                //var authenticationString = $"{clientId}:{clientSecret}";
                //var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));

                request = new RestRequest("https://api.amazon.com/auth/o2/token", eSyncMate.Processor.Models.CommonUtils.GetRequestMethod("POST"));
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("application_id", ApplicationID);
                request.AddParameter("client_id", clientId);
                request.AddParameter("client_secret", clientSecret);
                request.AddParameter("refresh_token", RefereshToken);
                request.AddParameter("grant_type", GrantType);


                response = await client.ExecuteAsync(request);

                var tokenInfoDefinition = new
                {
                    access_token = "",
                    refresh_token = "",
                    token_type = "",
                    expires_in = "",
                };

                var tokenInfo = JsonConvert.DeserializeAnonymousType(response.Content, tokenInfoDefinition);
                AmazonConnector.Token = tokenInfo.access_token;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}