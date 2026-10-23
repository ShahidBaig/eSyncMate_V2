using eSyncMate.Processor.Connections;
using Newtonsoft.Json;
using RestSharp;

namespace eSyncMate.Processor.Models
{
    /// <summary>
    /// One call to the Shopify GraphQL Admin API (Task 00006).
    /// The connector carries BaseUrl https://{shop}.myshopify.com/admin/api/{version}/, Url graphql.json, Method POST,
    /// AuthType SHOPIFYGetToken. Shopify answers HTTP 200 even for query errors, so the body is always checked.
    /// Cost-based throttle: a THROTTLED error waits for the bucket to refill and retries; a nearly empty bucket
    /// after a successful call makes the next call wait first.
    /// </summary>
    public class ShopifyGraphQLResult<T> where T : ShopifyGraphQLResponseModel
    {
        public bool Success { get; set; }
        public T Response { get; set; }
        public string Error { get; set; } = string.Empty;
        public string RequestBody { get; set; } = string.Empty;
        public string ResponseBody { get; set; } = string.Empty;
    }

    public static class ShopifyGraphQL
    {
        private const int MaxThrottleRetries = 5;

        public static ShopifyGraphQLResult<T> Execute<T>(ConnectorDataModel connector, string query, object variables) where T : ShopifyGraphQLResponseModel
        {
            return ExecuteAsync<T>(connector, query, variables).GetAwaiter().GetResult();
        }

        // Waits with Task.Delay, so calls running side by side do not hold a thread while the bucket refills
        public static async Task<ShopifyGraphQLResult<T>> ExecuteAsync<T>(ConnectorDataModel connector, string query, object variables) where T : ShopifyGraphQLResponseModel
        {
            ShopifyGraphQLResult<T> l_Result = new ShopifyGraphQLResult<T>();

            // Own copy: Url is set per call and threads must not share it
            ConnectorDataModel l_Connector = JsonConvert.DeserializeObject<ConnectorDataModel>(JsonConvert.SerializeObject(connector));

            l_Connector.Method = "POST";
            l_Connector.BodyFormat = "JSON";
            l_Connector.Url = string.IsNullOrWhiteSpace(l_Connector.Url) ? "graphql.json" : l_Connector.Url;

            l_Result.RequestBody = JsonConvert.SerializeObject(new { query, variables });

            for (int l_Attempt = 0; l_Attempt <= MaxThrottleRetries; l_Attempt++)
            {
                RestResponse l_Response;

                try
                {
                    l_Response = await RestConnector.ExecuteWithRetry(l_Connector, l_Result.RequestBody);
                }
                catch (Exception ex)
                {
                    // token request failed, DNS / TLS / network error: the caller decides, nothing is thrown past here
                    l_Result.Error = ex.Message;
                    return l_Result;
                }

                l_Result.ResponseBody = l_Response.Content ?? string.Empty;

                if (!l_Response.IsSuccessful)
                {
                    l_Result.Error = $"HTTP {(int)l_Response.StatusCode} {l_Response.StatusCode}: {l_Response.Content ?? l_Response.ErrorMessage}";
                    return l_Result;
                }

                try
                {
                    // dates stay Shopify's text: Newtonsoft would otherwise turn "2026-10-23T10:31:00Z" into a culture-formatted string
                    l_Result.Response = JsonConvert.DeserializeObject<T>(l_Result.ResponseBody, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
                }
                catch (JsonException)
                {
                    l_Result.Response = null;
                }

                if (l_Result.Response == null)
                {
                    l_Result.Error = "Shopify returned a response that could not be read.";
                    return l_Result;
                }

                ShopifyGraphQLResponseModel.Cost l_Cost = l_Result.Response.extensions?.cost;

                if (l_Result.Response.errors != null && l_Result.Response.errors.Count > 0)
                {
                    bool l_Throttled = l_Result.Response.errors.Any(e => string.Equals(e?.extensions?.code, "THROTTLED", StringComparison.OrdinalIgnoreCase));

                    if (l_Throttled && l_Attempt < MaxThrottleRetries)
                    {
                        await Task.Delay(GetWait(l_Cost?.throttleStatus, l_Cost?.requestedQueryCost ?? 100));
                        continue;
                    }

                    l_Result.Error = string.Join("; ", l_Result.Response.errors.Select(e => e?.message));
                    return l_Result;
                }

                l_Result.Success = true;

                // Bucket nearly empty: let it refill before the caller's next call
                if (l_Cost?.throttleStatus != null)
                {
                    TimeSpan l_Wait = GetWait(l_Cost.throttleStatus, Math.Max(l_Cost.actualQueryCost, 50));

                    if (l_Wait > TimeSpan.Zero)
                        await Task.Delay(l_Wait);
                }

                return l_Result;
            }

            l_Result.Error = "Shopify kept throttling the request.";
            return l_Result;
        }

        // Time for the bucket to hold p_Needed points again; zero when it already does
        private static TimeSpan GetWait(ShopifyGraphQLResponseModel.ThrottleStatus p_Throttle, double p_Needed)
        {
            if (p_Throttle == null)
                return TimeSpan.FromSeconds(1);

            if (p_Throttle.currentlyAvailable >= p_Needed)
                return TimeSpan.Zero;

            double l_Restore = p_Throttle.restoreRate <= 0 ? 50 : p_Throttle.restoreRate;
            double l_Seconds = (p_Needed - p_Throttle.currentlyAvailable) / l_Restore;

            return TimeSpan.FromSeconds(Math.Min(Math.Max(l_Seconds, 1), 60));
        }

        // userErrors field path ["input","quantities","3","quantity"] -> 3
        public static int GetInputIndex(List<string> p_Field, string p_ArrayName)
        {
            if (p_Field == null)
                return -1;

            for (int i = 0; i < p_Field.Count - 1; i++)
            {
                if (string.Equals(p_Field[i], p_ArrayName, StringComparison.OrdinalIgnoreCase) && int.TryParse(p_Field[i + 1], out int l_Index))
                    return l_Index;
            }

            return -1;
        }
    }
}
