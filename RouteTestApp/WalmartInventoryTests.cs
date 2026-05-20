using System;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace RouteTestApp
{
    /// <summary>
    /// Walmart MP_INVENTORY Feed — Code Test
    /// Step 1: Token get karo (clientId + clientSecret)
    /// Step 2: Feed submit karo (multipart/form-data)
    /// Step 3: Status check karo
    /// </summary>
    public static class WalmartInventoryTests
    {
        // ══════════════════════════════════════════════════════════════
        // TODO: Yahan apni credentials fill karo
        // ══════════════════════════════════════════════════════════════
        private const string CLIENT_ID     = "TODO_CLIENT_ID";      // Walmart API Client ID
        private const string CLIENT_SECRET = "TODO_CLIENT_SECRET";  // Walmart API Client Secret
        private const string BASE_URL      = "https://marketplace.walmartapis.com/v3/";
        private const string JSON_FILE     = @"D:\eSoftage_Projects\ESYNCMATE_Project\walmart_inventory_ACC5702D_correct.json";
        // ══════════════════════════════════════════════════════════════

        public static void Run()
        {
            Console.WriteLine(new string('═', 65));
            Console.WriteLine("  WALMART MP_INVENTORY — AUTO TOKEN + FEED SUBMIT");
            Console.WriteLine(new string('═', 65));
            Console.WriteLine($"  File      : {JSON_FILE}");
            Console.WriteLine($"  Timestamp : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            if (CLIENT_ID == "TODO_CLIENT_ID" || CLIENT_SECRET == "TODO_CLIENT_SECRET")
            {
                Console.WriteLine("  [ERROR] CLIENT_ID ya CLIENT_SECRET set nahi — pehle fill karo.");
                Console.ReadLine();
                return;
            }

            // ── Step 1: Token get karo ─────────────────────────────────
            Console.WriteLine("  ── Step 1: Getting access token...");
            string token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("  [FAIL] Token nahi mila.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine($"  Token     : {token.Substring(0, 40)}...");
            Console.WriteLine();

            // ── Step 2: JSON file read karo ────────────────────────────
            Console.WriteLine("  ── Step 2: Reading JSON file...");
            if (!System.IO.File.Exists(JSON_FILE))
            {
                Console.WriteLine($"  [ERROR] File nahi mili: {JSON_FILE}");
                Console.ReadLine();
                return;
            }
            string jsonContent = System.IO.File.ReadAllText(JSON_FILE, Encoding.UTF8);
            Console.WriteLine($"  File size : {jsonContent.Length} chars");
            Console.WriteLine($"  Preview   : {jsonContent.Substring(0, Math.Min(100, jsonContent.Length))}...");
            Console.WriteLine();

            // ── Step 3: Feed submit karo ───────────────────────────────
            Console.WriteLine("  ── Step 3: Submitting feed...");
            string feedId = SubmitFeed(token, jsonContent);
            if (string.IsNullOrEmpty(feedId))
            {
                Console.WriteLine("  [FAIL] feedId nahi mila.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine($"  feedId    : {feedId}");
            Console.WriteLine();

            // ── Step 4: Status check ───────────────────────────────────
            Console.WriteLine("  ── Step 4: Waiting 20 seconds...");
            for (int i = 20; i > 0; i--)
            {
                Console.Write($"\r  Wait: {i} sec...  ");
                Thread.Sleep(1000);
            }
            Console.WriteLine();
            Console.WriteLine();
            CheckFeedStatus(token, feedId);

            Console.WriteLine();
            Console.WriteLine(new string('═', 65));
            Console.WriteLine("  Done. Press Enter to exit.");
            Console.ReadLine();
        }

        // ── Step 1: Get OAuth Token ────────────────────────────────────
        private static string GetToken()
        {
            try
            {
                string authString = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"));

                var client  = new RestClient();
                var request = new RestRequest(BASE_URL + "token", Method.Post);

                request.AddHeader("Authorization",         "Basic " + authString);
                request.AddHeader("WM_SVC.NAME",           "Walmart Marketplace");
                request.AddHeader("WM_QOS.CORRELATION_ID", Guid.NewGuid().ToString());
                request.AddHeader("Accept",                "application/json");
                request.AddHeader("Content-Type",          "application/x-www-form-urlencoded");
                request.AddParameter("grant_type",         "client_credentials");

                var sw = Stopwatch.StartNew();
                RestResponse response = client.Execute(request);
                sw.Stop();

                Console.WriteLine($"  Token HTTP: {(int)response.StatusCode} ({sw.ElapsedMilliseconds}ms)");

                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"  [ERROR] {response.Content}");
                    return null;
                }

                var json = JObject.Parse(response.Content);
                return json["access_token"]?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Token: {ex.Message}");
                return null;
            }
        }

        // ── Step 2: Submit Feed ────────────────────────────────────────
        private static string SubmitFeed(string token, string jsonContent)
        {
            try
            {
                string url = BASE_URL + "feeds?feedType=MP_INVENTORY";

                var client  = new RestClient();
                var request = new RestRequest(url, Method.Post);

                request.AddHeader("WM_SEC.ACCESS_TOKEN",   token);
                request.AddHeader("WM_SVC.NAME",           "Walmart Marketplace");
                request.AddHeader("WM_QOS.CORRELATION_ID", Guid.NewGuid().ToString());
                request.AddHeader("Accept",                "application/json");

                byte[] fileBytes = Encoding.UTF8.GetBytes(jsonContent);
                request.AddFile("file", fileBytes, "inventory.json", "application/json");

                var sw = Stopwatch.StartNew();
                RestResponse response = client.Execute(request);
                sw.Stop();

                Console.WriteLine($"  Feed HTTP : {(int)response.StatusCode} ({sw.ElapsedMilliseconds}ms)");
                Console.WriteLine($"  Response  : {response.Content}");

                if (!response.IsSuccessful) return null;

                var json = JObject.Parse(response.Content);
                return json["feedId"]?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Submit: {ex.Message}");
                return null;
            }
        }

        // ── Step 3: Check Status ───────────────────────────────────────
        private static void CheckFeedStatus(string token, string feedId)
        {
            try
            {
                string url = BASE_URL + $"feeds/{feedId}?includeDetails=true";

                var client  = new RestClient();
                var request = new RestRequest(url, Method.Get);

                request.AddHeader("WM_SEC.ACCESS_TOKEN",   token);
                request.AddHeader("WM_SVC.NAME",           "Walmart Marketplace");
                request.AddHeader("WM_QOS.CORRELATION_ID", Guid.NewGuid().ToString());
                request.AddHeader("Accept",                "application/json");

                RestResponse response = client.Execute(request);

                var json          = JObject.Parse(response.Content);
                string feedStatus = json["feedStatus"]?.ToString();
                int    received   = json["itemsReceived"]?.Value<int>()   ?? 0;
                int    succeeded  = json["itemsSucceeded"]?.Value<int>()  ?? 0;
                int    failed     = json["itemsFailed"]?.Value<int>()     ?? 0;

                Console.WriteLine($"  feedStatus    : {feedStatus}");
                Console.WriteLine($"  itemsReceived : {received}");
                Console.WriteLine($"  itemsSucceeded: {succeeded}");
                Console.WriteLine($"  itemsFailed   : {failed}");

                if (received == 0)
                    Console.WriteLine("  ❌ itemsReceived=0 — format abhi bhi wrong");
                else if (succeeded > 0)
                    Console.WriteLine($"  ✅ {succeeded} items successfully updated!");
                else if (failed > 0)
                    Console.WriteLine($"  ⚠️  {failed} items failed — check errors below");

                // Errors
                var errors = json["ingestionErrors"]?["ingestionError"];
                if (errors != null && errors.HasValues)
                {
                    Console.WriteLine("  Ingestion Errors:");
                    foreach (var err in errors)
                        Console.WriteLine($"    [{err["code"]}] {err["description"]}");
                }

                // Item level errors
                var itemDetails = json["itemDetails"]?["itemIngestionStatus"];
                if (itemDetails != null && itemDetails.HasValues)
                {
                    Console.WriteLine("  Item Details:");
                    foreach (var item in itemDetails)
                        Console.WriteLine($"    SKU={item["sku"]} Status={item["ingestionStatus"]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Status: {ex.Message}");
            }
        }
    }
}
