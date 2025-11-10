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
using EdiEngine.Common.Definitions;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;
using SegmentDefinitions = EdiEngine.Standards.X12_004010.Segments;

namespace eSyncMate.Processor.Managers
{
    public static class RepaintGetOrderRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            DataTable l_Data = new DataTable();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
                string destinationData = string.Empty;
                string sourceData = string.Empty;
                string transformedData = string.Empty;

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    logger.LogError("Source Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Exception, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    logger.LogError("Destination Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Exception, "Destination Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.AuthType == ConnectorTypesEnum.SFTP.ToString())
                {
                    Dictionary<string, string> files = SftpConnector.Execute(l_SourceConnector).GetAwaiter().GetResult();

                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file.Key);
                        string fileContent = file.Value;

                        IFormFile formFile = CreateFormFileFromString(fileContent, fileName);

                        RepaintGetOrderRoute.ProcessEDIFile(formFile,l_SourceConnector,route).GetAwaiter().GetResult();
                    }

                    route.SaveLog(LogTypeEnum.Info, $"Route data received!", string.Empty, userNo);
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_Data.Dispose();
            }
        }


        public static async Task ProcessEDIFile(IFormFile file, ConnectorDataModel connection, Routes route)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            string l_TransformationMap = string.Empty;
            string l_DBFieldsMap = string.Empty;
            string l_EDIData = string.Empty;
            int l_SystemUser = 1;

            OrderResponseModel l_Response = new OrderResponseModel();
            Customers l_Customer = new Customers();
            EdiDataReader r = new EdiDataReader();
            InboundEDI l_EDI = new InboundEDI();

            l_Response.Code = (int)ResponseCodes.Error;
            InboundEDIInfo l_EDIInfo = new InboundEDIInfo();

            try
            {
                if (file.Length <= 0)
                {
                    l_Response.Message = "Please provide a EDI data file";
                    route.SaveLog(LogTypeEnum.Exception, l_Response.Message, string.Empty, 1);
                    return;
                }

                using (StreamReader reader = new StreamReader(file.OpenReadStream()))
                {
                    l_EDIData = await reader.ReadToEndAsync();
                }

                l_EDI.UseConnection(CommonUtils.ConnectionString);

                l_EDI.Type = "850";
                l_EDI.Status = "NEW";
                l_EDI.Data = l_EDIData;
                l_EDI.CreatedBy = l_SystemUser;
                l_EDI.CreatedDate = DateTime.Now;

                l_EDI.SaveNew();


                EdiBatch b = r.FromString(l_EDIData);

                l_Customer.UseConnection(CommonUtils.ConnectionString);


                foreach (EdiInterchange i in b.Interchanges)
                {
                    if (!l_Customer.GetObject("ISACustomerID", connection.Realm.ToString().Trim()).IsSuccess)
                    {

                        l_EDI.Status = "ERROR";
                        l_EDI.Modify();

                        l_Response.Message = $"The party [{i.ISA.Content[5].Val.ToString().Trim()}] is not registered";
                        //return l_Response;
                        route.SaveLog(LogTypeEnum.Exception, l_Response.Message, string.Empty, 1);
                        return;
                    }

                    if (i.ISA.Content[5].Val.ToString().Trim() != connection.Realm.ToString())
                    {
                        continue;
                    }


                    var l_Map = l_Customer.Maps.Where(p => p.MapTypeName == "850 Transformation");

                    if (l_Map != null)
                    {
                        l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                    }

                    l_Map = l_Customer.Maps.Where(p => p.MapTypeName == "850 DB Fields");

                    if (l_Map != null)
                    {
                        l_DBFieldsMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                    }

                    if (string.IsNullOrEmpty(l_TransformationMap) || string.IsNullOrEmpty(l_DBFieldsMap))
                    {

                        l_EDI.Status = "ERROR";
                        l_EDI.Modify();

                        l_Response.Message = $"Required maps for 850 processing are missing for [{l_Customer.Name}]";

                        route.SaveLog(LogTypeEnum.Exception, l_Response.Message, string.Empty, 1);
                        return;
                    }


                    if (i.Groups[0].GS.Content[0].ToString().Trim() != "PO")
                    {
                        continue;
                    }

                    l_EDIInfo = new InboundEDIInfo();

                    l_EDIInfo.InboundEDIId = l_EDI.Id;
                    l_EDIInfo.ISASenderQual = i.ISA.Content[4].Val.ToString().Trim();
                    l_EDIInfo.ISASenderId = i.ISA.Content[5].Val.ToString().Trim();
                    l_EDIInfo.ISAReceiverQual = i.ISA.Content[6].Val.ToString().Trim();
                    l_EDIInfo.ISAReceiverId = i.ISA.Content[7].Val.ToString().Trim();
                    l_EDIInfo.ISAEdiVersion = i.ISA.Content[11].ToString().Trim();
                    l_EDIInfo.ISAUsageIndicator = i.ISA.Content[14].ToString().Trim();
                    l_EDIInfo.ISAControlNumber = i.ISA.Content[12].ToString().Trim();
                    l_EDIInfo.SegmentSeparator = i.SegmentSeparator;
                    l_EDIInfo.ElementSeparator = i.ElementSeparator;
                    l_EDIInfo.CreatedBy = l_SystemUser;
                    l_EDIInfo.CreatedDate = DateTime.Now;

                    l_EDIInfo.UseConnection(CommonUtils.ConnectionString);

                    l_Response.Orders = new List<OrderModel>();

                    foreach (EdiGroup g in i.Groups)
                    {

                        l_EDIInfo.GSSenderId = g.GS.Content[1].ToString().Trim();
                        l_EDIInfo.GSReceiverId = g.GS.Content[2].ToString().Trim();
                        l_EDIInfo.GSControlNumber = g.GS.Content[5].ToString().Trim();
                        l_EDIInfo.GSEdiVersion = g.GS.Content[7].ToString().Trim();

                        l_EDIInfo.SaveNew();

                        SftpConnector.DeleteFileFromSFTP(file.FileName, connection).GetAwaiter().GetResult();

                        foreach (EdiTrans t in g.Transactions)
                        {
                            OrderTransformationResponseModel l_OrderData = OrderManager.ParseOrder(l_Customer, t, l_TransformationMap, l_DBFieldsMap);

                            if (l_OrderData.Code != (int)ResponseCodes.Success)
                            {

                                l_Response.Code = l_OrderData.Code;
                                l_Response.Message = l_OrderData.Message;

                                route.SaveLog(LogTypeEnum.Exception, l_Response.Message, string.Empty, 1);
                                return;
                            }

                            l_OrderData.EDI = l_EDIData;
                            l_OrderData.SystemUser = l_SystemUser;


                            OrderSaveResponseModel l_OrderResponse = OrderManager.SaveOrder(l_EDI, l_Customer, l_OrderData);

                            if (l_OrderResponse.Code != (int)ResponseCodes.Success)
                            {

                                l_Response.Code = l_OrderResponse.Code;
                                l_Response.Message = l_OrderResponse.Message;

                            }

                            string edi_997 = RepaintGetOrderRoute.Generate997(l_EDIInfo, l_OrderResponse, t);

                            if (!string.IsNullOrEmpty(edi_997))
                            {
                                route.SaveData("ACK", 0, edi_997, 1);

                                connection.BaseUrl = connection.Url;
                                SftpConnector.Execute(connection, false, $"{Path.GetFileNameWithoutExtension(file.Name)}-997", edi_997).GetAwaiter().GetResult();
                            }
                        }
                    }
                }

                //route.SaveLog(LogTypeEnum.Exception, "Destination Connector is not setup properly", string.Empty, 1);

                l_EDI.Status = "PROCESSED";
                l_EDI.Modify();

            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;

                l_EDI.Status = "EXCEPTION";
                l_EDI.Modify();

            }
            finally
            {
                
            }
        }
        

        public static IFormFile CreateFormFileFromString(string content, string fileName)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);

            return new FormFile(stream, 0, bytes.Length, "file", fileName);
        }

        public static string Generate997(InboundEDIInfo inboundEDIInfo, OrderSaveResponseModel l_OrderSaveResponseModel, EdiTrans ctlTrans)
        {
            string edi_997 = string.Empty;

            Maps_4010.M_997 map = new Maps_4010.M_997();
            EdiTrans t = new EdiTrans(map);
            MapSegment l_SegmentDef = null;

            //AK1
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK1");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "PO", inboundEDIInfo.GSControlNumber })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK2
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK2");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "850", ctlTrans.ST.Content[1].Val })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK5
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK5");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK9
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK9");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A", "1", "1", "1" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            var g = new EdiGroup("FA");
            g.Transactions.Add(t);

            var i = new EdiInterchange();
            i.Groups.Add(g);

            EdiBatch b = new EdiBatch();
            b.Interchanges.Add(i);

            OutboundEDI l_Outbound = new OutboundEDI();

            l_Outbound.UseConnection(string.Empty, inboundEDIInfo.Connection);

            l_Outbound.Status = "NEW";
            l_Outbound.Data = string.Empty;
            l_Outbound.CreatedBy = inboundEDIInfo.CreatedBy;
            l_Outbound.CreatedDate = DateTime.Now;
            l_Outbound.OrderId = l_OrderSaveResponseModel.OrderId;

            if (l_Outbound.SaveNew().IsSuccess)
            {
                EdiDataWriterSettings settings = new EdiDataWriterSettings(
                new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                inboundEDIInfo.ISAReceiverQual, inboundEDIInfo.ISAReceiverId, inboundEDIInfo.ISASenderQual, inboundEDIInfo.ISASenderId, inboundEDIInfo.GSReceiverId, inboundEDIInfo.GSSenderId,
                inboundEDIInfo.ISAEdiVersion, inboundEDIInfo.GSEdiVersion, inboundEDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, inboundEDIInfo.SegmentSeparator, inboundEDIInfo.ElementSeparator, "U", ">");

                EdiDataWriter w = new EdiDataWriter(settings);

                OrderData l_OData = new OrderData();

                l_OData.UseConnection(string.Empty, inboundEDIInfo.Connection);

                l_OData.DeleteWithType(l_OrderSaveResponseModel.OrderId, "997-EDI");

                l_OData.Type = "997-EDI";
                l_OData.Data = w.WriteToString(b);
                l_OData.CreatedBy = inboundEDIInfo.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.OrderId = l_OrderSaveResponseModel.OrderId;
                l_OData.OrderNumber = "";
                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Outbound.Data = l_OData.Data;
                    l_Outbound.Modify();

                    edi_997 = l_Outbound.Data;
                }
            }

            return edi_997;
        }
    }
}
