using EdiEngine;
using EdiEngine.Runtime;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Controllers;
using eSyncMate.Processor.Models;
using Hangfire;
using Hangfire.Storage;
using Intercom.Core;
using JUST;
using Nancy;
using Newtonsoft.Json;
using RestSharp;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using static eSyncMate.DB.Declarations;
using Intercom.Data;
using eSyncMate.DB;
using System.Data;
using System.Threading;
using Newtonsoft.Json.Linq;
using eSyncMate.Maps;
using static eSyncMate.Processor.Models._856TransformJson;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components.Forms;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;


namespace eSyncMate.Processor.Managers
{
    public class Download810FromFTPRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            const int userNo = 1;

            try
            {
                var sourceConnector = DeserializeConnector<ConnectorDataModel>(route.SourceConnectorObject.Data);
                var destinationConnector = DeserializeConnector<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                if (sourceConnector == null || destinationConnector == null)
                {
                    return;
                }

                if (sourceConnector.AuthType != ConnectorTypesEnum.FTP.ToString())
                {
                    logger.LogError("Unsupported connector type. Expected FTP.");
                    return;
                }

                var files = FtpConnector.Execute(sourceConnector.Host, sourceConnector.ConsumerKey, sourceConnector.ConsumerSecret, sourceConnector.Method, true, "", "").GetAwaiter().GetResult();

                foreach (var file in files)
                {
                    ProcessFile(file, sourceConnector, destinationConnector, route, userNo);
                }

                route.SaveLog(LogTypeEnum.Info, $"Route data received!", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        private static T? DeserializeConnector<T>(string data) where T : class
        {
            var connector = JsonConvert.DeserializeObject<T>(data);
            if (connector == null)
            {
                throw new InvalidOperationException("Connector is not set up properly");
            }
            return connector;
        }

        private static void ProcessFile(KeyValuePair<string, string> file, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, Routes route, int userNo)
        {
            try
            {
                route.UseConnection(destinationConnector.ConnectionString);
                route.SaveLog(LogTypeEnum.Info, $"Processing file {file.Key}", string.Empty, userNo);
                route.SaveData("SRC", 0, file.Value, userNo);

                var edi = CreateInboundEDI(destinationConnector.ConnectionString, file.Value, userNo);
                var ediBatch = new EdiDataReader().FromString(file.Value);

                foreach (var interchange in ediBatch.Interchanges)
                {
                    ProcessInterchange(interchange, edi, route, userNo, destinationConnector, sourceConnector, file.Key);
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed processing file {file.Key}", string.Empty, userNo);

                Task<bool> task = FtpConnector.DeleteFileFromFTP(sourceConnector.Host, sourceConnector.ConsumerKey, sourceConnector.ConsumerSecret, sourceConnector.Method, file.Key);

            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error processing file [{file.Key}]", ex.ToString(), userNo);
            }
        }

        private static InboundEDI CreateInboundEDI(string connectionString, string data, int userNo)
        {
            var edi = new InboundEDI();

            edi.UseConnection(connectionString);

            edi.Type = "810";
            edi.Status = "NEW";
            edi.Data = data;
            edi.CreatedBy = userNo;
            edi.CreatedDate = DateTime.Now;

            edi.SaveNew();

            return edi;
        }

        private static void ProcessInterchange(EdiInterchange interchange, InboundEDI edi, Routes route, int userNo, ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, string fileKey)
        {
            var transformationMap = GetTransformationMap(route.MapId, destinationConnector.ConnectionString);
            if (string.IsNullOrEmpty(transformationMap))
            {
                edi.UseConnection(destinationConnector.ConnectionString);
                edi.Status = "ERROR";
                edi.Modify();
                route.SaveLog(LogTypeEnum.Exception, $"Required maps for 810 processing are missing.", string.Empty, userNo);
                return;
            }

            var ediInfo = CreateInboundEDIInfo(interchange, edi.Id, userNo, destinationConnector.ConnectionString);

            if (ediInfo.SaveNew().IsSuccess)
            {
                foreach (var group in interchange.Groups)
                {
                    ProcessGroup(group, ediInfo, transformationMap, route, userNo, destinationConnector, sourceConnector, fileKey);
                }
            }
        }
       
        private static InboundEDIInfo CreateInboundEDIInfo(EdiInterchange interchange, int ediId, int userNo, string connectionString)
        {
            var ediInfo = new InboundEDIInfo();
            ediInfo.UseConnection(connectionString);

            ediInfo.InboundEDIId = ediId;
            ediInfo.ISASenderQual = interchange.ISA.Content[4].Val.ToString().Trim();
            ediInfo.ISASenderId = interchange.ISA.Content[5].Val.ToString().Trim();
            ediInfo.ISAReceiverQual = interchange.ISA.Content[6].Val.ToString().Trim();
            ediInfo.ISAReceiverId = interchange.ISA.Content[7].Val.ToString().Trim();
            ediInfo.ISAEdiVersion = interchange.ISA.Content[11].ToString().Trim();
            ediInfo.ISAUsageIndicator = interchange.ISA.Content[14].ToString().Trim();
            ediInfo.ISAControlNumber = interchange.ISA.Content[12].ToString().Trim();
            ediInfo.SegmentSeparator = interchange.SegmentSeparator;
            ediInfo.ElementSeparator = interchange.ElementSeparator;
            ediInfo.CreatedBy = userNo;
            ediInfo.CreatedDate = DateTime.Now;

            return ediInfo;
        }

        private static string GetTransformationMap(int mapId, string connectionString)
        {
            var map = new eSyncMate.DB.Entities.Maps();
            map.UseConnection(connectionString);
            map.GetObject(mapId);
            return map.Map;
        }

        private static object EvaluateJsonPath(JToken data, string path)
        {
            try
            {
                // Select the token using the JSONPath expression
                var result = data.SelectToken(path);
                return result?.ToString(); // Return the value as a string or null if not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating JSONPath '{path}': {ex.Message}");
                return null; // Return null in case of an error
            }
        }

        private static void ProcessGroup(EdiGroup group, InboundEDIInfo ediInfo, string transformationMap, Routes route, int userNo, ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, string fileKey)
        {
            foreach (var transaction in group.Transactions)
            {
                try
                {
                    PurchaseOrders l_PurchaseOrders = new PurchaseOrders();
                    var jsonData = JsonConvert.SerializeObject(transaction);
                    JObject transactionData = JObject.Parse(jsonData);
                    var transformedJson = new JsonTransformer().Transform(transformationMap, jsonData);

                    route.SaveData("JSON", 0, transformedJson, userNo);
                    route.SaveLog(LogTypeEnum.Info, $"Transformed JSON: {transformedJson}", string.Empty, userNo);

                    var receivedData = JsonConvert.DeserializeObject<_810TransformJson>(transformedJson);
                    if (receivedData == null || string.IsNullOrEmpty(receivedData.InvoiceNo))
                    {
                        route.SaveLog(LogTypeEnum.Warning, "Transformed data is null or InvoiceNo is missing.", string.Empty, userNo);
                        continue;
                    }

                    var headerId = SaveHeaderDataFromDB(destinationConnector.ConnectionString, transformedJson, userNo);

                    if (headerId > 0)
                    {
                        var detailList = ExtractDetails(transactionData);
                        SaveDetailData(destinationConnector.ConnectionString, headerId, detailList, receivedData.InvoiceNo, receivedData.PoNumber, receivedData.TrackingNo, userNo);
                    }

                    l_PurchaseOrders.UseConnection(destinationConnector.ConnectionString);
                    l_PurchaseOrders.GetObject("PONumber", receivedData.PoNumber);
                    if (l_PurchaseOrders != null)
                    {
                        l_PurchaseOrders.UpdatePoStatus(receivedData.PoNumber, "INVOICED", destinationConnector.ConnectionString);

                        string edi_997 = CarrierLoadTenderManager.Generate997For810(ediInfo, transaction, "810");

                        if (!string.IsNullOrEmpty(edi_997))
                        {
                            route.SaveData("INVOICED", 0, edi_997, userNo);
                            var method = "/outbound/997";
                            FtpConnector.Execute(sourceConnector.Host, sourceConnector.ConsumerKey, sourceConnector.ConsumerSecret, method, false, $"{Path.GetFileNameWithoutExtension(fileKey)}-997", edi_997).GetAwaiter().GetResult();
                        }
                    }
                }
                catch (Exception ex)
                {
                    route.SaveLog(LogTypeEnum.Exception, $"Error processing transaction [{transaction}]: {ex.Message}", ex.ToString(), userNo);
                }
            }
        }

        private static int SaveHeaderDataFromDB(string connectionString, string transformedJson, int userNo)
        {
            var receivedData = JsonConvert.DeserializeObject<_810TransformJson>(transformedJson);
            if (receivedData == null)
            {
                throw new Exception("Transformed data is null.");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"INSERT INTO SalesInvoiceNDC (InvoiceNo, InvoiceDate, PoNumber, Status, SCACCode, Routing, ShippingDate, 
                                           ShippingName, ShippingToNo, ShippingAddress1, ShippingAddress2, ShippingCity, 
                                           ShippingState, ShippingZip, ShippingCountry, InvoiceTerms, Frieght, 
                                           HandlingAmount, SalesTax, InvoiceAmount, TrackingNo, CreatedDate, CreatedBy)
                                            OUTPUT INSERTED.ID
                                            VALUES (@InvoiceNo, @InvoiceDate, @PoNumber, @Status, @SCACCode, @Routing, @ShippingDate, @ShippingName, 
                                                    @ShippingToNo, @ShippingAddress1, @ShippingAddress2, @ShippingCity, @ShippingState, @ShippingZip, 
                                                    @ShippingCountry, @InvoiceTerms, @Frieght, @HandlingAmount, @SalesTax, @InvoiceAmount, @TrackingNo, 
                                                    @CreatedDate, @CreatedBy)", connection);

                command.Parameters.AddWithValue("@InvoiceNo", receivedData.InvoiceNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@InvoiceDate", receivedData.InvoiceDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PoNumber", receivedData.PoNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", "NEW");
                command.Parameters.AddWithValue("@SCACCode", receivedData.SCACCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Routing", receivedData.Routing ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingDate", receivedData.ShippingDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingName", receivedData.ShippingName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingToNo", receivedData.ShippingToNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingAddress1", receivedData.ShippingAddress1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingAddress2", receivedData.ShippingFromAddress2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingCity", receivedData.ShippingCity ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingState", receivedData.ShippingState ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingZip", receivedData.ShippingZip ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingCountry", receivedData.ShippingCountry ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@InvoiceTerms", receivedData.InvoiceTerms ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Frieght", receivedData.Frieght ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@HandlingAmount", receivedData.HandlingAmount ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SalesTax", receivedData.SalesTax ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@InvoiceAmount", receivedData.InvoiceAmount ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TrackingNo", receivedData.TrackingNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                command.Parameters.AddWithValue("@CreatedBy", userNo);

                return (int)command.ExecuteScalar();
            }
        }

        private static List<dynamic> ExtractDetails(JObject transactionData)
        {
            var details = new List<dynamic>();
            var it1Segments = transactionData.SelectTokens("$.Content[?(@.Name=='L_IT1')]");

            foreach (var segment in it1Segments)
            {
                var it1 = segment["Content"].FirstOrDefault(s => (string)s["Name"] == "IT1");
                var pid = segment["Content"]
                    .FirstOrDefault(s => (string)s["Name"] == "L_PID")?["Content"]
                    .FirstOrDefault(s => (string)s["Name"] == "PID");

                if (it1 != null)
                {
                    details.Add(new
                    {
                        EDILineID = it1["Content"][0]?["E"]?.ToString(),
                        QTY = it1["Content"][1]?["E"]?.ToString(),
                        UOM = it1["Content"][2]?["E"]?.ToString(),
                        UnitPrice = it1["Content"][3]?["E"]?.ToString(),
                        ItemID = it1["Content"][6]?["E"]?.ToString(),
                        SKU = it1["Content"][6]?["E"]?.ToString(),
                        Description = pid?["Content"][4]?["E"]?.ToString()
                    });
                }
            }

            return details;
        }

        private static void SaveDetailData(string connectionString, int headerId, List<dynamic> details, string invoiceNo, string poNumber, string trackingNo, int userNo)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                foreach (var detail in details)
                {
                    var command = new SqlCommand(
                        @"INSERT INTO SalesInvoiceDetailNDC (SalesInvoice_ID, InvoiceNo,PoNumber, Status, EDILineID, QTY, UOM, UnitPrice, ItemID, SKU, TrackingNo ,Description, CreatedDate, CreatedBy)
                           VALUES (@HeaderID, @InvoiceNo,@PoNumber, @Status, @EDILineID, @QTY, @UOM, @UnitPrice, @ItemID, @SKU,@TrackingNo, @Description, @CreatedDate, @CreatedBy)",
                        connection);

                    command.Parameters.AddWithValue("@HeaderID", headerId);
                    command.Parameters.AddWithValue("@InvoiceNo", invoiceNo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PoNumber", poNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Status", "NEW");
                    command.Parameters.AddWithValue("@EDILineID", detail.EDILineID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@QTY", detail.QTY ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@UOM", detail.UOM ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@UnitPrice", detail.UnitPrice ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ItemID", detail.ItemID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SKU", detail.SKU ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TrackingNo", trackingNo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Description", detail.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@CreatedBy", userNo);

                    command.ExecuteNonQuery();
                }
            }
        }

        private static void SaveOrSendACK(string edi997, string documentType, ConnectorDataModel connector, int userNo)
        {
            
            //SaveACKToDatabase(edi997, documentType, userNo);
            //// Or send via FTP
            //FtpConnector.SendFile(connector.Host, connector.ConsumerKey, connector.ConsumerSecret, connector.Method, $"{documentType}_997.edi", edi997);
        }
    }
}
