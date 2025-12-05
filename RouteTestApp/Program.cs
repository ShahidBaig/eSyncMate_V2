using RestSharp;
using RouteTestApp;
using Newtonsoft.Json;
using System.Text;
using JUST;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using System.Data;
using eSyncMate.DB;
using eSyncMate.Processor.Managers;
using Nancy;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using static eSyncMate.Processor.Models.SCSGetOrderResponseModel;
using eSyncMate.Processor.Connections;
using static eSyncMate.Processor.Models.SCSInventoryFeedModel;
using static eSyncMate.Processor.Models.WalmartGetOrderResponseModel;
using System.Dynamic;
using DocumentFormat.OpenXml.Wordprocessing;
using static eSyncMate.Processor.Models.PurchaseOrderResponseModel;
using DocumentFormat.OpenXml.Spreadsheet;
using static eSyncMate.Processor.Models.MacysGetOrderResponseModel;
using Intercom.Data;
using static eSyncMate.DB.Declarations;
using Microsoft.Extensions.Logging;
using Mysqlx.Prepare;
using static eSyncMate.Processor.Models.LowesGetOrderResponseModel;

static void Main()
{
    //string l_data = "{\"results\":[{\"external_id\":\"ADR109\",\"status\":200,\"product\":{\"id\":\"60cd006234e3910cbd9ed821\",\"external_id\":\"ADR109\",\"relationship_type\":\"VAP\",\"seller_id\":\"5d949496fcd4b70097dfad5e\",\"fields\":[{\"name\":\"brands.name\",\"value\":\"Safavieh\"},{\"name\":\"107703\",\"value\":\"Rug Pad Recommended\"},{\"name\":\"107703\",\"value\":\"Vacuum Without Beater Bar\"},{\"name\":\"304048\",\"value\":\"Machine Made\"},{\"name\":\"descriptions.long_description\",\"value\":\"Transform your living space with our exquisite 10' X 10' round area rug, a harmonious blend of grey and blue hues brought to life in a timeless round shape. Crafted in Turkey using a precision power-loomed technique, this rug boasts a comfortable 3/8\\\" pile height and a durable composition of 55% Polypropylene, 40% Jute, and 5% Polyester, ensuring both softness and longevity. The intricate pattern, inspired by classic designs, adds a touch of elegance and sophistication to any room. OEKO-TEX certification guarantees that the rug is free from harmful substances, making it a safe choice for your home. Elevate your decor with this stylish, high-quality piece that seamlessly combines tradition with modernity.\"},{\"name\":\"compliance.import_designation\",\"value\":\"Imported\"},{\"name\":\"178051\",\"value\":\"Indoor\"},{\"name\":\"109632\",\"value\":\"Jute\"},{\"name\":\"107888\",\"value\":\"Medallion\"},{\"name\":\"112550\",\"value\":\"Botanical\"},{\"name\":\"304047\",\"value\":\"Cut\"},{\"name\":\"compliance.is_proposition_65\",\"value\":\"No\"},{\"name\":\"108166\",\"value\":\"Non-Toxic\"},{\"name\":\"109641\",\"value\":\"Polypropylene\"},{\"name\":\"109644\",\"value\":\"55\"},{\"name\":\"304527\",\"value\":\"Low Pile (Less Than 0.5\\\")\"},{\"name\":\"109568\",\"value\":\"Area Rugs\"},{\"name\":\"107886\",\"value\":\"All Ages\"},{\"name\":\"compliance.tax_category.tax_code\",\"value\":\"General\"},{\"name\":\"109655\",\"value\":\"Loomed\"},{\"name\":\"descriptions.bullets[1]\",\"value\":\"Area Rug Crafted with Polypropylene, Jute, Polyester\"},{\"name\":\"descriptions.bullets[10]\",\"value\":\"Sizes may vary slightly\"},{\"name\":\"descriptions.bullets[2]\",\"value\":\"Construction Method: Power Loomed\"},{\"name\":\"descriptions.bullets[3]\",\"value\":\"Pile Thickness: 3/8\\\"\"},{\"name\":\"descriptions.bullets[4]\",\"value\":\"Made in Turkey\"},{\"name\":\"descriptions.bullets[5]\",\"value\":\"For Indoor Use Only\"},{\"name\":\"descriptions.bullets[6]\",\"value\":\"Rug pad recommended; purchase separately\"},{\"name\":\"descriptions.bullets[7]\",\"value\":\"Rug Features: Pet Friendly\"},{\"name\":\"descriptions.bullets[8]\",\"value\":\"Suggested Setting: Bedroom, Guest Room, Home Office, Living Room, Study Room\"},{\"name\":\"descriptions.bullets[9]\",\"value\":\"Routine vacuuming is the most important step in maintaining the life and beauty of your rug. It is recommended that you have area rugs professionally cleaned every 1-2 years to remove deep set dirt in high traffic areas. Otherwise, attentive vacuuming will significantly extend the life of area rugs. Vacuum thoroughly at least once a week with a canister vacuum. Do not engage beater bars. Rugs placed in high traffic areas of the home, office or rugs exposed to household pets should be vacuumed more frequently. Avoid vacuuming the fringes of your rug, especially those on hand-knotted rugs. Use a broom to clean fringes.\"},{\"name\":\"108459\",\"value\":\"Traditional\"},{\"name\":\"109612\",\"value\":\"Pet Friendly\"},{\"name\":\"109612\",\"value\":\"Rug Pad Recommended\"},{\"name\":\"109635\",\"value\":\"100\"},{\"name\":\"1173243791\",\"value\":\"1511051\"},{\"name\":\"673629455\",\"value\":\"Centexbel\"},{\"name\":\"descriptions.parent_title\",\"value\":\"Adirondack ADR109 Machine Made Indoor Rug - Safavieh\"},{\"name\":\"seller_return_policy\",\"value\":\"AAA\"},{\"name\":\"1611034609\",\"value\":\"Bedroom\"},{\"name\":\"1611034609\",\"value\":\"Guest Room\"},{\"name\":\"1611034609\",\"value\":\"Home Office\"},{\"name\":\"1611034609\",\"value\":\"Living Room\"},{\"name\":\"1611034609\",\"value\":\"Study Room\"},{\"name\":\"109642\",\"value\":\"Jute\"},{\"name\":\"109645\",\"value\":\"40\"},{\"name\":\"109643\",\"value\":\"Polyester\"},{\"name\":\"109646\",\"value\":\"55\"},{\"name\":\"shipping_exclusion\",\"value\":\"Yes\"},{\"name\":\"703650169\",\"value\":\"Moderate Traffic Areas\"},{\"name\":\"variation.theme\",\"value\":\"Size & Color\"},{\"name\":\"107946\",\"value\":\"30 Day Limited Warranty\"},{\"name\":\"product_classification.item_type\",\"value\":\"Rugs\"}],\"quantities\":[{\"quantity\":0,\"distribution_center_id\":\"mikpz1\",\"last_modified\":\"2021-06-21T14:31:09.543Z\",\"last_modified_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\"}],\"tcin\":\"83789105\",\"item_type_id\":\"247928\",\"created\":\"2021-06-18T20:21:54.179Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2024-12-17T09:01:55.455Z\",\"last_modified_by\":\"SYSTEM\",\"previously_approved\":true,\"product_statuses\":[{\"id\":\"0oc66x\",\"version\":26,\"current\":true,\"latest\":true,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2024-12-17T09:00:22.713Z\",\"created_by\":\"6e039aadf66b143a5d8c70cd5e96eeca114d1aac2e3804d1c051c5208f6af6a1\",\"last_modified\":\"2024-12-17T09:01:55.455Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"c74lmv\",\"version\":25,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2024-12-11T21:00:17.344Z\",\"created_by\":\"6e039aadf66b143a5d8c70cd5e96eeca114d1aac2e3804d1c051c5208f6af6a1\",\"last_modified\":\"2024-12-11T21:01:52.826Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"cd5ekr\",\"version\":24,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2024-12-10T21:07:40.858Z\",\"created_by\":\"6e039aadf66b143a5d8c70cd5e96eeca114d1aac2e3804d1c051c5208f6af6a1\",\"last_modified\":\"2024-12-10T21:09:16.319Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"kzu2hd\",\"version\":23,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":false,\"created\":\"2024-05-09T19:29:07.643Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2024-05-09T19:30:29.853Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"p0fw8l\",\"version\":22,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2024-02-06T14:34:48.449Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2024-02-06T14:35:30.552Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"i2kzym\",\"version\":21,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2024-02-03T15:41:08.043Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2024-02-03T15:42:22.380Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"62zss9\",\"version\":19,\"current\":false,\"latest\":false,\"listing_status\":\"REJECTED\",\"errors\":[{\"category\":\"PROCESS_EXCEPTION\",\"reason\":\"A new version of this product was submitted before approval.\",\"type\":\"ITEM\",\"error_code\":768}],\"validation_status\":\"ERROR\",\"is_changed\":false,\"created\":\"2024-02-02T21:37:26.504Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2024-02-02T21:38:16.164Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"9rm8w7\",\"version\":18,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2024-02-02T20:31:55.852Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2024-02-02T20:32:51.689Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"6njznz\",\"version\":17,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2024-01-25T14:59:14.900Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2024-01-25T14:59:49.973Z\",\"last_modified_by\":\"SYSTEM\"},{\"id\":\"3rfpsl\",\"version\":16,\"current\":false,\"latest\":false,\"listing_status\":\"APPROVED\",\"errors\":[],\"validation_status\":\"VALIDATED\",\"is_changed\":true,\"created\":\"2023-12-15T15:41:22.322Z\",\"created_by\":\"d28df639fa131ecf446e66ce808067693ee8794f7cffd22daf1e8b228c4cf906\",\"last_modified\":\"2023-12-15T15:42:41.524Z\",\"last_modified_by\":\"SYSTEM\"}]}},{\"external_id\":\"ADR109K-3SQ\",\"status\":403,\"reason\":\"Exceeded limit of 300 variation children per family. This is the maximum number of published + newly pending VCs allowed in a variation\"}]}";
    //SCSProductsResponse l_SCSProductsResponse = JsonConvert.DeserializeObject<SCSProductsResponse>(l_data);

    //var filteredResults = l_SCSProductsResponse.results
    //.Where(r => r.external_id == "ADR109K-3SQ")
    //.ToList();

    //string l_ChildResponse = JsonConvert.SerializeObject(filteredResults);


    //Instantiate IConfiguration and ILogger(you need to provide your own implementations)
    IConfiguration config = new MyConfigurationImplementation();
    RouteEngine routeEngine = new RouteEngine(config);
    ////1, 4, 10,7 GECKO
    int routeId = 100;
    routeEngine.Execute(routeId);

    //MissingOrdersProcessed().GetAwaiter().GetResult();


    //IConfiguration config = new MyConfigurationImplementation(); // Your own config implementbation

    //int orderId = 162087;
    //string customerID = "TAR6266P";

    //string result = SCSPlaceOrderRoute.ExecuteSingle(config, orderId, customerID);

    //Console.WriteLine($"Result: {result}");


    //    string connectionString = "Server=192.168.0.44,7100;Database=ESYNCMATE;UID=esyncmate;PWD=eSyncMate786$$$;";
    //    //string connectionString = "Server=DESKTOP-SPQA23Q;Database=eSyncmate_Dev;UID=sa;PWD=eSoft#1122;";

    //    string sql = $@"SELECT ProductID,ReturnPolicy FROM ReturnPolicy WITH (NOLOCK) WHERE ISNULL(Processed,0) = 0";
    //    string Body = string.Empty;
    //    StringBuilder data = new StringBuilder();

    //    DBConnector conn = new DBConnector(connectionString);
    //    DataTable l_Data = new DataTable();
    //    string Response = string.Empty;
    //    string l_ReturnPolicy = "AAC";

    //    if (conn.GetData(sql, ref l_Data))
    //    {
    //        foreach (DataRow dr in l_Data.Rows)
    //        {

    //            dynamic body2 = new ExpandoObject();
    //            body2.fields = new[]
    //            {
    //                new { name = "shipping_exclusion", value = dr["ReturnPolicy"].ToString() },
    //                new { name = "seller_return_policy", value = l_ReturnPolicy.ToString() }
    //            };

    //            Body = JsonConvert.SerializeObject(body2);

    //            Response = UpdateProductlogistics(dr["ProductID"].ToString(), Body);


    //            string Updatesql = $@" UPDATE ReturnPolicy SET Processed = 1 WHERE ProductID = '{dr["ProductID"].ToString()}'";
    //            conn.Execute(Updatesql);

    //            //data.AppendLine($"{dr["ProductID"].ToString()},Completed");
    //        }


    //    //    //File.WriteAllText("ReturnPolicy.csv", data.ToString());
    //    //}
    //    //else
    //    //{
    //    //    Console.WriteLine("Data is Completed");
    //    //}




    //    //ProcessOrderForShipment("102001756024237-7261513244");

    //GetAlItems();

    //    //string[] orders = new string[] {
    //    //};

    //    //string token = GetERPToken();

    //    //foreach (string orderNumber in orders)
    //    //{
    //    //    try
    //    //    {
    //    //        SCSGetOrderResponseModel order = GetOrdersData(orderNumber);
    //    //        List<ASNResponse> asn = GetASNData(orderNumber);
    //    //        SCSASNResponse trackingInfo = GetTrackingData(orderNumber, token);
    //    //        List<string> usedTrackings = new List<string>();     
    //    //        var l_Body = new
    //    //        {
    //    //            items = new List<dynamic>()
    //    //        };

    //    //        if (order.order_status.ToUpper() == "SHIPPED")
    //    //        {
    //    //            continue;
    //    //        }


    //    //        foreach (OrderLineModel line in order.order_lines)
    //    //        {
    //    //            foreach(OrderLineStatusModel lineStatus in line.order_line_statuses)
    //    //            {
    //    //                if (lineStatus.status == "ACKNOWLEDGED_BY_SELLER")
    //    //                {
    //    //                    string lineTracking = "";

    //    //                    foreach (TrackingInfo t in trackingInfo.OutPut.TrackingInfo)
    //    //                    {
    //    //                        int? count = asn.Where(a => a.tracking_number == t.TrackingNo)?.Count();
    //    //                        if(count == null || count <= 0)
    //    //                            count = usedTrackings.Contains(t.TrackingNo) ? 1 : 0;

    //    //                        if (count == null || count <= 0)
    //    //                        {
    //    //                            lineTracking = t.TrackingNo;
    //    //                            usedTrackings.Add(t.TrackingNo);

    //    //                            break;
    //    //                        }
    //    //                    }

    //    //                    var tracking = new
    //    //                    {
    //    //                        level_of_service = asn[0].level_of_service,
    //    //                        order_line_number = line.order_line_number,
    //    //                        quantity = lineStatus.quantity,
    //    //                        shipped_date = asn[0].shipped_date,
    //    //                        shipping_method = asn[0].shipping_method,
    //    //                        tracking_number = lineTracking
    //    //                    };

    //    //                    l_Body.items.Add(tracking);
    //    //                }
    //    //            }
    //    //        }

    //    //        if(CreateShipment(orderNumber, JsonConvert.SerializeObject(l_Body)) == "")
    //    //        {
    //    //            Console.WriteLine(orderNumber);
    //    //        }

    //    //        //ResponseModel response = ASNShipmentNotificationRoute.ExecuteShipment(orderNumber);

    //    //        //if (response.Code != 200)
    //    //        //{
    //    //        //    Console.WriteLine($"Error for Order {orderNumber} - {response.Message}");
    //    //        //}

    //    //        //string res = SCSGetOrders.ExecuteSingle(config, orderNumber);
    //    //        //if(!string.IsNullOrEmpty(res))
    //    //        //{
    //    //        //    Console.WriteLine($"Error for Order {orderNumber} - {res}");
    //    //        //}

    //    //        //string res = SCSASNRoute.ExecuteSingle(config, orderNumber);

    //    //        //if(!string.IsNullOrEmpty(res))
    //    //        //{
    //    //        //    Console.WriteLine($"Error for Order {orderNumber} - {response.Message}");
    //    //        //}
    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        Console.WriteLine($"Exception for Order {orderNumber} - {ex.Message}");
    //    //    }
    //    //}

    //GetAlItems();
    //    //GetWalmartAlItems();
    //    //SCSCancelOrderResponse response = JsonConvert.DeserializeObject<SCSCancelOrderResponse>("{\r\n    \"OutPut\": {\r\n        \"Order\": {\r\n            \"Header\": {\r\n                \"OrderNo\": 122371654,\r\n                \"CustomerID\": \"TAR6266P\",\r\n                \"OrderDate\": \"2024-07-10T00:00:00\",\r\n                \"CustomerPO\": \"902001820010598-7376181416\",\r\n                \"Status\": \"In Progress\",\r\n                \"BillingFirstName\": \"TARGET PLUS\",\r\n                \"BillingLastName\": \"\",\r\n                \"BillingAddress1\": \"P.O BOX 59251\",\r\n                \"BillingAddress2\": \"\",\r\n                \"BillingCity\": \"MINNEAPOLIS\",\r\n                \"BillingState\": \"MN\",\r\n                \"BillingZipCode\": \"55459-0251\",\r\n                \"BillingCountry\": \"USA\",\r\n                \"BillingPhone1\": \"612-304-6266\",\r\n                \"BillingPhone2\": \"\",\r\n                \"BillingFax\": \"763-440-9316\",\r\n                \"BillingEmail\": \"TCOM.INVOICING@TARGET.COM\",\r\n                \"PaymentTerm\": \"NET 30 DAYS\",\r\n                \"ShippingFirstName\": \"Adina\",\r\n                \"ShippingLastName\": \"Adina\",\r\n                \"ShippingAddress1\": \"512 Highland Ave\",\r\n                \"ShippingAddress2\": \"\",\r\n                \"ShippingState\": \"FL\",\r\n                \"ShippingZipCode\": \"32801-1347\",\r\n                \"ShipViaCode\": \"FEDH\",\r\n                \"SubTotal\": 114.99,\r\n                \"ShippingCost\": 0.0,\r\n                \"TotalAmount\": 114.99,\r\n                \"Instructions\": \"Thank you for your purchase. If you ordered additional items they will arrive separately.\\n\\nThanks fo\",\r\n                \"ShippingDate\": \"2024-07-10T00:00:00\",\r\n                \"OrderTakenBy\": \"API1\",\r\n                \"ExternalID\": \"902001820010598-7376181416\"\r\n            },\r\n            \"Detail\": [\r\n                {\r\n                    \"ItemID\": \"BHS121U-6\",\r\n                    \"Line_No\": 1,\r\n                    \"OrderQty\": 1,\r\n                    \"UnitPrice\": 114.99,\r\n                    \"Discount\": 0.0,\r\n                    \"Remarks\": \"\",\r\n                    \"Status\": \"Ready To Ship\",\r\n                    \"QtyInStock\": 1,\r\n                    \"ETA_Date\": null,\r\n                    \"APIOrderLineNo\": \"1\"\r\n                }\r\n            ]\r\n        },\r\n        \"Success\": true,\r\n        \"Message\": \"SaleOrder Information Provided.\"\r\n    }\r\n}");

    Console.WriteLine("Completed");
    Console.ReadLine();
    //}

}

