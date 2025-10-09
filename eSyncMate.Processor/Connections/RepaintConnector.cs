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
    public class RepaintConnector : RestConnector,IConnector
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
                request = new RestRequest($"{BaseURL}/auth/token", eSyncMate.Processor.Models.CommonUtils.GetRequestMethod("POST"));
                var body = new
                {
                    username = clientId,      
                    password = clientSecret  
                };

                request.AddStringBody(JsonConvert.SerializeObject(body), DataFormat.Json);

                response = await client.ExecuteAsync(request);

                var tokenInfoDefinition = new
                {
                    access_token = "",
                    refresh_token = "",
                    token_type = "",
                    expires_in = "",
                    scope= ""
                };

                var tokenInfo = JsonConvert.DeserializeAnonymousType(response.Content, tokenInfoDefinition);
                RepaintConnector.Token = tokenInfo.access_token;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}