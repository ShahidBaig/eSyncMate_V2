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
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;
using SegmentDefinitions = EdiEngine.Standards.X12_005010.Segments;
using EdiEngine.Common.Definitions;
using System.Data;

namespace eSyncMate.Processor.Managers
{
    public static class CarrierLoadTenderAcknowledgmentRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;

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

                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                string l_TransformationMap = string.Empty;

                map.UseConnection(l_SourceConnector.ConnectionString);
                map.GetObject(route.MapId);

                l_TransformationMap = map.Map;

                if (string.IsNullOrEmpty(l_TransformationMap))
                {
                    route.SaveLog(LogTypeEnum.Exception, $"Required maps for 990 processing is missing.", string.Empty, userNo);
                    return;
                }

                CarrierLoadTender l_Carrier = new CarrierLoadTender();

                l_Carrier.UseConnection(l_SourceConnector.ConnectionString);

                List<CarrierLoadTender> l_Carriers = l_Carrier.GetUnacknowledgedCLTs(route.DestinationPartyId);
                foreach (CarrierLoadTender carrier in l_Carriers)
                {
                    try
                    {
                        if (carrier.Status != "NEW")
                            continue;

                        route.SaveLog(LogTypeEnum.Info, $"Started processing Carrier Load Tender [{carrier.Id}]-[{carrier.ShipmentId}].", string.Empty, userNo);

                        string jsonData = carrier.Files.Where(f => f.Type == "204-JSON").ToArray()[0].Data;
                        string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, jsonData);

                        //jsonTransformation = jsonTransformation.Replace("@STATUS@", carrier.AckStatus == "ACCEPT" ? "A" : "D");
                        //jsonTransformation = jsonTransformation.Replace("@STATUSCODE@", carrier.AckStatus == "ACCEPT" ? "ACC" : "REJ");
                        //jsonTransformation = jsonTransformation.Replace("@STATUSCODE2@", carrier.AckStatus == "ACCEPT" ? "" : "RUN");
                        
                        jsonTransformation = jsonTransformation.Replace("@STATUS@", "A");
                        jsonTransformation = jsonTransformation.Replace("@STATUSCODE@", "ACC");
                        jsonTransformation = jsonTransformation.Replace("@STATUSCODE2@", "");

                        CarrierLoadTenderData l_OData = new CarrierLoadTenderData();

                        l_OData.UseConnection(l_SourceConnector.ConnectionString);

                        l_OData.DeleteWithType(carrier.Id, "990-JSON");

                        l_OData.Type = "990-JSON";
                        l_OData.Data = jsonTransformation;
                        l_OData.CreatedBy = userNo;
                        l_OData.CreatedDate = DateTime.Now;
                        l_OData.CarrierLoadTenderId = carrier.Id;

                        if (l_OData.SaveNew().IsSuccess)
                        {
                            CarrierLoadTenderAckowledgement990 l_990 = JsonConvert.DeserializeObject<CarrierLoadTenderAckowledgement990>(jsonTransformation);

                            Maps_4010.M_990 edimap = new Maps_4010.M_990();
                            EdiTrans t = new EdiTrans(edimap);

                            CarrierLoadTenderAcknowledgmentRoute.AddEDISegments(edimap, t, l_990.StartNodes);

                            var g = new EdiGroup("GF");
                            g.Transactions.Add(t);

                            var i = new EdiInterchange();
                            i.Groups.Add(g);

                            EdiBatch b = new EdiBatch();
                            b.Interchanges.Add(i);

                            OutboundEDI l_Outbound = new OutboundEDI();

                            l_Outbound.UseConnection(l_SourceConnector.ConnectionString);

                            l_Outbound.Status = "NEW";
                            l_Outbound.Data = string.Empty;
                            l_Outbound.CreatedBy = userNo;
                            l_Outbound.CreatedDate = DateTime.Now;
                            l_Outbound.OrderId = carrier.Id;

                            if (l_Outbound.SaveNew().IsSuccess)
                            {
                                InboundEDIInfo l_EDIInfo = carrier.Inbound.Info[0];
                                EdiDataWriterSettings settings = new EdiDataWriterSettings(
                                new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                                new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                                new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                                l_EDIInfo.ISAReceiverQual, l_EDIInfo.ISAReceiverId, l_EDIInfo.ISASenderQual, l_EDIInfo.ISASenderId, l_EDIInfo.GSReceiverId, l_EDIInfo.GSSenderId,
                                l_EDIInfo.ISAEdiVersion, l_EDIInfo.GSEdiVersion, l_EDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, l_EDIInfo.SegmentSeparator, l_EDIInfo.ElementSeparator, "U", "}");

                                EdiDataWriter w = new EdiDataWriter(settings);

                                l_OData = new CarrierLoadTenderData();

                                l_OData.UseConnection(l_SourceConnector.ConnectionString);

                                l_OData.DeleteWithType(carrier.Id, "990-EDI");

                                l_OData.Type = "990-EDI";
                                l_OData.Data = w.WriteToString(b);
                                l_OData.CreatedBy = userNo;
                                l_OData.CreatedDate = DateTime.Now;
                                l_OData.CarrierLoadTenderId = carrier.Id;

                                if (l_OData.SaveNew().IsSuccess)
                                {
                                    l_Outbound.Data = l_OData.Data;
                                    if (l_Outbound.Modify().IsSuccess)
                                    {
                                        SftpConnector.Execute(l_DestinationConnector, false, $"{carrier.ShipmentId}-990.edi", l_Outbound.Data).GetAwaiter().GetResult();

                                        carrier.UseConnection(l_SourceConnector.ConnectionString);
                                        DataTable l_Data = new DataTable();
                                        DataTable l_CData = new DataTable();

                                        l_Carrier.GetViewList($"ShipmentId = '{carrier.ShipmentId}'","",ref l_Data);

                                        carrier.UpdateAckStatusForAllShipmentId(carrier.ShipmentId, "ACK");

                                        carrier.GetViewList($"ShipmentId = '{carrier.ShipmentId}' AND [Status] = 'ACK'", string.Empty, ref l_CData, "Id DESC");

                                        foreach (DataRow row in l_CData.Rows)
                                        {
                                            CarrierLoadTender.InsertMySqlData(row["CustomerName"].ToString(), row["ShipperNo"].ToString(), row["ShipmentId"].ToString(), row["EquipmentNo"].ToString(), row["ConsigneeName"].ToString(), row["LastConsigneeName"].ToString(), CommonUtils.MySqlConnectionString);
                                        }

                                        List<CarrierLoadTender> l_Cs = l_Carriers.Where(c => c.ShipmentId == carrier.ShipmentId).ToList<CarrierLoadTender>();
                                        foreach (CarrierLoadTender l_C in l_Cs)
                                        {
                                            l_C.Status = "ACK";
                                        }

                                        route.SaveLog(LogTypeEnum.Info, $"Completed processing Carrier Load Tender [{carrier.Id}]-[{carrier.ShipmentId}].", string.Empty, userNo);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        route.SaveLog(LogTypeEnum.Exception, $"Error processing Carrier Load Tender [{carrier.Id}]-[{carrier.ShipmentId}].", ex.ToString(), userNo);
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        private static void AddEDISegments(Maps_4010.M_990 p_Map, EdiTrans p_Trans, List<SegmentNode> p_Nodes)
        {
            foreach (SegmentNode l_Node in p_Nodes)
            {
                MapSegment l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);

                if (l_SegmentDef != null)
                {
                    int l_Index = 0;
                    var l_Segment = new EdiSegment(l_SegmentDef);

                    foreach (string l_Value in l_Node.Data)
                    {
                        l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                    }

                    p_Trans.Content.Add(l_Segment);
                }
            }
        }
    }
}