static void MacysOrderProcess(IConfiguration config, ILogger logger, Routes route)
{
    int userNo = 1;
    DataTable l_data = new DataTable();
    Customers l_Customer = new Customers();
    Orders l_Orders = new Orders();
    DataTable l_OrderData = new DataTable();
    MacysGetOrderResponseModel OrdersList = new MacysGetOrderResponseModel();
    RestResponse sourceResponse = new RestResponse();
    DateTime currentDateTime = DateTime.UtcNow;
    DateTime startDateTime = currentDateTime.AddHours(-500);
    //string formattedStartDate = startDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    string formattedEndDate = startDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

    try
    {
        ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
        ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

        route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

        if (l_SourceConnector == null)
        {
            logger.LogError("Source Connector is not setup properly");
            route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
            return;
        }

        if (l_DestinationConnector == null)
        {
            logger.LogError("Destination Connector is not setup properly");
            route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly", string.Empty, userNo);
            return;
        }

        if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
        {
            //l_SourceConnector.Url = $"{l_SourceConnector.BaseUrl}/api/orders?start_date={formattedEndDate}&order_state_codes=WAITING_ACCEPTANCE";
            //sourceResponse = RestConnector.Execute(l_SourceConnector, string.Empty).GetAwaiter().GetResult();

            string l_response = @"{
                  ""orders"": [
                    {
                      ""acceptance_decision_date"": null,
                      ""can_cancel"": false,
                      ""can_shop_ship"": false,
                      ""channel"": null,
                      ""commercial_id"": ""4598491643"",
                      ""created_date"": ""2025-05-01T03:41:55Z"",
                      ""currency_iso_code"": ""USD"",
                      ""customer"": {
                        ""billing_address"": null,
                        ""civility"": null,
                        ""customer_id"": ""noorsabri12@yahoo.com"",
                        ""firstname"": ""Noor"",
                        ""lastname"": ""Sabri"",
                        ""locale"": null,
                        ""shipping_address"": null
                      },
                      ""customer_debited_date"": null,
                      ""customer_directly_pays_seller"": false,
                      ""customer_notification_email"": ""rfmv5g4u9oo.mgex46ant@us.notification.mirakl.net"",
                      ""delivery_date"": {
                        ""earliest"": ""2025-05-09T03:59:59.999Z"",
                        ""latest"": ""2025-05-14T03:59:59.999Z""
                      },
                      ""fulfillment"": {
                        ""center"": {
                          ""code"": ""DEFAULT""
                        }
                      },
                      ""fully_refunded"": false,
                      ""has_customer_message"": false,
                      ""has_incident"": false,
                      ""has_invoice"": false,
                      ""last_updated_date"": ""2025-05-01T04:12:16Z"",
                      ""leadtime_to_ship"": 4,
                      ""order_additional_fields"": [
        
                      ],
                      ""order_id"": ""4598491643-A"",
                      ""order_lines"": [
                        {
                          ""can_refund"": false,
                          ""cancelations"": [
            
                          ],
                          ""category_code"": ""Lamps"",
                          ""category_label"": ""Lamps"",
                          ""commission_fee"": 19.44,
                          ""commission_rate_vat"": 0.0000,
                          ""commission_taxes"": [
                            {
                              ""amount"": 0.00,
                              ""code"": ""TAXDEFAULT"",
                              ""rate"": 0.0000
                            }
                          ],
                          ""commission_vat"": 0.00,
                          ""created_date"": ""2025-05-01T03:41:55Z"",
                          ""debited_date"": null,
                          ""description"": null,
                          ""fees"": [
            
                          ],
                          ""last_updated_date"": ""2025-05-01T04:12:16Z"",
                          ""offer_id"": 23255938,
                          ""offer_sku"": ""FLL4130A"",
                          ""offer_state_code"": ""11"",
                          ""order_line_additional_fields"": [
                            {
                              ""code"": ""registry"",
                              ""type"": ""BOOLEAN"",
                              ""value"": ""false""
                            }
                          ],
                          ""order_line_id"": ""4598491643-1"",
                          ""order_line_index"": 1,
                          ""order_line_state"": ""WAITING_ACCEPTANCE"",
                          ""order_line_state_reason_code"": null,
                          ""order_line_state_reason_label"": null,
                          ""price"": 107.99,
                          ""price_additional_info"": null,
                          ""price_amount_breakdown"": {
                            ""parts"": [
                              {
                                ""amount"": 107.99,
                                ""commissionable"": true,
                                ""debitable_from_customer"": true,
                                ""payable_to_shop"": true
                              }
                            ]
                          },
                          ""price_unit"": 107.99,
                          ""product_medias"": [
                            {
                              ""media_url"": ""/media/product/image/4ebb71cd-acf4-40fe-83d1-cc3de4c96e0f"",
                              ""mime_type"": ""JPG"",
                              ""type"": ""MEDIUM""
                            },
                            {
                              ""media_url"": ""/media/product/image/be40fe35-2ab1-4352-a441-cd1b2889f43f"",
                              ""mime_type"": ""JPG"",
                              ""type"": ""LARGE""
                            },
                            {
                              ""media_url"": ""/media/product/image/e6de1373-90bb-4e70-8ee4-f09b7ed50d7c"",
                              ""mime_type"": ""JPG"",
                              ""type"": ""SMALL""
                            }
                          ],
                          ""product_shop_sku"": ""FLL4130A"",
                          ""product_sku"": ""195058672221_19011291_12"",
                          ""product_title"": ""Thera Floor Lamp"",
                          ""promotions"": [
            
                          ],
                          ""quantity"": 1,
                          ""received_date"": null,
                          ""refunds"": [
            
                          ],
                          ""shipped_date"": null,
                          ""shipping_from"": {
                            ""address"": {
                              ""city"": null,
                              ""country_iso_code"": ""USA"",
                              ""state"": null,
                              ""street_1"": null,
                              ""street_2"": null,
                              ""zip_code"": null
                            },
                            ""warehouse"": null
                          },
                          ""shipping_price"": 0.00,
                          ""shipping_price_additional_unit"": null,
                          ""shipping_price_amount_breakdown"": {
                            ""parts"": [
                              {
                                ""amount"": 0.00,
                                ""commissionable"": false,
                                ""debitable_from_customer"": true,
                                ""payable_to_shop"": true
                              }
                            ]
                          },
                          ""shipping_price_unit"": null,
                          ""shipping_taxes"": [
                            {
                              ""amount"": 0.00,
                              ""amount_breakdown"": {
                                ""parts"": [
                                  {
                                    ""amount"": 0.00,
                                    ""commissionable"": false,
                                    ""debitable_from_customer"": true,
                                    ""payable_to_shop"": false
                                  }
                                ]
                              },
                              ""code"": ""SHIPPING-TAX-OPERATOR"",
                              ""tax_calculation_rule"": ""PROPORTIONAL_TO_AMOUNT""
                            }
                          ],
                          ""taxes"": [
                            {
                              ""amount"": 6.48,
                              ""amount_breakdown"": {
                                ""parts"": [
                                  {
                                    ""amount"": 6.48,
                                    ""commissionable"": false,
                                    ""debitable_from_customer"": true,
                                    ""payable_to_shop"": false
                                  }
                                ]
                              },
                              ""code"": ""SALES-TAX-OPERATOR"",
                              ""tax_calculation_rule"": ""PROPORTIONAL_TO_AMOUNT""
                            }
                          ],
                          ""total_commission"": 19.44,
                          ""total_price"": 107.99
                        }
                      ],
                      ""order_refunds"": null,
                      ""order_state"": ""WAITING_ACCEPTANCE"",
                      ""order_state_reason_code"": null,
                      ""order_state_reason_label"": null,
                      ""order_tax_mode"": ""TAX_EXCLUDED"",
                      ""order_taxes"": null,
                      ""paymentType"": null,
                      ""payment_type"": null,
                      ""payment_workflow"": ""PAY_ON_SHIPMENT"",
                      ""price"": 107.99,
                      ""promotions"": {
                        ""applied_promotions"": [
          
                        ],
                        ""total_deduced_amount"": 0
                      },
                      ""quote_id"": null,
                      ""shipping_carrier_code"": null,
                      ""shipping_carrier_standard_code"": null,
                      ""shipping_company"": null,
                      ""shipping_deadline"": ""2025-05-06T03:59:59.999Z"",
                      ""shipping_price"": 0.00,
                      ""shipping_pudo_id"": null,
                      ""shipping_tracking"": null,
                      ""shipping_tracking_url"": null,
                      ""shipping_type_code"": ""standard"",
                      ""shipping_type_label"": ""Standard"",
                      ""shipping_type_standard_code"": ""CU_ADD-STD"",
                      ""shipping_zone_code"": ""us"",
                      ""shipping_zone_label"": ""Continental US"",
                      ""total_commission"": 19.44,
                      ""total_price"": 107.99
                    }
                  ],
                  ""total_count"": 1
                }";


            route.SaveData("JSON-SNT", 0, l_SourceConnector.Url, userNo);

            OrdersList = JsonConvert.DeserializeObject<MacysGetOrderResponseModel>(l_response);
            route.SaveData("JSON-RVD", 0, l_response, userNo);

            if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
            {
                l_Orders.UseConnection("Server=MUHAMMADBILAL;Database=eSyncMate;UID=sa;PWD=Bilal@123;");
                l_Customer.UseConnection("Server=MUHAMMADBILAL;Database=eSyncMate;UID=sa;PWD=Bilal@123;");

                l_Customer.GetObject("ERPCustomerID", l_SourceConnector.CustomerID);

                foreach (var order in OrdersList.orders)
                {
                    l_OrderData = new DataTable();

                    if (!l_Orders.GetViewList($"OrderNumber ='{order.order_id}'", string.Empty, ref l_OrderData))
                    {
                        ProcessOrder(order, route, l_Customer, l_SourceConnector, l_DestinationConnector, userNo);
                    }
                }
            }
        }

        route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
    }
    catch (Exception ex)
    {
        route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
    }
    finally
    {
        l_data.Dispose();
        l_OrderData.Dispose();
    }
}


