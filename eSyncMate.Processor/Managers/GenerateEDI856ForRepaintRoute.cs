using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Intercom.Core;
using Intercom.Data;
using JUST;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators;
using System.Data;
using static eSyncMate.DB.Declarations;
using static eSyncMate.Processor.Models.SCSPlaceOrderResponseModel;
using Nancy;
using static eSyncMate.Processor.Models.MacysAsnRequestModel;
using EdiEngine.Runtime;
using EdiEngine;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using EdiEngine.Runtime;
using JUST;
using Namotion.Reflection;
using Nancy;
using Nancy.Responses;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators;
using System.Reflection.PortableExecutable;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;
using Maps_4010VICS = EdiEngine.Standards.X12_004010VICS.Maps;
using Maps_5010 = EdiEngine.Standards.X12_005010.Maps;
using EdiEngine.Common.Definitions;
using EdiEngine;
using SegmentDefinitions = EdiEngine.Standards.X12_005010.Segments;
using Nancy.Diagnostics;
using System.Linq;
using System.Data;
using eSyncMate.DB;
using System.Reflection;
using eSyncMate.Processor.Controllers;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.Xml;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using Hangfire.Storage;


namespace eSyncMate.Processor.Managers
{
    public class GenerateEDI856ForRepaintRoute
    {
        
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_dataTable = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            SCSPlaceOrderResponse l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();
            OrderObjectModel? l_Order = null;

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

                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                string l_TransformationMap = string.Empty;

                map.UseConnection(l_SourceConnector.ConnectionString);
                map.GetObject(route.MapId);

                l_TransformationMap = map.Map;

