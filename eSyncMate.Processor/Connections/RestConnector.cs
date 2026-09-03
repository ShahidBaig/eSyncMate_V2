
using eSyncMate.Processor.Models;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth;
using System;
using System.Globalization;
using System.Linq;
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

        // Waits used when the partner does not say when to come back.
        private static readonly int[] BackoffSeconds = { 5, 15, 45 };

        /// <summary>
        /// Execute that retries a transient failure (429 / 5xx / timeout) instead of handing it straight
        /// back. The partner's Retry-After header wins when it sends one, otherwise the wait backs off
        /// 5s, 15s, 45s. A definite error (401/403/404, 400 with a body) returns on the first attempt --
        /// retrying it would only burn quota. Either way the last response is returned, so the caller
        /// still decides what a failure means.
        /// </summary>
        public static async Task<RestResponse> ExecuteWithRetry(ConnectorDataModel connector, string body, int p_MaxAttempts = 3, Action<int, TimeSpan, RestResponse> p_OnRetry = null)
        {
            RestResponse response = null;
            int l_Attempts = p_MaxAttempts < 1 ? 1 : p_MaxAttempts;

            for (int l_Attempt = 1; l_Attempt <= l_Attempts; l_Attempt++)
            {
                response = await Execute(connector, body);

                if (!CommonUtils.IsTransientResponse(response))
                    return response;

                if (l_Attempt == l_Attempts)
                    break;

                TimeSpan l_Wait = GetRetryDelay(response, l_Attempt);

                p_OnRetry?.Invoke(l_Attempt, l_Wait, response);

                await Task.Delay(l_Wait);
            }

            return response;
        }

        /// <summary>
        /// How long to wait before the next attempt. Mirakl and most rate-limited APIs answer a 429 with
        /// Retry-After, either as seconds or as an HTTP date; that is respected but capped at 5 minutes
        /// so a route cannot park a worker on a partner's say-so.
        /// </summary>
        private static TimeSpan GetRetryDelay(RestResponse p_Response, int p_Attempt)
        {
            string l_RetryAfter = p_Response?.Headers?
                .FirstOrDefault(h => string.Equals(h.Name, "Retry-After", StringComparison.OrdinalIgnoreCase))?
                .Value?.ToString();

            if (!string.IsNullOrWhiteSpace(l_RetryAfter))
            {
                TimeSpan l_Cap = TimeSpan.FromMinutes(5);

                if (int.TryParse(l_RetryAfter, out int l_Seconds) && l_Seconds > 0)
                    return TimeSpan.FromSeconds(l_Seconds) > l_Cap ? l_Cap : TimeSpan.FromSeconds(l_Seconds);

                if (DateTime.TryParse(l_RetryAfter, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime l_When))
                {
                    TimeSpan l_Delay = l_When - DateTime.UtcNow;

                    if (l_Delay > TimeSpan.Zero)
                        return l_Delay > l_Cap ? l_Cap : l_Delay;
                }
            }

            return TimeSpan.FromSeconds(BackoffSeconds[Math.Min(p_Attempt - 1, BackoffSeconds.Length - 1)]);
        }
    }
}