static async Task MissingOrdersProcessed()
{
    Orders l_Orders = new Orders();
    DataTable l_dt = new DataTable();
    string  l_OrderNumber = string.Empty;
    l_Orders.UseConnection("Server=192.168.0.44,7100;Database=ESYNCMATE;UID=esyncmate;PWD=eSyncMate786$$$;");
    l_Orders.ProcessAPIMissingOrders("LOW2221MP", ref l_dt);
    Routes route = new Routes();

    route.UseConnection("Server=192.168.0.44,7100;Database=ESYNCMATE;UID=esyncmate;PWD=eSyncMate786$$$;");

    route.Id = 79;
    if (!route.GetObject().IsSuccess)
    {
        return;
    }

    ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
    ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

    if (l_dt.Rows.Count > 0)
    {

        foreach (DataRow item in l_dt.Rows)
        {
            l_OrderNumber = Convert.ToString(item["OrderNumber"]);

            ProcessOrdersByStatus(l_OrderNumber, route, l_SourceConnector, l_DestinationConnector, 1);
        }

    }
}



static void ProcessOrdersByStatus(string OrderNumber, Routes route, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo)
{
    Customers l_Customer = new Customers();
    Orders l_Orders = new Orders();
    DataTable l_OrderData = new DataTable();
    LowesGetOrderResponseModel ordersList = new LowesGetOrderResponseModel();
    RestResponse sourceResponse = new RestResponse();

    try
    {
        string url = $"{sourceConnector.BaseUrl}/api/orders?order_ids={OrderNumber}";
        sourceConnector.Url = url;

        sourceResponse = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();

        route.SaveData("JSON-SNT", 0, url, userNo);
        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

        ordersList = JsonConvert.DeserializeObject<LowesGetOrderResponseModel>(sourceResponse.Content);

        if (ordersList?.orders == null || ordersList.orders.Count == 0)
        {
            route.SaveLog(LogTypeEnum.Info, $"No orders found for OrderNumber: {OrderNumber}", string.Empty, userNo);
            return;
        }

        if (destinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
        {
            l_Orders.UseConnection(destinationConnector.ConnectionString);
            l_Customer.UseConnection(destinationConnector.ConnectionString);

            l_Customer.GetObject("ERPCustomerID", sourceConnector.CustomerID);

            foreach (var order in ordersList.orders)
            {
                l_OrderData = new DataTable();

                if (!l_Orders.GetViewList($"OrderNumber ='{order.order_id}'", string.Empty, ref l_OrderData))
                {
                    LowesProcessOrder(order, route, l_Customer, sourceConnector, destinationConnector, userNo);
                }
            }
        }
    }
    catch (Exception ex)
    {
        route.SaveLog(LogTypeEnum.Exception, $"Error processing orders for OrderNumber [{OrderNumber}]", ex.ToString(), userNo);
    }
    finally
    {
        l_OrderData.Dispose();
    }
}


static void LowesProcessOrder(LowesOrder order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo)
{
    RestResponse sourceResponse = new RestResponse();
    Addresses addresses = new Addresses();
    Orders l_Orders = new Orders();
    OrderData l_Data = null;
    string jsonString = string.Empty;
    LowesOrderAcceptInputModel l_LowesOrderAcceptInputModel = new LowesOrderAcceptInputModel();
    l_Orders.UseConnection(destinationConnector.ConnectionString);
    //jsonString = JsonConvert.SerializeObject(order);
    LowesGetOrderResponseModel OrdersList = new LowesGetOrderResponseModel();

    try
    {
        sourceConnector.Url = sourceConnector.BaseUrl + $"/api/orders/{order.order_id}/accept";
        sourceConnector.Method = "PUT";

        foreach (var orderLine in order.order_lines)
        {
            var l_LowesAcceptedOrder_Lines = new LowesOrderAcceptInputModel.LowesAcceptedOrder_Lines
            {
                accepted = true,
                id = orderLine.order_line_id
            };

            l_LowesOrderAcceptInputModel.order_lines.Add(l_LowesAcceptedOrder_Lines);
        }

        string Body = JsonConvert.SerializeObject(l_LowesOrderAcceptInputModel);

        route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

        l_Data = new OrderData();

        l_Data.UseConnection(string.Empty, l_Orders.Connection);
        l_Data.DeleteWithType(l_Orders.Id, "API-ACK-SNT");

        l_Data.Type = "API-ACK-SNT";
        l_Data.Data = Body;
        l_Data.CreatedBy = userNo;
        l_Data.CreatedDate = DateTime.Now;
        l_Data.OrderId = l_Orders.Id;
        l_Data.OrderNumber = order.order_id;

        l_Data.SaveNew();

        sourceResponse = RestConnector.Execute(sourceConnector, Body).GetAwaiter().GetResult();

        l_Data = new OrderData();

        l_Data.UseConnection(string.Empty, l_Orders.Connection);
        l_Data.DeleteWithType(l_Orders.Id, "API-ACK");

        l_Data.Type = "API-ACK";
        l_Data.Data = sourceResponse.Content;
        l_Data.CreatedBy = userNo;
        l_Data.CreatedDate = DateTime.Now;
        l_Data.OrderId = l_Orders.Id;
        l_Data.OrderNumber = order.order_id;

        l_Data.SaveNew();

        Thread.Sleep(500);

        sourceConnector.Url = sourceConnector.BaseUrl + $"/api/orders?order_ids={order.order_id}";
        sourceConnector.Method = "GET";

        sourceResponse = RestConnector.Execute(sourceConnector, "").GetAwaiter().GetResult();

        route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

        if (sourceResponse.Content != null)
        {
            OrdersList = JsonConvert.DeserializeObject<LowesGetOrderResponseModel>(sourceResponse.Content);
            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

            order = new LowesOrder();
            order = OrdersList.orders[0];

            jsonString = JsonConvert.SerializeObject(order);

            l_Orders.Status = order.customer.shipping_address == null ? "ERROR" : "New";
            l_Orders.CustomerId = customer.Id;
            l_Orders.OrderDate = order.created_date;
            l_Orders.OrderNumber = order.order_id;
            l_Orders.ShipToName = $"{order.customer.shipping_address?.firstname ?? ""} {order.customer.shipping_address?.lastname ?? ""}";
            l_Orders.ShipToAddress1 = order.customer.shipping_address?.street_1 ?? "";
            l_Orders.ShipToAddress2 = order.customer.shipping_address?.street_2 ?? "";
            l_Orders.ShipToCity = order.customer.shipping_address?.city ?? "";
            l_Orders.ShipToState = order.customer.shipping_address?.state ?? "";
            l_Orders.ShipToZip = order.customer.shipping_address?.zip_code ?? "";
            l_Orders.ShipToCountry = order.customer.shipping_address?.country ?? "";
            l_Orders.IsStoreOrder = false;
            l_Orders.CreatedBy = userNo;
            l_Orders.CreatedDate = DateTime.Now;
        }
        else
        {
            l_Orders.Status = "ERROR";
            l_Orders.CustomerId = customer.Id;
            l_Orders.OrderDate = order.created_date;
            l_Orders.OrderNumber = order.order_id;
            l_Orders.ShipToName = "";
            l_Orders.ShipToAddress1 = "";
            l_Orders.ShipToAddress2 = "";
            l_Orders.ShipToCity = "";
            l_Orders.ShipToState = "";
            l_Orders.ShipToZip = "";
            l_Orders.ShipToCountry = "";
            l_Orders.IsStoreOrder = false;
            l_Orders.CreatedBy = userNo;
            l_Orders.CreatedDate = DateTime.Now;
        }

        if (l_Orders.SaveNew().IsSuccess)
        {
            l_Data = new OrderData();

            l_Data.UseConnection(string.Empty, l_Orders.Connection);
            l_Data.DeleteWithType(l_Orders.Id, "API-JSON");

            l_Data.Type = "API-JSON";
            l_Data.Data = jsonString;
            l_Data.CreatedBy = userNo;
            l_Data.CreatedDate = DateTime.Now;
            l_Data.OrderId = l_Orders.Id;
            l_Data.OrderNumber = l_Orders.OrderNumber;

            l_Data.SaveNew();

            l_Data.UpdateOrderDataOrderID(l_Data.OrderNumber, l_Data.OrderId);
        }

        foreach (var orderLine in order.order_lines)
        {
            OrderDetail l_OrderDetail = new OrderDetail();

            l_OrderDetail.UseConnection(string.Empty, l_Orders.Connection);

            for (int i = 1; i <= Convert.ToInt32(orderLine.quantity); i++)
            {
                l_OrderDetail.OrderId = l_Orders.Id;
                l_OrderDetail.LineNo = Convert.ToInt32(orderLine.order_line_index);
                l_OrderDetail.LineQty = 1;
                l_OrderDetail.ItemID = orderLine.offer_sku;
                l_OrderDetail.UnitPrice = Convert.ToDecimal(orderLine.price_unit);
                l_OrderDetail.Status = "NEW";
                l_OrderDetail.order_line_id = orderLine.order_line_id;
                l_OrderDetail.CreatedBy = userNo;
                l_OrderDetail.CreatedDate = DateTime.Now;

                l_OrderDetail.SaveNew();
            }
        }

        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
        route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
    }
    catch (Exception ex)
    {
        route.SaveLog(LogTypeEnum.Error, $"Processing Error [{l_Orders.OrderNumber}]", ex.Message, userNo);
    }
}
static void ProcessOrder(MacysOrder order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo)
{
    RestResponse sourceResponse = new RestResponse();
    Addresses addresses = new Addresses();
    Orders l_Orders = new Orders();
    OrderData l_Data = null;
    string jsonString = string.Empty;
    MacysOrderAcceptInputModel l_MacysOrderAcceptInputModel = new MacysOrderAcceptInputModel();
    l_Orders.UseConnection("Server=MUHAMMADBILAL;Database=eSyncMate;UID=sa;PWD=Bilal@123;");
    //jsonString = JsonConvert.SerializeObject(order);
    MacysGetOrderResponseModel OrdersList = new MacysGetOrderResponseModel();

    try
    {
        sourceConnector.Url = sourceConnector.BaseUrl + $"/api/orders/{order.order_id}/accept";
        sourceConnector.Method = "PUT";

        foreach (var orderLine in order.order_lines)
        {
            var l_MacysAcceptedOrder_Lines = new MacysOrderAcceptInputModel.MacysAcceptedOrder_Lines
            {
                accepted = true,
                id = orderLine.order_line_id
            };

            l_MacysOrderAcceptInputModel.order_lines.Add(l_MacysAcceptedOrder_Lines);
        }

        string Body = JsonConvert.SerializeObject(l_MacysOrderAcceptInputModel);

        route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

        l_Data = new OrderData();

        l_Data.UseConnection(string.Empty, l_Orders.Connection);
        l_Data.DeleteWithType(l_Orders.Id, "API-ACK-SNT");

        l_Data.Type = "API-ACK-SNT";
        l_Data.Data = Body;
        l_Data.CreatedBy = userNo;
        l_Data.CreatedDate = DateTime.Now;
        l_Data.OrderId = l_Orders.Id;
        l_Data.OrderNumber = order.order_id;

        l_Data.SaveNew();

        //sourceResponse = RestConnector.Execute(sourceConnector, Body).GetAwaiter().GetResult();

        l_Data = new OrderData();

        l_Data.UseConnection(string.Empty, l_Orders.Connection);
        l_Data.DeleteWithType(l_Orders.Id, "API-ACK");

        l_Data.Type = "API-ACK";
        l_Data.Data = "";
        l_Data.CreatedBy = userNo;
        l_Data.CreatedDate = DateTime.Now;
        l_Data.OrderId = l_Orders.Id;
        l_Data.OrderNumber = order.order_id;

        l_Data.SaveNew();

        Thread.Sleep(500);

        //sourceConnector.Url = sourceConnector.BaseUrl + $"/api/orders?order_ids={order.order_id}";
        //sourceConnector.Method = "GET";

        //sourceResponse = RestConnector.Execute(sourceConnector, "").GetAwaiter().GetResult();


        string l_response = @"{
                  ""orders"": [
                    {
                      ""acceptance_decision_date"": null,
                      ""can_cancel"": false,
                      ""can_shop_ship"": false,
                      ""channel"": null,
                      ""commercial_id"": ""4598491643"",
                      ""created_date"": ""2025-05-01T03:41:55Z"",
                      ""currency_iso_code"": ""USD"",
                      ""customer"": {
                        ""billing_address"": null,
                        ""civility"": null,
                        ""customer_id"": ""noorsabri12@yahoo.com"",
                        ""firstname"": ""Noor"",
                        ""lastname"": ""Sabri"",
                        ""locale"": null,
                        ""shipping_address"": null
                      },
                      ""customer_debited_date"": null,
                      ""customer_directly_pays_seller"": false,
                      ""customer_notification_email"": ""rfmv5g4u9oo.mgex46ant@us.notification.mirakl.net"",
                      ""delivery_date"": {
                        ""earliest"": ""2025-05-09T03:59:59.999Z"",
                        ""latest"": ""2025-05-14T03:59:59.999Z""
                      },
                      ""fulfillment"": {
                        ""center"": {
                          ""code"": ""DEFAULT""
                        }
                      },
                      ""fully_refunded"": false,
                      ""has_customer_message"": false,
                      ""has_incident"": false,
                      ""has_invoice"": false,
                      ""last_updated_date"": ""2025-05-01T04:12:16Z"",
                      ""leadtime_to_ship"": 4,
                      ""order_additional_fields"": [
        
                      ],
                      ""order_id"": ""4598491643-A"",
                      ""order_lines"": [
                        {
                          ""can_refund"": false,
                          ""cancelations"": [
            
                          ],
                          ""category_code"": ""Lamps"",
                          ""category_label"": ""Lamps"",
                          ""commission_fee"": 19.44,
                          ""commission_rate_vat"": 0.0000,
                          ""commission_taxes"": [
                            {
                              ""amount"": 0.00,
                              ""code"": ""TAXDEFAULT"",
                              ""rate"": 0.0000
                            }
                          ],
                          ""commission_vat"": 0.00,
                          ""created_date"": ""2025-05-01T03:41:55Z"",
                          ""debited_date"": null,
                          ""description"": null,
                          ""fees"": [
            
                          ],
                          ""last_updated_date"": ""2025-05-01T04:12:16Z"",
                          ""offer_id"": 23255938,
                          ""offer_sku"": ""FLL4130A"",
                          ""offer_state_code"": ""11"",
                          ""order_line_additional_fields"": [
                            {
                              ""code"": ""registry"",
                              ""type"": ""BOOLEAN"",
                              ""value"": ""false""
                            }
                          ],
                          ""order_line_id"": ""4598491643-1"",
                          ""order_line_index"": 1,
                          ""order_line_state"": ""WAITING_ACCEPTANCE"",
                          ""order_line_state_reason_code"": null,
                          ""order_line_state_reason_label"": null,
                          ""price"": 107.99,
                          ""price_additional_info"": null,
                          ""price_amount_breakdown"": {
                            ""parts"": [
                              {
                                ""amount"": 107.99,
                                ""commissionable"": true,
                                ""debitable_from_customer"": true,
                                ""payable_to_shop"": true
                              }
                            ]
                          },
                          ""price_unit"": 107.99,
                          ""product_medias"": [
                            {
                              ""media_url"": ""/media/product/image/4ebb71cd-acf4-40fe-83d1-cc3de4c96e0f"",
                              ""mime_type"": ""JPG"",
                              ""type"": ""MEDIUM""
                            },
                            {
                              ""media_url"": ""/media/product/image/be40fe35-2ab1-4352-a441-cd1b2889f43f"",
                              ""mime_type"": ""JPG"",
                              ""type"": ""LARGE""
                            },
                            {
                              ""media_url"": ""/media/product/image/e6de1373-90bb-4e70-8ee4-f09b7ed50d7c"",
                              ""mime_type"": ""JPG"",
                              ""type"": ""SMALL""
                            }
                          ],
                          ""product_shop_sku"": ""FLL4130A"",
                          ""product_sku"": ""195058672221_19011291_12"",
                          ""product_title"": ""Thera Floor Lamp"",
                          ""promotions"": [
            
                          ],
                          ""quantity"": 1,
                          ""received_date"": null,
                          ""refunds"": [
            
                          ],
                          ""shipped_date"": null,
                          ""shipping_from"": {
                            ""address"": {
                              ""city"": null,
                              ""country_iso_code"": ""USA"",
                              ""state"": null,
                              ""street_1"": null,
                              ""street_2"": null,
                              ""zip_code"": null
                            },
                            ""warehouse"": null
                          },
                          ""shipping_price"": 0.00,
                          ""shipping_price_additional_unit"": null,
                          ""shipping_price_amount_breakdown"": {
                            ""parts"": [
                              {
                                ""amount"": 0.00,
                                ""commissionable"": false,
                                ""debitable_from_customer"": true,
                                ""payable_to_shop"": true
                              }
                            ]
                          },
                          ""shipping_price_unit"": null,
                          ""shipping_taxes"": [
                            {
                              ""amount"": 0.00,
                              ""amount_breakdown"": {
                                ""parts"": [
                                  {
                                    ""amount"": 0.00,
                                    ""commissionable"": false,
                                    ""debitable_from_customer"": true,
                                    ""payable_to_shop"": false
                                  }
                                ]
                              },
                              ""code"": ""SHIPPING-TAX-OPERATOR"",
                              ""tax_calculation_rule"": ""PROPORTIONAL_TO_AMOUNT""
                            }
                          ],
                          ""taxes"": [
                            {
                              ""amount"": 6.48,
                              ""amount_breakdown"": {
                                ""parts"": [
                                  {
                                    ""amount"": 6.48,
                                    ""commissionable"": false,
                                    ""debitable_from_customer"": true,
                                    ""payable_to_shop"": false
                                  }
                                ]
                              },
                              ""code"": ""SALES-TAX-OPERATOR"",
                              ""tax_calculation_rule"": ""PROPORTIONAL_TO_AMOUNT""
                            }
                          ],
                          ""total_commission"": 19.44,
                          ""total_price"": 107.99
                        }
                      ],
                      ""order_refunds"": null,
                      ""order_state"": ""WAITING_ACCEPTANCE"",
                      ""order_state_reason_code"": null,
                      ""order_state_reason_label"": null,
                      ""order_tax_mode"": ""TAX_EXCLUDED"",
                      ""order_taxes"": null,
                      ""paymentType"": null,
                      ""payment_type"": null,
                      ""payment_workflow"": ""PAY_ON_SHIPMENT"",
                      ""price"": 107.99,
                      ""promotions"": {
                        ""applied_promotions"": [
          
                        ],
                        ""total_deduced_amount"": 0
                      },
                      ""quote_id"": null,
                      ""shipping_carrier_code"": null,
                      ""shipping_carrier_standard_code"": null,
                      ""shipping_company"": null,
                      ""shipping_deadline"": ""2025-05-06T03:59:59.999Z"",
                      ""shipping_price"": 0.00,
                      ""shipping_pudo_id"": null,
                      ""shipping_tracking"": null,
                      ""shipping_tracking_url"": null,
                      ""shipping_type_code"": ""standard"",
                      ""shipping_type_label"": ""Standard"",
                      ""shipping_type_standard_code"": ""CU_ADD-STD"",
                      ""shipping_zone_code"": ""us"",
                      ""shipping_zone_label"": ""Continental US"",
                      ""total_commission"": 19.44,
                      ""total_price"": 107.99
                    }
                  ],
                  ""total_count"": 1
                }";

        route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

        if (l_response != null)
        {
            OrdersList = JsonConvert.DeserializeObject<MacysGetOrderResponseModel>(l_response);
            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

            order = new MacysOrder();
            order = OrdersList.orders[0];

            jsonString = JsonConvert.SerializeObject(order);

            l_Orders.Status = order.customer.shipping_address == null ? "ERROR" : "New";
            l_Orders.CustomerId = customer.Id;
            l_Orders.OrderDate = order.created_date;
            l_Orders.OrderNumber = order.order_id;
            l_Orders.ShipToName = $"{order.customer.shipping_address?.firstname ?? ""} {order.customer.shipping_address?.lastname ?? ""}";
            l_Orders.ShipToAddress1 = order.customer.shipping_address?.street_1 ?? "";
            l_Orders.ShipToAddress2 = order.customer.shipping_address?.street_2 ?? "";
            l_Orders.ShipToCity = order.customer.shipping_address?.city ?? "";
            l_Orders.ShipToState = order.customer.shipping_address?.state ?? "";
            l_Orders.ShipToZip = order.customer.shipping_address?.zip_code ?? "";
            l_Orders.ShipToCountry = order.customer.shipping_address?.country ?? "";
            l_Orders.IsStoreOrder = false;
            l_Orders.CreatedBy = userNo;
            l_Orders.CreatedDate = DateTime.Now;
        }
        else
        {
            l_Orders.Status = "ERROR";
            l_Orders.CustomerId = customer.Id;
            l_Orders.OrderDate = order.created_date;
            l_Orders.OrderNumber = order.order_id;
            l_Orders.ShipToName = "";
            l_Orders.ShipToAddress1 = "";
            l_Orders.ShipToAddress2 = "";
            l_Orders.ShipToCity = "";
            l_Orders.ShipToState = "";
            l_Orders.ShipToZip = "";
            l_Orders.ShipToCountry = "";
            l_Orders.IsStoreOrder = false;
            l_Orders.CreatedBy = userNo;
            l_Orders.CreatedDate = DateTime.Now;
        }

        if (l_Orders.SaveNew().IsSuccess)
        {
            l_Data = new OrderData();

            l_Data.UseConnection(string.Empty, l_Orders.Connection);
            l_Data.DeleteWithType(l_Orders.Id, "API-JSON");

            l_Data.Type = "API-JSON";
            l_Data.Data = jsonString;
            l_Data.CreatedBy = userNo;
            l_Data.CreatedDate = DateTime.Now;
            l_Data.OrderId = l_Orders.Id;
            l_Data.OrderNumber = l_Orders.OrderNumber;

            l_Data.SaveNew();

            l_Data.UpdateOrderDataOrderID(l_Data.OrderNumber, l_Data.OrderId);
        }

        foreach (var orderLine in order.order_lines)
        {
            OrderDetail l_OrderDetail = new OrderDetail();

            l_OrderDetail.UseConnection(string.Empty, l_Orders.Connection);

            for (int i = 1; i <= Convert.ToInt32(orderLine.quantity); i++)
            {
                l_OrderDetail.OrderId = l_Orders.Id;
                l_OrderDetail.LineNo = Convert.ToInt32(orderLine.order_line_index);
                l_OrderDetail.LineQty = 1;
                l_OrderDetail.ItemID = orderLine.offer_sku;
                l_OrderDetail.UnitPrice = Convert.ToDecimal(orderLine.price_unit);
                l_OrderDetail.Status = "NEW";
                l_OrderDetail.order_line_id = orderLine.order_line_id;
                l_OrderDetail.CreatedBy = userNo;
                l_OrderDetail.CreatedDate = DateTime.Now;

                l_OrderDetail.SaveNew();
            }
        }

        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
        route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
    }
    catch (Exception)
    {
        route.SaveLog(LogTypeEnum.Error, $"Processing Error [{l_Orders.OrderNumber}]", string.Empty, userNo);
    }
}


