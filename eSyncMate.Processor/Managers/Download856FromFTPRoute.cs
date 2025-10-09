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
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using static eSyncMate.Processor.Models.MacysGetOrderResponseModel;
using System.Net.Http.Headers;
using System.Text;

namespace eSyncMate.Processor.Managers
{
    public class Download856FromFTPRoute
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

            edi.Type = "856";
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
                route.SaveLog(LogTypeEnum.Exception, $"Required maps for 856 processing are missing.", string.Empty, userNo);
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

            foreach (EdiGroup g in interchange.Groups)
            {
                ediInfo.GSSenderId = g.GS.Content[1].ToString().Trim();
                ediInfo.GSReceiverId = g.GS.Content[2].ToString().Trim();
                ediInfo.GSControlNumber = g.GS.Content[5].ToString().Trim();
                ediInfo.GSEdiVersion = g.GS.Content[7].ToString().Trim();

                ediInfo.SaveNew();
            }

            return ediInfo;
        }

        private static string GetTransformationMap(int mapId, string connectionString)
        {
            var map = new eSyncMate.DB.Entities.Maps();
            map.UseConnection(connectionString);
            map.GetObject(mapId);
            return map.Map;
        }

        private static void ProcessGroup(EdiGroup group, InboundEDIInfo ediInfo, string transformationMap, Routes route, int userNo, ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, string fileKey)
        {
            var headerMap = JObject.Parse(transformationMap).ToObject<Dictionary<string, string>>();
            string connectionString = destinationConnector.ConnectionString;

            foreach (var transaction in group.Transactions)
            {
                try
                {
                    PurchaseOrders l_PurchaseOrders = new PurchaseOrders();
                    var jsonData = JsonConvert.SerializeObject(transaction);
                    JObject transactionData = JObject.Parse(jsonData);
                    var transformedJson = new JsonTransformer().Transform(transformationMap, jsonData);

                    var result = new Dictionary<string, object>();
                    foreach (var field in headerMap)
                    {
                        result[field.Key] = EvaluateJsonPath(transactionData, field.Value);
                    }

                    string shipmentID = result["ShipmentID"]?.ToString();
                    string warehouseName = result["ShippingName"]?.ToString();
                    string PoNumber = result["PoNumber"]?.ToString();
                    var detailList = new List<dynamic>();

                    void TraverseContent(dynamic content, string currentBOLNo, string currentSSCC)
                    {
                        foreach (var segment in content)
                        {
                            if (segment.Type == "L" && segment.Name == "L_HL")
                            {
                                dynamic hlSegment = segment.Content[0];
                                if (hlSegment.Name == "HL")
                                {
                                    string hlCode = hlSegment.Content[2]?.E?.ToString();

                                    if (hlCode == "P")
                                    {
                                        string bolNo = currentBOLNo;
                                        string sscc = currentSSCC;

                                        foreach (var subSegment in segment.Content)
                                        {
                                            if (subSegment.Name == "REF" && subSegment.Content != null && subSegment.Content[0]?.E?.ToString() == "BM")
                                            {
                                                bolNo = subSegment.Content[1]?.E?.ToString() ?? bolNo;
                                            }
                                            else if (subSegment.Name == "MAN" && subSegment.Content != null && subSegment.Content[0]?.E?.ToString() == "GM")
                                            {
                                                sscc = subSegment.Content[1]?.E?.ToString() ?? sscc;
                                            }
                                        }

                                        if (segment.Content != null)
                                        {
                                            TraverseContent(segment.Content, bolNo, sscc);
                                        }
                                    }
                                    else if (hlCode == "I")
                                    {
                                        string itemId = null;
                                        string sku = null;
                                        string qty = null;
                                        string uom = null;
                                        string lotNumber = null;
                                        string ExpirationDate = null;

                                        foreach (var subSegment in segment.Content)
                                        {
                                            if (subSegment.Name == "LIN" && subSegment.Content != null)
                                            {
                                                itemId = subSegment.Content[2]?.E?.ToString();
                                                sku = itemId;
                                            }
                                            else if (subSegment.Name == "SN1" && subSegment.Content != null)
                                            {
                                                qty = subSegment.Content[1]?.E?.ToString();
                                                uom = subSegment.Content[2]?.E?.ToString();
                                            }
                                            else if (subSegment.Name == "REF" && subSegment.Content != null && subSegment.Content[0]?.E?.ToString() == "LT")
                                            {
                                                lotNumber = subSegment.Content[1]?.E?.ToString();
                                            }
                                            else if (subSegment.Name == "DTM" && subSegment.Content != null && subSegment.Content[0]?.E?.ToString() == "036")
                                            {
                                                ExpirationDate = subSegment.Content[1]?.E?.ToString();
                                            }
                                        }

                                        detailList.Add(new
                                        {
                                            BOLNo = currentBOLNo,
                                            SSCC = currentSSCC,
                                            ItemID = itemId,
                                            SKU = sku,
                                            QTY = qty,
                                            UOM = uom,
                                            LotNumber = lotNumber,
                                            ExpirationDate = ExpirationDate
                                        });

                                        if (segment.Content != null)
                                        {
                                            TraverseContent(segment.Content, currentBOLNo, currentSSCC);
                                        }
                                    }
                                    else
                                    {
                                        if (segment.Content != null)
                                        {
                                            TraverseContent(segment.Content, currentBOLNo, currentSSCC);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    TraverseContent(transactionData["Content"], null, null);
                    int headerId = SaveHeaderData(result, connectionString, userNo);
                    SaveDetailData(headerId, detailList, shipmentID, warehouseName, PoNumber, connectionString, userNo);

                    var finalJson = JsonConvert.SerializeObject(result, Formatting.Indented);
                    route.SaveData("JSON", 0, finalJson, userNo);
                    route.SaveLog(LogTypeEnum.Info, $"Final transformed JSON: {finalJson}", string.Empty, userNo);
                    l_PurchaseOrders.UseConnection(destinationConnector.ConnectionString);
                    l_PurchaseOrders.GetObject("PONumber", PoNumber);
                    if (l_PurchaseOrders != null)
                    {
                        l_PurchaseOrders.UpdatePoStatus(PoNumber, "RECEIVED", destinationConnector.ConnectionString);
                        string edi_997 = CarrierLoadTenderManager.Generate997For856(ediInfo, transaction, "856");

                        if (!string.IsNullOrEmpty(edi_997))
                        {
                            route.SaveData("RECEIVED", 0, edi_997, userNo);
                            var method = "/outbound/997";
                            FtpConnector.Execute(sourceConnector.Host, sourceConnector.ConsumerKey, sourceConnector.ConsumerSecret, method, false, $"{Path.GetFileNameWithoutExtension(fileKey)}-997", edi_997).GetAwaiter().GetResult();
                        }
                    }
                }
                catch (Exception ex)
                {
                    route.SaveLog(LogTypeEnum.Exception, $"Error processing transaction [{transaction}]", ex.ToString(), userNo);
                }
            }
        }
        private static object EvaluateJsonPath(JToken data, string path)
        {
            try
            {
                var result = data.SelectToken(path);
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static int SaveHeaderData(Dictionary<string, object> headerData, string connectionString, int userNo)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"INSERT INTO ShipmentFromNDC (ShipmentID, TransactionDate, PoNumber, PoDate, Status, SCACCode, Routing, ShippingDate, N1_ShipID, ShippingName, 
                      ShippingAddress1, ShippingCity, ShippingState, ShippingZip, ShippingCountry, SellerID, ShippingFromName, ShippingFromAddress1, 
                      ShippingFromAddress2, ShippingFromCity, ShippingFromState, ShippingFromZip, ShippingFromCountry, CreatedDate, CreatedBy) 
                      OUTPUT INSERTED.ID 
                      VALUES (@ShipmentID, @TransactionDate, @PoNumber, @PoDate, @Status, @SCACCode, @Routing, @ShippingDate, @N1_ShipID, @ShippingName, 
                      @ShippingAddress1, @ShippingCity, @ShippingState, @ShippingZip, @ShippingCountry, @SellerID, @ShippingFromName, @ShippingFromAddress1, 
                      @ShippingFromAddress2, @ShippingFromCity, @ShippingFromState, @ShippingFromZip, @ShippingFromCountry, @CreatedDate, @CreatedBy)", connection);

                command.Parameters.AddWithValue("@ShipmentID", headerData["ShipmentID"] ?? (object)DBNull.Value);
                DateTime transDate;
                command.Parameters.AddWithValue("@TransactionDate",
                    DateTime.TryParseExact(headerData["TransactionDate"]?.ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out transDate)
                    ? (object)transDate
                    : DBNull.Value);
                command.Parameters.AddWithValue("@PoNumber", headerData["PoNumber"] ?? (object)DBNull.Value);
                DateTime poDate;
                command.Parameters.AddWithValue("@PoDate",
                    DateTime.TryParseExact(headerData["PoDate"]?.ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out poDate)
                    ? (object)poDate
                    : DBNull.Value);
                command.Parameters.AddWithValue("@Status", "NEW");
                command.Parameters.AddWithValue("@SCACCode", headerData["SCACCode"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Routing", headerData["Routing"] ?? (object)DBNull.Value);
                DateTime shipDate;
                command.Parameters.AddWithValue("@ShippingDate",
                    DateTime.TryParseExact(headerData["ShippingDate"]?.ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out shipDate)
                    ? (object)shipDate
                    : DBNull.Value);
                command.Parameters.AddWithValue("@N1_ShipID", headerData["N1_ShipID"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingName", headerData["ShippingName"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingAddress1", headerData["ShippingAddress1"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingCity", headerData["ShippingCity"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingState", headerData["ShippingState"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingZip", headerData["ShippingZip"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingCountry", headerData["ShippingCountry"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SellerID", headerData["SellerID"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingFromName", headerData["ShippingFromName"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingFromAddress1", headerData["ShippingFromAddress1"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingFromAddress2", headerData["ShippingFromAddress2"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingFromCity", headerData["ShippingFromCity"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingFromState", headerData["ShippingFromState"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingFromZip", headerData["ShippingFromZip"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ShippingFromCountry", headerData["ShippingFromCountry"] ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                command.Parameters.AddWithValue("@CreatedBy", userNo);

                int headerId = (int)command.ExecuteScalar();
                return headerId;
            }
        }

        private static void SaveDetailData(int headerId, List<dynamic> details, string shipmentID, string warehouseName, string poNumber, string connectionString, int userNo)
        {
            var newRecords = new List<dynamic>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var detail in details)
                {
                    var command = new SqlCommand(
                        @"INSERT INTO ShipmentDetailFromNDC 
                        (ShipmentFromNDC_ID, ShipmentID, PoNumber, Status, BOLNO, SSCC, ItemID, SKU, QTY, UOM, WarehouseName, CreatedDate, CreatedBy, ExpirationDate, LotNumber, ShipStationStatus) 
                        OUTPUT INSERTED.WarehouseName, INSERTED.ItemID, INSERTED.QTY
                        VALUES (@HeaderID, @ShipmentID, @PoNumber, @Status, @BOLNO, @SSCC, @ItemID, @SKU, @QTY, @UOM, @WarehouseName, @CreatedDate, @CreatedBy, @ExpirationDate, @LotNumber, @ShipStationStatus)",
                                connection);

                    command.Parameters.AddWithValue("@HeaderID", headerId);
                    command.Parameters.AddWithValue("@ShipmentID", shipmentID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PoNumber", poNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Status", "NEW");
                    command.Parameters.AddWithValue("@ShipStationStatus", "NEW");
                    command.Parameters.AddWithValue("@BOLNO", detail.BOLNo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SSCC", detail.SSCC ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ItemID", detail.ItemID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SKU", detail.SKU ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@QTY", detail.QTY ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@UOM", detail.UOM ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@LotNumber", detail.LotNumber ?? (object)DBNull.Value);
                    DateTime expirationDate;
                    command.Parameters.AddWithValue("@ExpirationDate",
                        DateTime.TryParseExact(detail.ExpirationDate?.ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out expirationDate)
                        ? (object)expirationDate
                        : DBNull.Value);
                    command.Parameters.AddWithValue("@WarehouseName", warehouseName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@CreatedBy", userNo);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            newRecords.Add(new
                            {
                                WarehouseName = reader["WarehouseName"],
                                ItemID = reader["ItemID"],
                                QTY = reader["QTY"]
                            });
                        }
                    }
                }

                if (newRecords.Any())
                {
                    DataTable newDetailsTable = new DataTable();
                    newDetailsTable.Columns.Add("WarehouseName", typeof(string));
                    newDetailsTable.Columns.Add("ItemID", typeof(string));
                    newDetailsTable.Columns.Add("Qty", typeof(decimal));

                    foreach (var record in newRecords)
                    {
                        newDetailsTable.Rows.Add(record.WarehouseName, record.ItemID, record.QTY);
                    }

                    using (var spCommand = new SqlCommand("sp_SaveUpdateVeeqoOrderDetails", connection))
                    {
                        spCommand.CommandType = CommandType.StoredProcedure;

                        SqlParameter tableParam = new SqlParameter("@OrderDetails", SqlDbType.Structured)
                        {
                            TypeName = "dbo.VeeqoOrderDetailsType856",
                            Value = newDetailsTable
                        };
                        spCommand.Parameters.Add(tableParam);

                        spCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