                if (string.IsNullOrEmpty(l_TransformationMap))
                {
                    route.SaveLog(LogTypeEnum.Error, $"Required map for 855 processing is missing.", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    if (l_SourceConnector.Parmeters != null)
                    {
                        foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                        {
                            l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                        }
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@DATATYPE@", "ASN-RES");
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", "ACKNOWLEDGED");

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_dataTable);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_dataTable.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start...", string.Empty, userNo);

                    foreach (DataRow item in l_dataTable.Rows)
                    {
                        //var first = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(item["Data"]));

                        string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, item["Data"].ToString());

                        l_Order = OrderManager.GetOrder(Convert.ToInt32(item["Id"])).GetAwaiter().GetResult();

                        InboundEDIInfo l_EDIInfo = l_Order.Order.Inbound.Info[0];

                        Maps_5010.M_856 l_map = new Maps_5010.M_856();
                        EdiTrans t = new EdiTrans(l_map);
                        int l_HLIndex = 1, l_HL_PIndex = 1;

                        ASN l_ASN = JsonConvert.DeserializeObject<ASN>(jsonTransformation);

                        GenerateEDI856ForRepaintRoute.AddASNSegments(l_map, t, l_ASN.StartNodes, "L_HL|L_N1", ref l_HLIndex, l_HL_PIndex);


                        if (l_ASN.Orders != null && l_ASN.Orders.Count > 0)
                        {
                            l_HLIndex += 1;
                            foreach (LoopOrder l_Orders in l_ASN.Orders)
                            {
                                GenerateEDI856ForRepaintRoute.AddASNSegments(l_map, t, l_Orders.Data, "L_HL|L_N1", ref l_HLIndex, l_HL_PIndex);
                                l_HL_PIndex = l_HLIndex;

                                foreach (LoopPackage l_Package in l_Orders.Packs)
                                {
                                    GenerateEDI856ForRepaintRoute.AddASNSegments(l_map, t, l_Package.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);
                                    l_HL_PIndex = l_HLIndex;

                                    foreach (LoopItem l_Item in l_Package.Items)
                                    {
                                        GenerateEDI856ForRepaintRoute.AddASNSegments(l_map, t, l_Item.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);
                                    }
                                }
                            }
                        }
                        else
                        {
                            l_HLIndex += 2;
                            foreach (LoopPackage l_Package in l_ASN.Packs)
                            {
                                l_HL_PIndex = l_HLIndex;
                                GenerateEDI856ForRepaintRoute.AddASNSegments(l_map, t, l_Package.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);

                                foreach (LoopItem l_Item in l_Package.Items)
                                {
                                    GenerateEDI856ForRepaintRoute.AddASNSegments(l_map, t, l_Item.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);
                                }
                            }
                        }

                        GenerateEDI856ForRepaintRoute.AddASNSegments(l_map, t, l_ASN.EndNodes, string.Empty, ref l_HLIndex, l_HL_PIndex);

                        var g = new EdiGroup("SH");
                        g.Transactions.Add(t);

                        var i = new EdiInterchange();
                        i.Groups.Add(g);

                        EdiBatch b = new EdiBatch();
                        b.Interchanges.Add(i);


                        OutboundEDI l_Outbound = new OutboundEDI();

                        l_Outbound.UseConnection(l_SourceConnector.ConnectionString);

                        l_Outbound.Status = "NEW";
                        l_Outbound.Data = string.Empty;
                        l_Outbound.CreatedBy = 1;
                        l_Outbound.CreatedDate = DateTime.Now;
                        l_Outbound.OrderId = l_Order.Order.Id;

                        if (l_Outbound.SaveNew().IsSuccess)
                        {
                            EdiDataWriterSettings settings = new EdiDataWriterSettings(
                            new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                            new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                            new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                            l_EDIInfo.ISAReceiverQual, l_EDIInfo.ISAReceiverId, l_EDIInfo.ISASenderQual, l_EDIInfo.ISASenderId, l_EDIInfo.GSReceiverId, l_EDIInfo.GSSenderId,
                            l_EDIInfo.ISAEdiVersion, l_EDIInfo.GSEdiVersion, l_EDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, l_EDIInfo.SegmentSeparator, l_EDIInfo.ElementSeparator, "^", "");

                            EdiDataWriter w = new EdiDataWriter(settings);

                            OrderData l_OData = new OrderData();

                            l_OData.UseConnection(string.Empty, l_Order.Order.Connection);

                            l_OData.DeleteWithType(l_Order.Order.Id, "ASN-eSyncMate");

                            l_OData.Type = "ASN-eSyncMate";
                            l_OData.Data = w.WriteToString(b);
                            l_OData.CreatedBy = 1;
                            l_OData.CreatedDate = DateTime.Now;
                            l_OData.OrderId = l_Order.Order.Id;
                            l_OData.OrderNumber = l_Order.Order.OrderNumber;

                            if (l_OData.SaveNew().IsSuccess)
                            {
                                if (!string.IsNullOrEmpty(l_OData.Data))
                                {
                                    //l_DestinationConnector.BaseUrl = l_DestinationConnector.BaseUrl;
                                    //SftpConnector.Execute(l_DestinationConnector, false, $"{l_OData.OrderNumber}-856", l_OData.Data).GetAwaiter().GetResult();

                                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                                    string Command = string.Empty;


                                    Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + l_SourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.GenerateEDI856ForRepaintRoute + "', @p_OrderId = '" + Convert.ToInt32(item["Id"]) + "'";

                                    connection.Execute(Command);
                                }
                            }
                            
                        }

                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
                    }

                    route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_dataTable.Dispose();
            }
        }

        private static void AddASNSegments(Maps_5010.M_856 p_Map, EdiTrans p_Trans, List<SegmentNode> p_Nodes, string p_ParentNodeName, ref int p_HLIndex, int p_HL_PIndex)
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
                                MapLoop l_MapLoop = (MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0]);
                                while (l_index < l_ParentNodeNames.Length)
                                {
                                    try
                                    {
                                        l_SegmentDef = (MapSegment)(GenerateEDI856ForRepaintRoute.FindMapLoopDef(l_MapLoop, l_ParentNodeNames[l_index])).Content.First(s => s.Name == l_Node.Name);
                                        break;
                                    }
                                    catch (InvalidCastException)
                                    {
                                    }

                                    l_index++;
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
                        string l_Val = l_Value;

                        if (l_Val == "@HL_IDX@")
                        {
                            l_Val = p_HLIndex.ToString();
                            p_HLIndex++;
                        }
                        else if (l_Val == "@HL_P_IDX@")
                        {
                            l_Val = p_HL_PIndex.ToString();
                        }
                        else if (l_Value == "@HLCOUNT@")
                        {
                            l_Val = (p_HLIndex - 1).ToString();
                        }

                        l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Val));
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