static void ProcessOrderForShipment(string orderNumber)
{
    string l_TransformationMap = "{     \"items\": {         \"#loop($.OutPut.TrackingInfo)\": {             \"level_of_service\": \"#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX HOME DELIVERY,FH,#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX GROUND,G2,FH))\",             \"order_line_number\": \"#currentvalueatpath($.OrderLineNo)\",             \"quantity\": \"#currentvalueatpath($.ShippedQty)\",             \"shipped_date\": \"#concat(#currentvalueatpath($.ShippedDate),T00:00:00.000Z)\",             \"shipping_method\": \"#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX HOME DELIVERY,FedExGroundHomeDelivery,#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX GROUND,FedExGround,FedExGround))\",             \"tracking_number\": \"#currentvalueatpath($.TrackingNo)\"         }     } }";
    string Body = "";
    int orderId = 0;
    string connectionString = "Server=.;Database=ESYNCMATE;UID=sa;PWD=angel;";

    SCSGetOrderResponseModel order = GetOrdersData(orderNumber);

    string sql = $@"SELECT TOP 1 O.Id, OD.Data 
                    FROM OrderData OD
                        INNER JOIN Orders O ON OD.OrderId = O.Id
                    WHERE O.OrderNumber = '{orderNumber}' AND OD.Type = 'ERPASN-JSON'";

    DBConnector conn = new DBConnector(connectionString);
    DataTable l_Data = new DataTable();

    if (conn.GetData(sql, ref l_Data))
    {
        Body = l_Data.Rows[0]["Data"].ToString();
        orderId = Convert.ToInt32(l_Data.Rows[0]["Id"].ToString());

        SCSASNResponse asn = JsonConvert.DeserializeObject<SCSASNResponse>(Body);

        foreach (TrackingInfo line in asn?.OutPut.TrackingInfo)
        {
            line.OrderLineNo = order.order_lines[Convert.ToInt32(line.OrderLineNo) - 1].order_line_number;
        }

        Body = JsonConvert.SerializeObject(asn);

        string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, Body);
        OrderData l_OrderData = new OrderData();

        l_OrderData.UseConnection(connectionString);

        l_OrderData.Type = "ASN-SNT";
        l_OrderData.Data = jsonTransformation;
        l_OrderData.CreatedBy = 1;
        l_OrderData.CreatedDate = DateTime.Now;
        l_OrderData.OrderId = orderId;
        l_OrderData.OrderNumber = orderNumber;

        l_OrderData.SaveNew();

        string asnResponse = CreateShipment(orderNumber, jsonTransformation);

        l_OrderData = new OrderData();

        l_OrderData.UseConnection(connectionString);

        l_OrderData.Type = "ASN-RES";
        l_OrderData.Data = asnResponse;
        l_OrderData.CreatedBy = 1;
        l_OrderData.CreatedDate = DateTime.Now;
        l_OrderData.OrderId = orderId;
        l_OrderData.OrderNumber = orderNumber;

        l_OrderData.SaveNew();
    }
}

