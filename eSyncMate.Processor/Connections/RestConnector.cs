
using eSyncMate.Processor.Models;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth;
using System;
using System.Net.Http.Headers;
using System.Text;


namespace eSyncMate.Processor.Connections
{
    public class RestConnector
    {
        public static async Task<RestResponse> Execute(ConnectorDataModel connector, string body)
        {
            try
            {
retry:
                RestClient client;
                RestRequest request;
                RestResponse response;
                RestClientOptions options = new RestClientOptions(string.IsNullOrEmpty(connector.BaseUrl) ? connector.Url : connector.BaseUrl)
                {
                    MaxTimeout = -1,
                };

                if (connector.AuthType == "Auth1" && !string.IsNullOrEmpty(connector.ConsumerKey) && !string.IsNullOrEmpty(connector.ConsumerSecret))
                {
                    OAuth1Authenticator l_Auth1 = OAuth1Authenticator.ForAccessToken(
                            consumerKey: connector.ConsumerKey,
                            consumerSecret: connector.ConsumerSecret,
                            token: connector.Token,
                            tokenSecret: connector.TokenSecret,
                            OAuthSignatureMethod.HmacSha256);
                    l_Auth1.Realm = connector.Realm;

                    options.Authenticator = l_Auth1;
                }

                client = new RestClient(options);
                request = new RestRequest(string.IsNullOrEmpty(connector.BaseUrl) ? string.Empty : connector.Url, CommonUtils.GetRequestMethod(connector.Method));

                if (connector.AuthType == "SPARSGetToken")
                {
                    SCSConnector l_SCSConnector = new SCSConnector();

                    await l_SCSConnector.GetApiToken(connector.BaseUrl, connector.ConsumerKey, connector.ConsumerSecret, "", "", "");

                    connector.Token = SCSConnector.Token;
                    request.AddQueryParameter("AccessToken", connector.Token);
                }

                if (connector.AuthType == "WALMARTGetToken")
                {
                    Guid guid = Guid.NewGuid();

                    WalmartConnector l_WalmartConnector = new WalmartConnector();

                    await l_WalmartConnector.GetApiToken(connector.BaseUrl, connector.ConsumerKey, connector.ConsumerSecret,"","","");
                    
                    //if (string.IsNullOrEmpty(WalmartConnector.Token))
                    //{
                    //    WalmartConnector l_WalmartConnector = new WalmartConnector();

                    //    await l_WalmartConnector.GetApiToken(connector.BaseUrl, connector.ConsumerKey, connector.ConsumerSecret);
                    //}

                    connector.Token = WalmartConnector.Token;
                   
                    request.AddHeader("WM_SEC.ACCESS_TOKEN", connector.Token);
                    request.AddHeader("WM_QOS.CORRELATION_ID", guid.ToString());
                    request.AddHeader("WM_SVC.NAME", "WalmartAPI");
                    request.AddHeader("Accept", "application/json");

                }

                if (connector.AuthType == "AmazonGetToken")
                {
                    AmazonConnector l_AmazonConnector = new AmazonConnector();

                    await l_AmazonConnector.GetApiToken(connector.BaseUrl, connector.ConsumerKey, connector.ConsumerSecret, connector.Realm, connector.TokenSecret, "refresh_token");
                    
                    //connector.Token = AmazonConnector.Token;
                    
                    request.AddHeader("x-amz-access-token", AmazonConnector.Token);
                }

                // Only take the OAuth path when the customer's UseNewAuthentication flag is ON;
                // otherwise legacy header auth (x-api-key / x-seller-token) applies as before.
                if (connector.AuthType == "TargetGetToken" && TargetConnector.IsNewAuthEnabled(connector.CustomerID))
                {
                    // Credentials + tokens live on the Customers OAuth columns, resolved by CustomerID (ERP id).
                    TargetConnector l_TargetConnector = new TargetConnector();

                    string l_TargetToken = await l_TargetConnector.GetAccessToken(connector.CustomerID);

                    if (!string.IsNullOrEmpty(l_TargetToken))
                    {
                        request.AddHeader("Authorization", $"Bearer {l_TargetToken}");
                    }
                }
                if (connector.AuthType == "RepaintGetToken")
                {
                    RepaintConnector l_RepaintConnector = new RepaintConnector();

                    //await l_RepaintConnector.GetApiToken(connector.BaseUrl, connector.ConsumerKey, connector.ConsumerSecret, connector.Realm, connector.TokenSecret, "refresh_token");


                    var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{connector.ConsumerKey}:{connector.ConsumerSecret}"));
                    request.AddHeader("Authorization", $"Basic {credentials}");
                }

                if (connector.Parmeters != null)
                {
                    foreach (Models.Parameter l_Parameter in connector.Parmeters)
                    {
                        request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                    }
                }

                if (connector.Headers != null)
                {
                    foreach (ConnectorHeader l_Header in connector.Headers)
                    {
                        request.AddHeader(l_Header.Name, l_Header.Value);
                    }
                }

                if (!string.IsNullOrEmpty(body))
                {
                    request.AddStringBody(body, CommonUtils.GetRequestBodyFormat(connector.BodyFormat));
                }

                response = await client.ExecuteAsync(request);

                if(response.StatusCode == System.Net.HttpStatusCode.OK && response.Content.Contains("You are not authorized to perform this action"))
                {
                    if(connector.AuthType == "SPARSGetToken")
                    {
                        SCSConnector.Token = string.Empty;
                        goto retry;
                    }
                }

                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}