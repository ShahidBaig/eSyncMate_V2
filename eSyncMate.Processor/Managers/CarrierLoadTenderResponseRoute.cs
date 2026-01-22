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

namespace eSyncMate.Processor.Managers
{
    public static class CarrierLoadTenderResponseRoute
    {
        public static void Execute(IConfiguration config, Routes route)
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
                    
                    route.SaveLog(LogTypeEnum.Exception, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    
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
                    route.SaveLog(LogTypeEnum.Exception, $"Required maps for 214 processing is missing.", string.Empty, userNo);
                    return;
                }

                CarrierLoadTender l_Carrier = new CarrierLoadTender();

                l_Carrier.UseConnection(l_SourceConnector.ConnectionString);

                List<CarrierLoadTender> l_Carriers = l_Carrier.GetAcknowledgedCLTs(route.DestinationPartyId);
                foreach (CarrierLoadTender carrier in l_Carriers)
                {
                    try
                    {
                        route.SaveLog(LogTypeEnum.Info, $"Started processing Carrier Load Tender Response [{carrier.Id}]-[{carrier.ShipperNo}].", string.Empty, userNo);
                        string left30ConsigneeCity = carrier.ConsigneeCity.Substring(0, Math.Min(30, carrier.ConsigneeCity.Length));

                        string jsonData = carrier.Files.Where(f => f.Type == "204-JSON").ToArray()[0].Data;
                        string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, jsonData);

                        jsonTransformation = jsonTransformation.Replace("@TRACKSTATUS@", carrier.TrackStatus);
                        jsonTransformation = jsonTransformation.Replace("@CITY@", carrier.ShipFromCity);
                        jsonTransformation = jsonTransformation.Replace("@STATE@", carrier.ShipFromState);
                        jsonTransformation = jsonTransformation.Replace("@COUNTRY@", carrier.ShipFromCountry);
                        jsonTransformation = jsonTransformation.Replace("@CARRIERCODE@", carrier.CarrierCode);

                        if (string.IsNullOrEmpty(carrier.ManualEquipmentNo))
                        {
                            jsonTransformation = jsonTransformation.Replace("@EQUIPMENTNO@", carrier.EquipmentNo);
                        }
                        else
                        {
                            jsonTransformation = jsonTransformation.Replace("@EQUIPMENTNO@", carrier.ManualEquipmentNo);
                        }

                        jsonTransformation = jsonTransformation.Replace("@ConsigneeName@", carrier.ConsigneeName);
                        jsonTransformation = jsonTransformation.Replace("@ConsigneeAddress@", carrier.ConsigneeAddress);
                        jsonTransformation = jsonTransformation.Replace("@ConsigneeCity@", left30ConsigneeCity);
                        jsonTransformation = jsonTransformation.Replace("@ConsigneeState@", carrier.ConsigneeState);
                        jsonTransformation = jsonTransformation.Replace("@ConsigneeZip@", carrier.ConsigneeZip);
                        jsonTransformation = jsonTransformation.Replace("@ConsigneeCountry@", carrier.ConsigneeCountry);

                        CarrierLoadTenderData l_OData = new CarrierLoadTenderData();

                        l_OData.UseConnection(l_SourceConnector.ConnectionString);

                        l_OData.DeleteWithType(carrier.Id, "214-JSON");

                        l_OData.Type = "214-JSON";
                        l_OData.Data = jsonTransformation;
                        l_OData.CreatedBy = userNo;
                        l_OData.CreatedDate = DateTime.Now;
                        l_OData.CarrierLoadTenderId = carrier.Id;

                        if (l_OData.SaveNew().IsSuccess)
                        {
                            CarrierLoadTenderResponse214 l_214 = JsonConvert.DeserializeObject<CarrierLoadTenderResponse214>(jsonTransformation);

                            Maps_4010.M_214 edimap = new Maps_4010.M_214();
                            EdiTrans t = new EdiTrans(edimap);

                            CarrierLoadTenderResponseRoute.AddEDISegments(edimap, t, l_214.StartNodes, "L_N1");

                            CarrierLoadTenderResponseRoute.AddEDISegments(edimap, t, l_214.Packs.Data, "L_LX|L_AT7");
                            //foreach(CLTPacks pack in l_214.Packs)
                            //{
                            //    CarrierLoadTenderResponseRoute.AddEDISegments(edimap, t, pack.Data, "L_LX|L_AT7");
                            //}

                            var g = new EdiGroup("QM");
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

                                //l_OData.DeleteWithType(carrier.Id, "214-EDI");

                                l_OData.Type = "214-EDI";
                                l_OData.Data = w.WriteToString(b);
                                l_OData.CreatedBy = userNo;
                                l_OData.CreatedDate = DateTime.Now;
                                l_OData.CarrierLoadTenderId = carrier.Id;

                                if (l_OData.SaveNew().IsSuccess)
                                {
                                    l_Outbound.Data = l_OData.Data;
                                    if (l_Outbound.Modify().IsSuccess)
                                    {
                                        SftpConnector.Execute(l_DestinationConnector, false, $"{carrier.ShipperNo}-214.edi", l_Outbound.Data).GetAwaiter().GetResult();

                                        carrier.UseConnection(l_SourceConnector.ConnectionString);

                                        if (carrier.TrackStatus == "D1" || carrier.TrackStatus == "X1")
                                        {
                                            carrier.UpdateTrackStatusForX1AndD1(carrier.Id, carrier.TrackStatus, carrier.ShipmentId, carrier.ShipperNo, carrier.ConsigneeName);
                                        }
                                        else
                                        {
                                            carrier.UpdateTrackStatus(carrier.Id, carrier.TrackStatus, carrier.ShipmentId, carrier.ConsigneeName);
                                        }

                                        route.SaveLog(LogTypeEnum.Info, $"Completed processing Carrier Load Tender [{carrier.Id}]-[{carrier.ShipperNo}].", string.Empty, userNo);
                                    }

                                    l_Carrier.CLTUpdateStatus();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        route.SaveLog(LogTypeEnum.Exception, $"Error processing Carrier Load Tender [{carrier.Id}]-[{carrier.ShipperNo}].", ex.ToString(), userNo);
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        private static void AddEDISegments(Maps_4010.M_214 p_Map, EdiTrans p_Trans, List<SegmentNode> p_Nodes, string p_ParentNodeName)
        {
            foreach (SegmentNode l_Node in p_Nodes)
            {
                MapSegment l_SegmentDef = null;

                try
                {
                    if (string.IsNullOrEmpty(p_ParentNodeName))
                        l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                    else
                    {
                        string[] l_ParentNodeNames = p_ParentNodeName.Split("|".ToCharArray());

                        try
                        {
                            l_SegmentDef = (MapSegment)((MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0])).Content.First(s => s.Name == l_Node.Name);
                        }
                        catch (InvalidOperationException)
                        {
                            if (l_ParentNodeNames.Length > 1)
                            {
                                int l_index = 1;
                                while (l_index < l_ParentNodeNames.Length)
                                {
                                    try
                                    {
                                        l_SegmentDef = (MapSegment)((MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[l_index])).Content.First(s => s.Name == l_Node.Name);
                                        break;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                    }

                                    l_index++;
                                }

                                if (l_SegmentDef == null)
                                {
                                    l_index = 1;
                                    MapLoop l_MapLoop = (MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0]);
                                    while (l_index < l_ParentNodeNames.Length)
                                    {
                                        try
                                        {
                                            l_SegmentDef = (MapSegment)(CarrierLoadTenderResponseRoute.FindMapLoopDef(l_MapLoop, l_ParentNodeNames[l_index])).Content.First(s => s.Name == l_Node.Name);
                                            break;
                                        }
                                        catch (InvalidOperationException)
                                        {
                                        }

                                        l_index++;
                                    }
                                }
                            }
                        }

                        if (l_SegmentDef == null)
                        {
                            l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                }

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

        private static MapLoop FindMapLoopDef(MapLoop p_Map, string p_ParentNodeName)
        {
            MapLoop l_MapLoopDef = null;

            l_MapLoopDef = (MapLoop)p_Map.Content.First(s => s.Name == p_ParentNodeName);

            return l_MapLoopDef;
        }
    }
}