static int GetAlItems()
{
    string after_id = string.Empty;
    StringBuilder data = new StringBuilder();
    int i = 1;

    do
    {
        string json = GetItems(after_id);

        if (string.IsNullOrEmpty(json))
        {
            File.WriteAllText("Items.csv", data.ToString());
            Console.WriteLine("Data not received");
            return 0;
        }

        after_id = string.Empty;
        RouteTestApp.Items[] items = JsonConvert.DeserializeObject<RouteTestApp.Items[]>(json);

        foreach (RouteTestApp.Items item in items)
        {
            data.AppendLine($"{item.id},{item.external_id}");
            after_id = item.id;
        }

        Console.WriteLine($"Page # {i++} completed");
    }
    while (!string.IsNullOrEmpty(after_id));

    File.WriteAllText("Items.csv", data.ToString());
    Console.WriteLine("Job completed");

    return 0;
}

static string GetItems(string after_id)
{

    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.target.com/sellers/v1/")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    if (string.IsNullOrEmpty(after_id))
        request = new RestRequest("sellers/6802accd146a9b60e3850f70/products_catalog?per_page=1000&expand=fields", Method.Get);
    else
        request = new RestRequest($"sellers/6802accd146a9b60e3850f70/products_catalog?per_page=1000&after_id={after_id}", Method.Get);

    request.AddHeader("x-api-key", "80951e9d352afdd7725961817c62a51baf637658");
    request.AddHeader("x-seller-token", "d061a03b9bbc48c48c63b93559bd48a8");
    request.AddHeader("x-seller-id", "6802accd146a9b60e3850f70");

    response = client.Execute(request);

    return response.Content.ToString();
}


