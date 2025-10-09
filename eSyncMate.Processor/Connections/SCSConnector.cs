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
    public class SCSConnector : RestConnector,IConnector
    {
        public static string Token = string.Empty;

        public async Task GetApiToken(string BaseURL,string Compnay,string Key, string ApplicationID, string RefereshToken, string GrantType)
        {
            RestClient client = new RestClient();
            RestRequest request = new RestRequest();
            RestResponse response = new RestResponse();
            try
            {
                request = new RestRequest(BaseURL + "Get_Token?key="+ Key + "&Company="+ Compnay, eSyncMate.Processor.Models.CommonUtils.GetRequestMethod("GET"));

                response = await client.ExecuteAsync(request);

                var tokenInfoDefinition = new
                {
                    Code = "",
                    Description = "",
                    Token = ""
                };

                var tokenInfo = JsonConvert.DeserializeAnonymousType(response.Content, tokenInfoDefinition);
                SCSConnector.Token = tokenInfo.Token;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}