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
using static eSyncMate.Processor.Models._846ResponseModel;

namespace eSyncMate.Processor.Managers
{
    public static class Download846FromFTPRoute
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

                if (l_SourceConnector.AuthType == ConnectorTypesEnum.FTP.ToString())
                {
                    Dictionary<string, string> files = FtpConnector.Execute(l_SourceConnector.Host, l_SourceConnector.ConsumerKey, l_SourceConnector.ConsumerSecret, l_SourceConnector.Method, true, "", "").GetAwaiter().GetResult();



                    foreach (var file in files)
                    {
                        EdiDataReader r = new EdiDataReader();
                        InboundEDI l_EDI = new InboundEDI();
                        Customers l_Customer = new Customers();
                        try
                        {
                            route.SaveLog(LogTypeEnum.Info, $"Processing file {file.Key}", string.Empty, userNo);
                            route.SaveData("SRC", 0, file.Value, userNo);

                            l_EDI.UseConnection(l_DestinationConnector.ConnectionString);
                            l_Customer.UseConnection(l_DestinationConnector.ConnectionString);

                            l_EDI.Type = "846";
                            l_EDI.Status = "NEW";
                            l_EDI.Data = file.Value;
                            l_EDI.CreatedBy = userNo;
                            l_EDI.CreatedDate = DateTime.Now;

                            l_EDI.SaveNew();

                            EdiBatch b = r.FromString(file.Value);

                            foreach (EdiInterchange i in b.Interchanges)
                            {
                                //if (!l_Customer.GetObject("ISACustomerID", i.ISA.Content[5].Val.ToString().Trim() + i.ISA.Content[7].Val.ToString().Trim()).IsSuccess)
                                //{
                                //    route.SaveLog(LogTypeEnum.Exception, $"The party [{i.ISA.Content[5].Val.ToString().Trim() + i.ISA.Content[7].Val.ToString().Trim()}] is not registered.", string.Empty, userNo);

                                //    l_EDI.Status = "ERROR";
                                //    l_EDI.Modify();

                                //    continue;
                                //}

                                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                                string l_TransformationMap = string.Empty;

                                map.UseConnection(l_DestinationConnector.ConnectionString);
                                map.GetObject(route.MapId);

                                l_TransformationMap = map.Map;

                                if (string.IsNullOrEmpty(l_TransformationMap))
                                {
                                    route.SaveLog(LogTypeEnum.Exception, $"Required maps for 855 processing is missing.", string.Empty, userNo);

                                    l_EDI.Status = "ERROR";
                                    l_EDI.Modify();

                                    continue;
                                }

                                InboundEDIInfo l_EDIInfo = new InboundEDIInfo();

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
                                l_EDIInfo.CreatedBy = userNo;
                                l_EDIInfo.CreatedDate = DateTime.Now;

                                l_EDIInfo.UseConnection(l_DestinationConnector.ConnectionString);
                                foreach (EdiGroup g in i.Groups)
                                {
                                    l_EDIInfo.GSSenderId = g.GS.Content[1].ToString().Trim();
                                    l_EDIInfo.GSReceiverId = string.IsNullOrEmpty(g.GS.Content[2]?.ToString().Trim()) ? "6464214136" : g.GS.Content[2].ToString().Trim();
                                    l_EDIInfo.GSControlNumber = g.GS.Content[5].ToString().Trim();
                                    l_EDIInfo.GSEdiVersion = g.GS.Content[7].ToString().Trim();

                                    if (l_EDIInfo.SaveNew().IsSuccess)
                                        FtpConnector.DeleteFileFromFTP(l_SourceConnector.Host, l_SourceConnector.ConsumerKey, l_SourceConnector.ConsumerSecret, l_SourceConnector.Method, file.Key);

                                    foreach (EdiTrans t in g.Transactions)
                                    {
                                        InvFeedFromNDC l_InvFeedFromNDC = new InvFeedFromNDC();
                                        l_InvFeedFromNDC.UseConnection(l_DestinationConnector.ConnectionString);

                                        try
                                        {
                                            string jsonData = JsonConvert.SerializeObject(t);
                                            string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, jsonData);

                                            route.SaveData("JSON", 0, jsonTransformation, userNo);

                                            _846ResponseModel received = JsonConvert.DeserializeObject<_846ResponseModel>(jsonTransformation);


                                            foreach (Item846 item in received.Items)
                                            {
                                                l_InvFeedFromNDC.InvFeedFromNDC846(item.SKU, item.ItemID, string.IsNullOrEmpty(item.Description) ? item.ItemID : item.Description, item.Qty, 0, "");
                                            }

                                            //l_PurchaseOrders.UseConnection(l_DestinationConnector.ConnectionString);

                                            //l_PurchaseOrders.GetObject("PONumber", received.PONumber);

                                            //if (l_PurchaseOrders != null)
                                            //{
                                            //    l_PurchaseOrders.UpdatePoStatus(received.PONumber,"ACKNOWLEDGED", l_DestinationConnector.ConnectionString);
                                            //}
                                        }
                                        catch (Exception)
                                        {
                                            throw;
                                        }
                                        finally
                                        {
                                            if (l_InvFeedFromNDC != null)
                                            {
                                                string edi_997 = CarrierLoadTenderManager.Surgimac846Generate997(l_EDIInfo, t, "846");

                                                if (!string.IsNullOrEmpty(edi_997))
                                                {
                                                    route.SaveData("ACK", 0, edi_997, userNo);
                                                    l_SourceConnector.Method = "/outbound/997";
                                                    FtpConnector.Execute(l_SourceConnector.Host, l_SourceConnector.ConsumerKey, l_SourceConnector.ConsumerSecret, l_SourceConnector.Method, false, $"{Path.GetFileNameWithoutExtension(file.Key)}-997", edi_997).GetAwaiter().GetResult();
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            route.SaveLog(LogTypeEnum.Info, $"Completed processing file {file.Key}", string.Empty, userNo);
                        }
                        catch (Exception ex)
                        {
                            route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}] and file [{file.Key}]", ex.ToString(), userNo);
                        }
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
            }
        }
    }
}