static string UpdateProductlogistics(string ProductID, string Body)
{
    StringBuilder data = new StringBuilder();
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.target.com/sellers/v1/")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/product_logistics/{ProductID}", Method.Put);

    request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
    request.AddHeader("x-seller-token", "0902d4a0688a4cdeaaee926fc1f70155");
    request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

    request.AddStringBody(Body, RestSharp.DataFormat.Json);

    response = client.Execute(request);


    return response.Content.ToString();

}

static void UpdateInventory()
{
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.target.com/sellers/v1/")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    request = new RestRequest("sellers/5d949496fcd4b70097dfad5e/products/63eb35bc67c68257765f408f/quantities/mikpz1", Method.Put);

    request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
    request.AddHeader("x-seller-token", "b6151b11b08e43ac81706a41a0d4ac00");
    request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

    request.AddStringBody("{\r\n    \"quantity\": 10 \r\n}", RestSharp.DataFormat.Json);

    response = client.Execute(request);

    if (response.StatusCode == System.Net.HttpStatusCode.OK)
    {
        Console.WriteLine("Success");
    }
    else
        Console.WriteLine("Error");

    Console.ReadLine();
}

static string CreateShipment(string orderNumber, string body)
{
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.target.com/seller_orders/v1/")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/orders/{orderNumber}/bulk_fulfillments_create", Method.Post);

    request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
    request.AddHeader("x-seller-token", "b6151b11b08e43ac81706a41a0d4ac00");
    request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

    request.AddStringBody(body, RestSharp.DataFormat.Json);

    response = client.Execute(request);

    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
    {
        return response.Content;
    }

    return "";
}

static SCSGetOrderResponseModel GetOrdersData(string orderNumber)
{
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.target.com/seller_orders/v1")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/orders/{orderNumber}", Method.Get);

    request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
    request.AddHeader("x-seller-token", "b6151b11b08e43ac81706a41a0d4ac00");
    request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

    response = client.Execute(request);

    return JsonConvert.DeserializeObject<SCSGetOrderResponseModel>(response.Content);
}

static List<ASNResponse> GetASNData(string orderNumber)
{
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.target.com/seller_orders/v1")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/orders/{orderNumber}/fulfillments", Method.Get);

    request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
    request.AddHeader("x-seller-token", "b6151b11b08e43ac81706a41a0d4ac00");
    request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

    response = client.Execute(request);

    return JsonConvert.DeserializeObject<List<ASNResponse>>(response.Content);
}

static SCSASNResponse GetTrackingData(string orderNumber, string token)
{
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.safavieh.com/SPARS.API/api/service/")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    request = new RestRequest($"Get_TrackingInfo", Method.Post);

    request.AddHeader("AccessToken", string.IsNullOrEmpty(token) ? GetERPToken() : token);

    var asnInfo = new
    {
        Input = new
        {
            OrderNo = "",
            CustomerPO = orderNumber,
            ExternalID = ""
        }
    };

    request.AddStringBody(JsonConvert.SerializeObject(asnInfo), RestSharp.DataFormat.Json);

    response = client.Execute(request);

    SCSASNResponse res = JsonConvert.DeserializeObject<SCSASNResponse>(response.Content);

    if (res.OutPut.Message == "SaleOrder Information not available.")
        return GetTrackingData(orderNumber.Split('-')[0], token);

    return res;
}




static int GetWalmartAlItems()
{
    string after_id = string.Empty;
    StringBuilder data = new StringBuilder();
    int i = 1;
    after_id = "aaf50ffc-d773-4407-ab85-e4b3ff835969";
    do
    {
        string json = GetWalmartItems(after_id);

        if (string.IsNullOrEmpty(json))
        {
            File.WriteAllText("WalmartItems.csv", data.ToString());
            Console.WriteLine("Data not received");
            return 0;
        }

        after_id = string.Empty;
        RouteTestApp.WalmartItemsData items = JsonConvert.DeserializeObject<RouteTestApp.WalmartItemsData>(json);

        after_id = items.nextCursor;

        foreach (var item in items.ItemResponse)
        {
            data.AppendLine($"{item.wpid},{item.sku},{after_id}");
        }

        Console.WriteLine($"Page # {i++} completed");
        System.Threading.Thread.Sleep(100);
    }
    while (!string.IsNullOrEmpty(after_id));

    File.WriteAllText("WalmartItems.csv", data.ToString());
    Console.WriteLine("Job completed");

    return 0;
}

static string GetWalmartItems(string after_id)
{
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://marketplace.walmartapis.com")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    if (string.IsNullOrEmpty(after_id))
        //request = new RestRequest("sellers/5d949496fcd4b70097dfad5e/products_catalog?per_page=1000&expand=fields", Method.Get);
        request = new RestRequest("/v3/items?nextCursor=*&limit=250", Method.Get);
    else
        //request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/products_catalog?per_page=1000&after_id={after_id}", Method.Get);
        request = new RestRequest($"/v3/items?nextCursor={after_id}&limit=250", Method.Get);

    request.AddHeader("WM_SEC.ACCESS_TOKEN", GetApiToken("https://marketplace.walmartapis.com", "985ea112-49e5-47c1-8697-434d58fb765b", "LnHtjFzxDmkQ4f385P-eum3nynIeOXkvRMCNZj0wPaBrWaOqgCkKZWEUuWEjHr1WqgKPclk-iMuMgG_lIIa1ng"));
    request.AddHeader("WM_QOS.CORRELATION_ID", "e61ac258-2ceb-4028-b8d4-11e5ae9df6e9");
    request.AddHeader("WM_SVC.NAME", "WalmartAPI");
    request.AddHeader("Accept", "application/json");

    response = client.Execute(request);

    return response.Content.ToString();
}

