using eSyncMate.Processor.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RouteTestApp
{
    /// <summary>
    /// Amazon order 111-6061724-6017830. The stored API-JSON carries an unescaped inch mark in the
    /// product Title, so the WHOLE document fails to parse and SCSPlaceOrderRoute can never place
    /// the order — it reports "Order payload is not readable JSON".
    ///
    /// Paste a different order's OrderData.Data into Payload below to test another one. The text
    /// goes in exactly as it is stored: it is a raw string literal, so nothing needs escaping.
    /// </summary>
    public static class OrderPayloadRepairTests
    {
        private const string Payload = """"
            {
              "BuyerInfo": {
                "BuyerEmail": "1x4r0p0zm4kkjhw@marketplace.amazon.com"
              },
              "AmazonOrderId": "111-6061724-6017830",
              "EarliestDeliveryDate": "2026-09-11T07:00:00Z",
              "EarliestShipDate": "2026-09-08T07:00:00Z",
              "SalesChannel": "Amazon.com",
              "AutomatedShippingSettings": {
                "HasAutomatedShippingSettings": true
              },
              "OrderStatus": "Unshipped",
              "NumberOfItemsShipped": 0,
              "OrderType": "StandardOrder",
              "IsPremiumOrder": false,
              "IsPrime": false,
              "FulfillmentChannel": "MFN",
              "NumberOfItemsUnshipped": 1,
              "HasRegulatedItems": false,
              "IsReplacementOrder": "false",
              "IsSoldByAB": false,
              "LatestShipDate": "2026-09-09T06:59:59Z",
              "ShipServiceLevel": "Std US D2D Dom",
              "DefaultShipFromLocationAddress": {
                "StateOrRegion": "CA",
                "AddressLine1": "400 Rogers Road",
                "PostalCode": "95363",
                "City": "Patterson",
                "CountryCode": "US",
                "Name": "L65, Patterson CA"
              },
              "IsISPU": false,
              "MarketplaceId": "ATVPDKIKX0DER",
              "LatestDeliveryDate": "2026-09-12T06:59:59Z",
              "PurchaseDate": "2026-09-08T06:17:49Z",
              "ShippingAddress": {
                "StateOrRegion": "ID",
                "PostalCode": "83404",
                "City": "Idaho Falls",
                "CountryCode": "US",
                "CompanyName": null
              },
              "IsAccessPointOrder": false,
              "PaymentMethod": "Other",
              "IsBusinessOrder": false,
              "OrderTotal": {
                "CurrencyCode": "USD",
                "Amount": "99.71"
              },
              "PaymentMethodDetails": [
                "Standard"
              ],
              "IsGlobalExpressEnabled": false,
              "LastUpdateDate": "2026-09-08T06:51:24Z",
              "ShipmentServiceLevelCategory": "Standard",
              "ReplacedOrderId": null,
              "OrderAddress": {
                "payload": {
                  "AmazonOrderId": "111-6061724-6017830",
                  "ShippingAddress": {
                    "StateOrRegion": "ID",
                    "PostalCode": "83404",
                    "City": "Idaho Falls",
                    "CountryCode": "US",
                    "Name": "Angie Clayton",
                    "AddressLine1": "5425 Wild Dunes Lane",
                    "AddressLine2": null,
                    "AddressLine3": null
                  }
                }
              },
              "OrderDetail": {
                "payload": {
                  "OrderItems": [
                    {
                      "TaxCollection": {
                        "Model": "MarketplaceFacilitator",
                        "ResponsibleParty": "Amazon Services, Inc."
                      },
                      "ProductInfo": {
                        "NumberOfItems": "1"
                      },
                      "BuyerInfo": {
                        
                      },
                      "ItemTax": {
                        "CurrencyCode": "USD",
                        "Amount": "5.64"
                      },
                      "QuantityShipped": 0,
                      "BuyerRequestedCancel": {
                        "IsBuyerRequestedCancel": "false",
                        "BuyerCancelReason": ""
                      },
                      "ItemPrice": {
                        "CurrencyCode": "USD",
                        "Amount": "94.07"
                      },
                      "ASIN": "B07QHKPNW9",
                      "SellerSKU": "B07QHKPNW9 TBL4104A",
                      "Title": "SAFAVIEH Lighting Orianna Table Lamp, 25"CeramicBlue/WhiteTBL4104A","IsGift":"false","ConditionSubtypeId":"New","IsTransparency":false,"QuantityOrdered":1,"PromotionDiscountTax":{"CurrencyCode":"USD","Amount":"0.00"},"ConditionId":"New","PromotionDiscount":{"CurrencyCode":"USD","Amount":"0.00"},"OrderItemId":"169297042303041","LineNo":1,"ItemID":"TBL4104A"}],"AmazonOrderId":"111-6061724-6017830"}}}
            """";

        /// <summary>
        /// Order 112-4821234-9412264 — the same fault on a different SAFAVIEH lamp, ingested
        /// 2026-09-09. It is not in the four the prod dry run of script 102 found on 2026-09-08,
        /// so broken rows are still being written while the fix sits undeployed.
        /// </summary>
        private const string Payload2 = """"
            {
              "BuyerInfo": {
                "BuyerEmail": "2g2ypbmgl8q43zs@marketplace.amazon.com"
              },
              "AmazonOrderId": "112-4821234-9412264",
              "EarliestDeliveryDate": "2026-09-14T07:00:00Z",
              "EarliestShipDate": "2026-09-10T07:00:00Z",
              "SalesChannel": "Amazon.com",
              "AutomatedShippingSettings": {
                "HasAutomatedShippingSettings": true
              },
              "OrderStatus": "Unshipped",
              "NumberOfItemsShipped": 0,
              "OrderType": "StandardOrder",
              "IsPremiumOrder": false,
              "IsPrime": false,
              "FulfillmentChannel": "MFN",
              "NumberOfItemsUnshipped": 1,
              "HasRegulatedItems": false,
              "IsReplacementOrder": "false",
              "IsSoldByAB": false,
              "LatestShipDate": "2026-09-11T06:59:59Z",
              "ShipServiceLevel": "Std US D2D Dom",
              "DefaultShipFromLocationAddress": {
                "StateOrRegion": "NJ",
                "AddressLine1": "1200 County Rd 523",
                "PostalCode": "08822",
                "City": "FLEMINGTON",
                "CountryCode": "US",
                "Name": "L41 Flemington, NJ"
              },
              "IsISPU": false,
              "MarketplaceId": "ATVPDKIKX0DER",
              "LatestDeliveryDate": "2026-09-15T06:59:59Z",
              "PurchaseDate": "2026-09-09T22:21:38Z",
              "ShippingAddress": {
                "StateOrRegion": "NC",
                "PostalCode": "28401-3815",
                "City": "WILMINGTON",
                "CountryCode": "US",
                "CompanyName": null
              },
              "IsAccessPointOrder": false,
              "PaymentMethod": "Other",
              "IsBusinessOrder": false,
              "OrderTotal": {
                "CurrencyCode": "USD",
                "Amount": "123.96"
              },
              "PaymentMethodDetails": [
                "Standard"
              ],
              "IsGlobalExpressEnabled": false,
              "LastUpdateDate": "2026-09-09T22:51:33Z",
              "ShipmentServiceLevelCategory": "Standard",
              "ReplacedOrderId": null,
              "OrderAddress": {
                "payload": {
                  "AmazonOrderId": "112-4821234-9412264",
                  "ShippingAddress": {
                    "StateOrRegion": "NC",
                    "PostalCode": "28401-3815",
                    "City": "WILMINGTON",
                    "CountryCode": "US",
                    "Name": "Robin Lewis",
                    "AddressLine1": "409 N 15TH ST",
                    "AddressLine2": null,
                    "AddressLine3": null
                  }
                }
              },
              "OrderDetail": {
                "payload": {
                  "OrderItems": [
                    {
                      "TaxCollection": {
                        "Model": "MarketplaceFacilitator",
                        "ResponsibleParty": "Amazon Services, Inc."
                      },
                      "ProductInfo": {
                        "NumberOfItems": "2"
                      },
                      "BuyerInfo": {

                      },
                      "ItemTax": {
                        "CurrencyCode": "USD",
                        "Amount": "8.11"
                      },
                      "QuantityShipped": 0,
                      "BuyerRequestedCancel": {
                        "IsBuyerRequestedCancel": "false",
                        "BuyerCancelReason": ""
                      },
                      "ItemPrice": {
                        "CurrencyCode": "USD",
                        "Amount": "115.85"
                      },
                      "ASIN": "B0812J9MR3",
                      "SellerSKU": "B0812J9MR3 TBL4223A-SET2",
                      "Title": "SAFAVIEH Lighting Karlen Table Lamp, 29"CeramicCream/GoldTBL4223A-SET2","IsGift":"false","ConditionSubtypeId":"New","IsTransparency":false,"QuantityOrdered":1,"PromotionDiscountTax":{"CurrencyCode":"USD","Amount":"0.00"},"ConditionId":"New","PromotionDiscount":{"CurrencyCode":"USD","Amount":"0.00"},"OrderItemId":"169392241060041","LineNo":1,"ItemID":"TBL4223A-SET2"}],"AmazonOrderId":"112-4821234-9412264"}}}
            """";

        private static readonly (string Order, string Json)[] Payloads =
        {
            ("111-6061724-6017830", Payload),
            ("112-4821234-9412264", Payload2),
        };

        public static void Run()
        {
            Console.WriteLine("=== OrderPayloadRepair — Amazon Title inch mark ===");

            foreach (var l_Payload in Payloads)
            {
                RunOne(l_Payload.Order, l_Payload.Json);
            }

            Console.WriteLine();

            const string RawTitle = "SAFAVIEH Lighting Orianna Table Lamp, 25\"CeramicBlue/WhiteTBL4104A";

            string l_Sanitised = RawTitle.Replace("\\", " ").Replace("\"", " in ").Trim();
            string l_FromRaw = JsonConvert.SerializeObject(new { Title = RawTitle, ItemID = "TBL4104A" });
            string l_FromSanitised = JsonConvert.SerializeObject(new { Title = l_Sanitised, ItemID = "TBL4104A" });

            Console.WriteLine("[4] The ingestion path (AmazonGetOrdersRoute.ProcessOrder)");
            Console.WriteLine($"    Title from Amazon      : {RawTitle}");
            Console.WriteLine($"    After the sanitise     : {l_Sanitised}");
            Console.WriteLine($"    Serialised, no sanitise: {l_FromRaw}");
            Console.WriteLine($"      -> valid JSON        : {OrderPayloadRepair.IsValid(l_FromRaw)}");
            Console.WriteLine($"    Serialised, sanitised  : {l_FromSanitised}");
            Console.WriteLine($"      -> valid JSON        : {OrderPayloadRepair.IsValid(l_FromSanitised)}");
            Console.WriteLine();

            const string Good = "{\"a\":1}";
            const string Junk = "{\"Title\":\"never closes";

            bool l_GoodOk = !OrderPayloadRepair.TryRepair(Good, out string l_GoodOut) && l_GoodOut == Good;
            bool l_JunkOk = !OrderPayloadRepair.TryRepair(Junk, out string l_JunkOut) && l_JunkOut == Junk;

            Console.WriteLine("[5] Edge cases");
            Console.WriteLine($"    Already valid  -> returns false, payload untouched : {l_GoodOk}");
            Console.WriteLine($"    Unsalvageable  -> returns false, original returned : {l_JunkOk}");
        }

        private static void RunOne(string p_Order, string p_Json)
        {
            Console.WriteLine();
            Console.WriteLine($"--- Order {p_Order} ---");
            Console.WriteLine();

            Console.WriteLine("[1] The payload as stored (OrderData.Data, Type = 'API-JSON')");
            Console.WriteLine($"    Length     : {p_Json.Length}");
            Console.WriteLine($"    IsValid    : {OrderPayloadRepair.IsValid(p_Json)}");
            Console.WriteLine($"    Parser says: {ParseError(p_Json)}");
            Console.WriteLine();

            bool l_IsRepaired = OrderPayloadRepair.TryRepair(p_Json, out string l_Repaired);

            Console.WriteLine("[2] OrderPayloadRepair.TryRepair — what the Re-Map Item IDs action runs");
            Console.WriteLine($"    Repaired   : {l_IsRepaired}");
            Console.WriteLine($"    IsValid now: {OrderPayloadRepair.IsValid(l_Repaired)}");

            if (!OrderPayloadRepair.IsValid(l_Repaired))
            {
                Console.WriteLine($"    Parser says: {ParseError(l_Repaired)}");
                Console.WriteLine();
                Console.WriteLine("    >>> NOT repaired — this order would stay in ERROR.");
            }
            else
            {
                JObject l_Json = JObject.Parse(l_Repaired);
                JToken? l_Line = l_Json["OrderDetail"]?["payload"]?["OrderItems"]?[0];

                Console.WriteLine();
                Console.WriteLine("[3] The fields the Amazon ERP map (id 6) reads out of the repaired payload");
                Console.WriteLine($"    AmazonOrderId   : {l_Json["AmazonOrderId"]}");
                Console.WriteLine($"    Title           : {l_Line?["Title"]}");
                Console.WriteLine($"    ItemID          : {l_Line?["ItemID"]}");
                Console.WriteLine($"    QuantityOrdered : {l_Line?["QuantityOrdered"]}");
                Console.WriteLine($"    ItemPrice.Amount: {l_Line?["ItemPrice"]?["Amount"]}");
                Console.WriteLine($"    LineNo          : {l_Line?["LineNo"]}");
                Console.WriteLine($"    SellerSKU       : {l_Line?["SellerSKU"]}");
            }
        }

        private static string ParseError(string p_Json)
        {
            try
            {
                JToken.Parse(p_Json);
                return "parses fine";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