static string GetApiToken(string BaseURL, string clientId, string clientSecret)
{
    RestClient client = new RestClient();
    RestRequest request = new RestRequest();
    RestResponse response = new RestResponse();
    Guid guid = Guid.NewGuid();
    try
    {
        var authenticationString = $"{clientId}:{clientSecret}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));

        request = new RestRequest(BaseURL + "/v3/token", Method.Post);

        request.AddHeader("Authorization", "Basic " + base64EncodedAuthenticationString);
        request.AddHeader("WM_SVC.NAME", "WalmartAPI");
        request.AddHeader("WM_QOS.CORRELATION_ID", guid.ToString());
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "client_credentials");

        response = client.Execute(request);

        var tokenInfoDefinition = new
        {
            access_token = "",
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


static string GetERPToken()
{
    RestClient client;
    RestRequest request;
    RestResponse response;
    RestClientOptions options = new RestClientOptions("https://api.safavieh.com/SPARS.API/api/service/")
    {
        MaxTimeout = -1,
    };

    client = new RestClient(options);

    request = new RestRequest($"Get_Token?key=07453D34-6A64-431A-939A-7AC666338427&Company=eMS", Method.Get);

    response = client.Execute(request);

    var tokenInfoDefinition = new
    {
        Code = "",
        Description = "",
        Token = ""
    };

    var tokenInfo = JsonConvert.DeserializeAnonymousType(response.Content, tokenInfoDefinition);
    return tokenInfo.Token;
}


static void ProcessOrderDetail()
{
    string Body = "";
    int orderId = 0;
    string connectionString = "Server=192.168.0.44,7100;Database=ESYNCMATE;UID=esyncmate;PWD=eSyncMate786$$$;";

    //SCSGetOrderResponseModel order = GetOrdersData(orderNumber);

    string sql = $@"SELECT * FROM [MissingDetail]";

    DBConnector conn = new DBConnector(connectionString);
    DataTable l_Data = new DataTable();

    if (conn.GetData(sql, ref l_Data))
    {

        foreach (DataRow row in l_Data.Rows)
        {
            Body = row["Data"].ToString();
            orderId = Convert.ToInt32(row["Id"].ToString());

            if (row["ERPCustomerID"].ToString() == "TAR6266P")
            {
                SCSGetOrderResponseModel Orders = new SCSGetOrderResponseModel();

                Orders = JsonConvert.DeserializeObject<SCSGetOrderResponseModel>(Body);

                foreach (var orderLine in Orders.order_lines)
                {
                    OrderDetail l_OrderDetail = new OrderDetail();

                    l_OrderDetail.UseConnection(string.Empty, conn);

                    for (int i = 1; i <= orderLine.quantity; i++)
                    {
                        l_OrderDetail.OrderId = orderId;
                        l_OrderDetail.LineNo = Convert.ToInt32(orderLine.order_line_number);
                        l_OrderDetail.ItemID = orderLine.external_id;
                        l_OrderDetail.LineQty = 1;
                        l_OrderDetail.UnitPrice = orderLine.unit_price;
                        l_OrderDetail.Status = "NEW";
                        l_OrderDetail.CreatedBy = 1;
                        l_OrderDetail.CreatedDate = DateTime.Now;

                        l_OrderDetail.SaveNew();
                    }
                }
            }

            if (row["ERPCustomerID"].ToString() == "WAL4001MP")
            {
                WalmartOrder l_WalmartOrder = new WalmartOrder();

                l_WalmartOrder = JsonConvert.DeserializeObject<WalmartOrder>(Body);


                foreach (var orderLine in l_WalmartOrder.orderLines.orderLine)
                {
                    OrderDetail l_OrderDetail = new OrderDetail();

                    l_OrderDetail.UseConnection(string.Empty, conn);

                    for (int i = 1; i <= Convert.ToInt32(orderLine.orderLineQuantity.amount); i++)
                    {
                        l_OrderDetail.OrderId = orderId;
                        l_OrderDetail.LineNo = Convert.ToInt32(orderLine.lineNumber);
                        l_OrderDetail.LineQty = 1;
                        l_OrderDetail.ItemID = orderLine.item.sku;
                        l_OrderDetail.UnitPrice = Convert.ToDecimal(orderLine.charges.charge[0].chargeAmount.amount);
                        l_OrderDetail.Status = "NEW";
                        l_OrderDetail.CreatedBy = 1;
                        l_OrderDetail.CreatedDate = DateTime.Now;

                        l_OrderDetail.SaveNew();
                    }
                }
            }

        }
    }
}



Main();


// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");








//using System;
//using System.IO;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//class AmazonSPInventoryFeed
//{
//    private static readonly string accessKey = "amzn1.application-oa2-client.a2f7e3f5554e439eab75ea3d82984528";
//    private static readonly string secretKey = "amzn1.oa2-cs.v1.cb4938d653dba200197927d6015200ca70174d5ba7ef33dee2269a1b07dad8e7";
//    private static readonly string roleArn = "YOUR_AWS_ROLE_ARN";
//    private static readonly string region = "NA"; // Change to your region
//    private static readonly string refreshToken = "Atzr|IwEBIFg-nxBqVVhfY1HfryXdERt54LfAPum1NSpWiEvmQN0Eawf-dy5_iFZBioPlpTCmz8oZiGtqFOTETpQeasNwp85IrlMNv0h2nhytbqZYtSOL_yCOurDIDAuKjjKn-ZJ7rXPiBTA3mlK6PNxidAQfrzqTml3DUBXpQTypx8Y3xiGf40vKUMP-aV4UDbkxkf4CV3zj5s5bV5IU86VvRpX2EKObJkF4_gJgryhyntik1rCM9VxkjYwlGJ52yfr4gQbUUZbhwYA0ZHoH2CtXIbAadTy1vGeu4r9M0E4fsP6Fffm3YLufZif-CoiHYlOXer7EZ-c54efTM-QC6zTIVGn69AcW";
//    private static readonly string endpoint = "https://sellingpartnerapi-na.amazon.com"; // Change for EU or other regions

//    static async Task Main()
//    {
//        try
//        {
//            string xmlContent = GenerateInventoryFeedXml();
//            string feedDocumentId = await CreateFeedDocument();
//            await UploadFeedDocument(feedDocumentId, xmlContent);
//            string feedId = await SubmitFeed(feedDocumentId);
//            Console.WriteLine($"Feed submitted successfully. Feed ID: {feedId}");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error: {ex.Message}");
//        }
//    }

//    private static string GenerateInventoryFeedXml()
//    {
//        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
//        <AmazonEnvelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""amzn-envelope.xsd"">
//            <Header>
//                <DocumentVersion>1.02</DocumentVersion>
//                <MerchantIdentifier>YOUR_SELLER_ID</MerchantIdentifier>
//            </Header>
//            <MessageType>Inventory</MessageType>
//            <Message>
//                <MessageID>1</MessageID>
//                <OperationType>Update</OperationType>
//                <Inventory>
//                    <SKU>SKU12345</SKU>
//                    <Quantity>10</Quantity>
//                    <FulfillmentLatency>1</FulfillmentLatency>
//                </Inventory>
//            </Message>
//            <Message>
//                <MessageID>2</MessageID>
//                <OperationType>Update</OperationType>
//                <Inventory>
//                    <SKU>SKU67890</SKU>
//                    <Quantity>5</Quantity>
//                    <FulfillmentLatency>2</FulfillmentLatency>
//                </Inventory>
//            </Message>
//        </AmazonEnvelope>";
//    }

//    private static async Task<string> CreateFeedDocument()
//    {
//        using HttpClient client = new HttpClient();
//        var lwaClient = new LWAAuthorizationSigner(accessKey, secretKey, refreshToken);
//        string uri = $"{endpoint}/feeds/2021-06-30/documents";
//        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri)
//        {
//            Content = new StringContent(JsonConvert.SerializeObject(new { contentType = "text/xml; charset=UTF-8" }), Encoding.UTF8, "application/json")
//        };
//        request = await lwaClient.SignWithLWAAsync(request, roleArn, region);

//        HttpResponseMessage response = await client.SendAsync(request);
//        string responseContent = await response.Content.ReadAsStringAsync();
//        JObject jsonResponse = JObject.Parse(responseContent);
//        return jsonResponse["feedDocumentId"].ToString();
//    }

//    private static async Task UploadFeedDocument(string feedDocumentId, string xmlContent)
//    {
//        using HttpClient client = new HttpClient();
//        // Get the pre-signed URL for upload
//        string uri = $"{endpoint}/feeds/2021-06-30/documents/{feedDocumentId}";
//        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
//        var lwaClient = new LWAAuthorizationSigner(accessKey, secretKey, refreshToken);
//        request = await lwaClient.SignWithLWAAsync(request, roleArn, region);

//        HttpResponseMessage response = await client.SendAsync(request);
//        string responseContent = await response.Content.ReadAsStringAsync();
//        JObject jsonResponse = JObject.Parse(responseContent);
//        string uploadUrl = jsonResponse["url"].ToString();

//        // Upload XML content to Amazon's pre-signed URL
//        HttpRequestMessage uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
//        {
//            Content = new StringContent(xmlContent, Encoding.UTF8, "text/xml")
//        };
//        await client.SendAsync(uploadRequest);
//    }

//    private static async Task<string> SubmitFeed(string feedDocumentId)
//    {
//        using HttpClient client = new HttpClient();
//        var lwaClient = new LWAAuthorizationSigner(accessKey, secretKey, refreshToken);
//        string uri = $"{endpoint}/feeds/2021-06-30";

//        var requestBody = new
//        {
//            feedType = "POST_INVENTORY_AVAILABILITY_DATA",
//            inputFeedDocumentId = feedDocumentId
//        };

//        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri)
//        {
//            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
//        };
//        request = await lwaClient.SignWithLWAAsync(request, roleArn, region);

//        HttpResponseMessage response = await client.SendAsync(request);
//        string responseContent = await response.Content.ReadAsStringAsync();
//        JObject jsonResponse = JObject.Parse(responseContent);
//        return jsonResponse["feedId"].ToString();
//    }
//}

//public class LWAAuthorizationSigner
//{
//    private string accessKey;
//    private string secretKey;
//    private string refreshToken;
//    private string endpoint = "https://api.amazon.com/auth/o2/token";

//    public LWAAuthorizationSigner(string accessKey, string secretKey, string refreshToken)
//    {
//        this.accessKey = accessKey;
//        this.secretKey = secretKey;
//        this.refreshToken = refreshToken;
//    }

//    public async Task<HttpRequestMessage> SignWithLWAAsync(HttpRequestMessage request, string roleArn, string region)
//    {
//        // Get access token using refresh token (LWA flow)
//        var token = await GetLwaAccessToken();
//        request.Headers.Add("Authorization", $"Bearer {token}");
//        return request;
//    }

//    private async Task<string> GetLwaAccessToken()
//    {
//        var content = new StringContent($"grant_type=refresh_token&refresh_token={this.refreshToken}&client_id={this.accessKey}&client_secret={this.secretKey}", Encoding.UTF8, "application/x-www-form-urlencoded");
//        using (var client = new HttpClient())
//        {
//            var response = await client.PostAsync(endpoint, content);
//            response.EnsureSuccessStatusCode();
//            var responseContent = await response.Content.ReadAsStringAsync();
//            JObject jsonResponse = JObject.Parse(responseContent);
//            return jsonResponse["access_token"].ToString();
//        }
//    }
//}




//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http.Headers;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using Newtonsoft.Json.Linq;
//using System.Security.Policy;
//using System.Net;

//namespace AMZTest
//{
//    internal class Program
//    {
//        static void Main(string[] args)
//        {
//            string token = GetToken().GetAwaiter().GetResult();
//            JObject response = CreateDocument(token).GetAwaiter().GetResult();

//            string feedDocumentId = response["feedDocumentId"].ToString();
//            string feedUrl = response["url"].ToString();

//            Console.WriteLine(feedUrl);
//            Console.WriteLine("");

//            string res = UploadDocument(feedDocumentId, feedUrl).GetAwaiter().GetResult();

//            Console.WriteLine(res);
//            Console.ReadLine();
//        }

//        private static string GenerateInventoryFeedXml()
//        {
//            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
//                    <AmazonEnvelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""amzn-envelope.xsd"">
//                        <Header>
//                            <DocumentVersion>1.02</DocumentVersion>
//                            <MerchantIdentifier>YOUR_SELLER_ID</MerchantIdentifier>
//                        </Header>
//                        <MessageType>Inventory</MessageType>
//                        <Message>
//                            <MessageID>1</MessageID>
//                            <OperationType>Update</OperationType>
//                            <Inventory>
//                                <SKU>SKU12345</SKU>
//                                <Quantity>10</Quantity>
//                                <FulfillmentLatency>1</FulfillmentLatency>
//                            </Inventory>
//                        </Message>
//                        <Message>
//                            <MessageID>2</MessageID>
//                            <OperationType>Update</OperationType>
//                            <Inventory>
//                                <SKU>SKU67890</SKU>
//                                <Quantity>5</Quantity>
//                                <FulfillmentLatency>2</FulfillmentLatency>
//                            </Inventory>
//                        </Message>
//                    </AmazonEnvelope>";
//        }

//        async static Task<string> GetToken()
//        {
//            var client = new HttpClient();
//            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.amazon.com/auth/o2/token?application_id=amzn1.sp.solution.5981b776-7570-4815-8455-4b0637d8d4ab&client_id=amzn1.application-oa2-client.a2f7e3f5554e439eab75ea3d82984528&client_secret=amzn1.oa2-cs.v1.cb4938d653dba200197927d6015200ca70174d5ba7ef33dee2269a1b07dad8e7&refresh_token=Atzr|IwEBIFg-nxBqVVhfY1HfryXdERt54LfAPum1NSpWiEvmQN0Eawf-dy5_iFZBioPlpTCmz8oZiGtqFOTETpQeasNwp85IrlMNv0h2nhytbqZYtSOL_yCOurDIDAuKjjKn-ZJ7rXPiBTA3mlK6PNxidAQfrzqTml3DUBXpQTypx8Y3xiGf40vKUMP-aV4UDbkxkf4CV3zj5s5bV5IU86VvRpX2EKObJkF4_gJgryhyntik1rCM9VxkjYwlGJ52yfr4gQbUUZbhwYA0ZHoH2CtXIbAadTy1vGeu4r9M0E4fsP6Fffm3YLufZif-CoiHYlOXer7EZ-c54efTM-QC6zTIVGn69AcW&grant_type=refresh_token");
//            request.Headers.Add("contentType", "application/x-www-form-urlencoded");
//            var content = new StringContent(string.Empty);
//            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
//            request.Content = content;
//            var response = await client.SendAsync(request);
//            response.EnsureSuccessStatusCode();

//            string responseContent = await response.Content.ReadAsStringAsync();
//            JObject jsonResponse = JObject.Parse(responseContent);

//            return jsonResponse["access_token"].ToString();
//        }

//        async static Task<JObject> CreateDocument(string token)
//        {
//            var client = new HttpClient();
//            var request = new HttpRequestMessage(HttpMethod.Post, "https://sellingpartnerapi-na.amazon.com/feeds/2021-06-30/documents");
//            request.Headers.Add("contentType", "application/json");
//            request.Headers.Add("x-amz-access-token", token);
//            var content = new StringContent("{\r\n    \"contentType\": \"application/xml\"\r\n}\r\n\r\n\r\n", null, "application/json");
//            request.Content = content;
//            var response = await client.SendAsync(request);
//            response.EnsureSuccessStatusCode();

//            string responseContent = await response.Content.ReadAsStringAsync();
//            JObject jsonResponse = JObject.Parse(responseContent);
//            return jsonResponse;
//        }

//        async static Task<string> UploadDocument(string feedDocumentId, string url)
//        {
//            var client = new HttpClient();
//            byte[] xmlBytes = Encoding.UTF8.GetBytes(GenerateInventoryFeedXml());
//            ByteArrayContent content = new ByteArrayContent(xmlBytes);
//            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");

//            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url)
//            {
//                Content = content
//            };
//            var response = await client.SendAsync(request);

//            response.EnsureSuccessStatusCode();

//            return await response.Content.ReadAsStringAsync();
//        }
//    }
//}


////OrderAcknoledgement
///


//using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Threading.Tasks;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        try
//        {
//            string token = await GetToken();
//            Console.WriteLine("Token obtained successfully");

//            JObject response = await CreateDocument(token);
//            string feedDocumentId = response["feedDocumentId"].ToString();
//            string feedUrl = response["url"].ToString();
//            Console.WriteLine($"Feed document created: {feedDocumentId}");
//            Console.WriteLine($"Upload URL: {feedUrl}");

//            await UploadDocument(feedDocumentId, feedUrl);
//            Console.WriteLine("Document uploaded successfully");

//            string feedId = await SubmitFeed(feedDocumentId, token, "https://sellingpartnerapi-na.amazon.com");
//            Console.WriteLine($"Feed submitted successfully. FeedId: {feedId}");

//            // Optional: check feed status
//            await Task.Delay(30000); // Wait 30 seconds
//            string status = await GetFeedStatus(feedId, token, "https://sellingpartnerapi-na.amazon.com");
//            Console.WriteLine($"Feed status: {status}");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error: {ex.Message}");
//            Console.WriteLine(ex.StackTrace);
//        }

//        Console.WriteLine("Press Enter to exit...");
//        Console.ReadLine();
//    }

//    private static string GenerateOrderAcknowledgementXml()
//    {
//        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
//                    <AmazonEnvelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""amzn-envelope.xsd"">
//                        <Header>
//                            <DocumentVersion>1.02</DocumentVersion>
//                            <MerchantIdentifier>YOUR_SELLER_ID</MerchantIdentifier>
//                        </Header>
//                        <MessageType>OrderAcknowledgement</MessageType>
//                        <Message>
//                          <MessageID>1</MessageID>
//                          <OrderAcknowledgement>
//                            <AmazonOrderID>114-3149162-1857002</AmazonOrderID>
//                            <MerchantOrderID>114-3149162-1857002</MerchantOrderID>
//                            <StatusCode>Success</StatusCode>
//                            <Item>
//                              <AmazonOrderItemCode>122483114924841</AmazonOrderItemCode>
//                              <MerchantOrderItemID>B09NM6C1SM HZN890P-28</MerchantOrderItemID>
//                              <quantity>1</quantity>
//                            </Item>
//                          </OrderAcknowledgement>
//                        </Message>
//                    </AmazonEnvelope>";
//    }

//    async static Task<string> GetToken()
//    {
//        var client = new HttpClient();

//        // Create the token request
//        var tokenRequest = new FormUrlEncodedContent(new[]
//        {
//            new KeyValuePair<string, string>("grant_type", "refresh_token"),
//            new KeyValuePair<string, string>("refresh_token", "Atzr|IwEBIFg-nxBqVVhfY1HfryXdERt54LfAPum1NSpWiEvmQN0Eawf-dy5_iFZBioPlpTCmz8oZiGtqFOTETpQeasNwp85IrlMNv0h2nhytbqZYtSOL_yCOurDIDAuKjjKn-ZJ7rXPiBTA3mlK6PNxidAQfrzqTml3DUBXpQTypx8Y3xiGf40vKUMP-aV4UDbkxkf4CV3zj5s5bV5IU86VvRpX2EKObJkF4_gJgryhyntik1rCM9VxkjYwlGJ52yfr4gQbUUZbhwYA0ZHoH2CtXIbAadTy1vGeu4r9M0E4fsP6Fffm3YLufZif-CoiHYlOXer7EZ-c54efTM-QC6zTIVGn69AcW"),
//            new KeyValuePair<string, string>("client_id", "amzn1.application-oa2-client.a2f7e3f5554e439eab75ea3d82984528"),
//            new KeyValuePair<string, string>("client_secret", "amzn1.oa2-cs.v1.cb4938d653dba200197927d6015200ca70174d5ba7ef33dee2269a1b07dad8e7")
//        });

//        var response = await client.PostAsync(
//            "https://api.amazon.com/auth/o2/token?application_id=amzn1.sp.solution.5981b776-7570-4815-8455-4b0637d8d4ab",
//            tokenRequest);

//        response.EnsureSuccessStatusCode();

//        string responseContent = await response.Content.ReadAsStringAsync();
//        JObject jsonResponse = JObject.Parse(responseContent);

//        return jsonResponse["access_token"].ToString();
//    }

//    async static Task<JObject> CreateDocument(string token)
//    {
//        var client = new HttpClient();
//        var request = new HttpRequestMessage(HttpMethod.Post, "https://sellingpartnerapi-na.amazon.com/feeds/2021-06-30/documents");
//        request.Headers.Add("x-amz-access-token", token);

//        var requestBody = new
//        {
//            contentType = "application/xml"
//        };

//        string jsonBody = JsonConvert.SerializeObject(requestBody);
//        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
//        request.Content = content;

//        var response = await client.SendAsync(request);
//        response.EnsureSuccessStatusCode();

//        string responseContent = await response.Content.ReadAsStringAsync();
//        JObject jsonResponse = JObject.Parse(responseContent);
//        return jsonResponse;
//    }

//    async static Task<string> UploadDocument(string feedDocumentId, string url)
//    {
//        var client = new HttpClient();
//        byte[] xmlBytes = Encoding.UTF8.GetBytes(GenerateOrderAcknowledgementXml());
//        ByteArrayContent content = new ByteArrayContent(xmlBytes);
//        content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

//        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url)
//        {
//            Content = content
//        };
//        var response = await client.SendAsync(request);
//        response.EnsureSuccessStatusCode();

//        return await response.Content.ReadAsStringAsync();
//    }

//    private static async Task<string> SubmitFeed(string feedDocumentId, string token, string baseUrl)
//    {
//        var client = new HttpClient();
//        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/feeds/2021-06-30/feeds");

//        //string p_token = await GetToken();
//        try
//        {
//            request.Headers.Add("x-amz-access-token",token);

//            var requestBody = new
//            {
//                feedType = "POST_ORDER_ACKNOWLEDGEMENT_DATA",
//                inputFeedDocumentId = feedDocumentId,
//                marketplaceIds = new[] { "ATVPDKIKX0DER" }

//            };

//            string requestBodyString = JsonConvert.SerializeObject(requestBody);
//            var content = new StringContent(requestBodyString, Encoding.UTF8);
//            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
//            request.Content = content;

//            var response = await client.SendAsync(request);

//            // Don't use EnsureSuccessStatusCode yet to capture error details
//            if (!response.IsSuccessStatusCode)
//            {
//                string errorContent = await response.Content.ReadAsStringAsync();
//                Console.WriteLine($"Error Status Code: {response.StatusCode}");
//                Console.WriteLine($"Error Details: {errorContent}");
//                throw new HttpRequestException($"Error: {response.StatusCode}. Details: {errorContent}");
//            }

//            string responseContent = await response.Content.ReadAsStringAsync();
//            JObject jsonResponse = JObject.Parse(responseContent);

//            return jsonResponse["feedId"].ToString();
//        }
//        catch (HttpRequestException ex)
//        {
//            Console.WriteLine($"HTTP error submitting feed: {ex.Message}");
//            throw;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error submitting feed: {ex.Message}");
//            throw;
//        }
//    }

//    private static async Task<string> GetFeedStatus(string feedId, string token, string baseUrl)
//    {
//        using (var client = new HttpClient())
//        {
//            try
//            {
//                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/feeds/2021-06-30/feeds/{feedId}");
//                request.Headers.Add("x-amz-access-token", token);

//                var response = await client.SendAsync(request);
//                response.EnsureSuccessStatusCode();

//                string responseContent = await response.Content.ReadAsStringAsync();
//                JObject jsonResponse = JObject.Parse(responseContent);

//                return jsonResponse["processingStatus"].ToString();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error getting feed status: {ex.Message}");
//                throw;
//            }
//        }
//    }
//}
