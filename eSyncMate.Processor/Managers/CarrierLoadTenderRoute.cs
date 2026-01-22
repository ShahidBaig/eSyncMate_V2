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

namespace eSyncMate.Processor.Managers
{
    public static class CarrierLoadTenderRoute
    {
        public static void Execute(IConfiguration config, Routes route)
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
                    
                    route.SaveLog(LogTypeEnum.Exception, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    
                    route.SaveLog(LogTypeEnum.Exception, "Destination Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.AuthType == ConnectorTypesEnum.SFTP.ToString())
                {
                    Dictionary<string, string> files = SftpConnector.Execute(l_SourceConnector).GetAwaiter().GetResult();

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

                            l_EDI.Type = "204";
                            l_EDI.Status = "NEW";
                            l_EDI.Data = file.Value;
                            l_EDI.CreatedBy = userNo;
                            l_EDI.CreatedDate = DateTime.Now;

                            l_EDI.SaveNew();

                            EdiBatch b = r.FromString(file.Value);

                            foreach (EdiInterchange i in b.Interchanges)
                            {
                                if (!l_Customer.GetObject("ISACustomerID", i.ISA.Content[5].Val.ToString().Trim() + i.ISA.Content[7].Val.ToString().Trim()).IsSuccess)
                                {
                                    route.SaveLog(LogTypeEnum.Exception, $"The party [{i.ISA.Content[5].Val.ToString().Trim() + i.ISA.Content[7].Val.ToString().Trim()}] is not registered.", string.Empty, userNo);

                                    l_EDI.Status = "ERROR";
                                    l_EDI.Modify();

                                    continue;
                                }

                                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                                string l_TransformationMap = string.Empty;

                                map.UseConnection(l_DestinationConnector.ConnectionString);
                                map.GetObject(route.MapId);

                                l_TransformationMap = map.Map;

                                if (string.IsNullOrEmpty(l_TransformationMap))
                                {
                                    route.SaveLog(LogTypeEnum.Exception, $"Required maps for 204 processing is missing.", string.Empty, userNo);

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
                                    l_EDIInfo.GSReceiverId = g.GS.Content[2].ToString().Trim();
                                    l_EDIInfo.GSControlNumber = g.GS.Content[5].ToString().Trim();
                                    l_EDIInfo.GSEdiVersion = g.GS.Content[7].ToString().Trim();

                                    if (l_EDIInfo.SaveNew().IsSuccess)
                                        SftpConnector.DeleteFileFromSFTP(file.Key, l_SourceConnector);

                                    foreach (EdiTrans t in g.Transactions)
                                    {
                                        CarrierLoadTender? l_Carrier = null;

                                        try
                                        {
                                            string jsonData = JsonConvert.SerializeObject(t);
                                            string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, jsonData);

                                            route.SaveData("JSON", 0, jsonTransformation, userNo);

                                            CarrierLoadTenderReceived? received = JsonConvert.DeserializeObject<CarrierLoadTenderReceived>(jsonTransformation);
                                            CarrierLoadTenderViewModel l_CarrierViewModel = null;
                                            string shipperNo = string.Empty;

                                            if (received?.StopOffs != null)
                                            {
                                                foreach (StopOff shipment in received.StopOffs.Where(s => !s.processed).OrderBy(s => Convert.ToInt32(s.LineNo)))
                                                {
                                                    if (shipment.processed)
                                                        continue;

                                                    shipment.processed = true;

                                                    l_Data = new DataTable();

                                                    l_Carrier = new CarrierLoadTender();
                                                    l_Carrier.UseConnection(l_DestinationConnector.ConnectionString);
                                                    l_Carrier.GetViewList($"ShipmentId = '{received.ShipmentId}' AND Status <> 'COMPLETE' ", string.Empty, ref l_Data, "Id DESC");

                                                    if (l_Data.Rows.Count > 0)
                                                    {
                                                        l_Carrier.UpdateStatus(received.ShipmentId, "COMPLETE");
                                                    }

                                                    List<string> consigneeAddresses = new List<string>();

                                                    foreach (string stopOffShipperNo in shipment.ShipperNo)
                                                    {
                                                        List<StopOff> relatedShipments = received.StopOffs.Where(s => !s.processed && s.ShipperNo.Where(sno => sno == stopOffShipperNo)?.ToList().Count > 0).ToList();

                                                        if (relatedShipments.Count == 0)
                                                            continue;

                                                        StopOff relatedShipment = relatedShipments[0];

                                                        l_CarrierViewModel = new CarrierLoadTenderViewModel();
                                                        l_Carrier = new CarrierLoadTender();
                                                        l_Carrier.UseConnection(l_DestinationConnector.ConnectionString);

                                                        l_Carrier.CarrierCode = received.CarrierCode;
                                                        l_Carrier.ShipmentId = received.ShipmentId;
                                                        l_Carrier.Purpose = received.Purpose;
                                                        l_Carrier.ReferenceNo = received.ReferenceNo;
                                                        l_Carrier.DocumentDate = DateTime.Parse(received.DocumentDate);
                                                        l_Carrier.BillToParty = received.BillToParty;
                                                        l_Carrier.EquipmentNo = received.EquipmentNo;
                                                        l_Carrier.Weight = shipment.Weight;
                                                        l_Carrier.WeightUnitCode = shipment.WeightUnitCode;
                                                        l_Carrier.TotalUnits = shipment.TotalUnits;
                                                        l_Carrier.TotalUnitsCode = shipment.TotalUnitsCode;
                                                        l_Carrier.ShipperNo = stopOffShipperNo;
                                                        l_Carrier.PickupDate = shipment.PickupDate;
                                                        l_Carrier.PickupTime = shipment.PickupTime;
                                                        l_Carrier.DeliverDate = shipment.DeliverDate;
                                                        l_Carrier.DeliverTime = shipment.DeliverTime;
                                                        l_Carrier.ShipFromName = shipment.ShipFromName;
                                                        l_Carrier.ShipFromCode = shipment.ShipFromCode;
                                                        l_Carrier.ShipFromAddress = shipment.ShipFromAddress;
                                                        l_Carrier.ShipFromCity = shipment.ShipFromCity;
                                                        l_Carrier.ShipFromState = shipment.ShipFromState;
                                                        l_Carrier.ShipFromZip = shipment.ShipFromZip;
                                                        l_Carrier.ShipFromCountry = shipment.ShipFromCountry;
                                                        l_Carrier.ConsigneeName = shipment.ConsigneeName;
                                                        l_Carrier.ConsigneeCode = shipment.ConsigneeCode;
                                                        l_Carrier.ConsigneeAddress = shipment.ConsigneeAddress;
                                                        l_Carrier.ConsigneeCity = shipment.ConsigneeCity;
                                                        l_Carrier.ConsigneeState = shipment.ConsigneeState;
                                                        l_Carrier.ConsigneeZip = shipment.ConsigneeZip;
                                                        l_Carrier.ConsigneeCountry = shipment.ConsigneeCountry;

                                                        l_Carrier.PickupDate = string.IsNullOrEmpty(relatedShipment.PickupDate) ? l_Carrier.PickupDate : relatedShipment.PickupDate;
                                                        l_Carrier.PickupTime = string.IsNullOrEmpty(relatedShipment.PickupTime) ? l_Carrier.PickupTime : relatedShipment.PickupTime;
                                                        l_Carrier.DeliverDate = string.IsNullOrEmpty(relatedShipment.DeliverDate) ? l_Carrier.DeliverDate : relatedShipment.DeliverDate;
                                                        l_Carrier.DeliverTime = string.IsNullOrEmpty(relatedShipment.DeliverTime) ? l_Carrier.DeliverTime : relatedShipment.DeliverTime;
                                                        l_Carrier.ShipFromName = string.IsNullOrEmpty(relatedShipment.ShipFromName) ? l_Carrier.ShipFromName : relatedShipment.ShipFromName;
                                                        l_Carrier.ShipFromCode = string.IsNullOrEmpty(relatedShipment.ShipFromCode) ? l_Carrier.ShipFromCode : relatedShipment.ShipFromCode;
                                                        l_Carrier.ShipFromAddress = string.IsNullOrEmpty(relatedShipment.ShipFromAddress) ? l_Carrier.ShipFromAddress : relatedShipment.ShipFromAddress;
                                                        l_Carrier.ShipFromCity = string.IsNullOrEmpty(relatedShipment.ShipFromCity) ? l_Carrier.ShipFromCity : relatedShipment.ShipFromCity;
                                                        l_Carrier.ShipFromState = string.IsNullOrEmpty(relatedShipment.ShipFromState) ? l_Carrier.ShipFromState : relatedShipment.ShipFromState;
                                                        l_Carrier.ShipFromZip = string.IsNullOrEmpty(relatedShipment.ShipFromZip) ? l_Carrier.ShipFromZip : relatedShipment.ShipFromZip;
                                                        l_Carrier.ShipFromCountry = string.IsNullOrEmpty(relatedShipment.ShipFromCountry) ? l_Carrier.ShipFromCountry : relatedShipment.ShipFromCountry;
                                                        l_Carrier.ConsigneeName = string.IsNullOrEmpty(relatedShipment.ConsigneeName) ? l_Carrier.ConsigneeName : relatedShipment.ConsigneeName;
                                                        l_Carrier.ConsigneeCode = string.IsNullOrEmpty(relatedShipment.ConsigneeCode) ? l_Carrier.ConsigneeCode : relatedShipment.ConsigneeCode;
                                                        l_Carrier.ConsigneeAddress = string.IsNullOrEmpty(relatedShipment.ConsigneeAddress) ? l_Carrier.ConsigneeAddress : relatedShipment.ConsigneeAddress;
                                                        l_Carrier.ConsigneeCity = string.IsNullOrEmpty(relatedShipment.ConsigneeCity) ? l_Carrier.ConsigneeCity : relatedShipment.ConsigneeCity;
                                                        l_Carrier.ConsigneeState = string.IsNullOrEmpty(relatedShipment.ConsigneeState) ? l_Carrier.ConsigneeState : relatedShipment.ConsigneeState;
                                                        l_Carrier.ConsigneeZip = string.IsNullOrEmpty(relatedShipment.ConsigneeZip) ? l_Carrier.ConsigneeZip : relatedShipment.ConsigneeZip;
                                                        l_Carrier.ConsigneeCountry = string.IsNullOrEmpty(relatedShipment.ConsigneeCountry) ? l_Carrier.ConsigneeCountry : relatedShipment.ConsigneeCountry;
                                                        l_Carrier.ConsigneeAddressMutiple = l_Carrier.ConsigneeName;
                                                       
                                                        foreach (CarrierLoadTenderPackage package in relatedShipment.Packages)
                                                        {
                                                            CarrierLoadTenderPackage pack = new CarrierLoadTenderPackage();

                                                            PublicFunctions.CopyTo(package, pack);
                                                            l_CarrierViewModel.Packages.Add(pack);
                                                        }

                                                        PublicFunctions.CopyTo(l_Carrier, l_CarrierViewModel);
                                                        jsonTransformation = JsonConvert.SerializeObject(l_CarrierViewModel);

                                                        CarrierLoadTenderManager.SaveCarrierLoadTender(l_Carrier, l_Customer, l_EDI, userNo, file.Value, jsonTransformation);

                                                        relatedShipment.processedCount++;

                                                        if (relatedShipment.ShipperNo.Count == relatedShipment.processedCount)
                                                            relatedShipment.processed = true;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //route.SaveLog(LogTypeEnum.Exception, "StopOffs collection is null for the current received object.", string.Empty, userNo);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            throw;
                                        }
                                        finally
                                        {
                                            if (l_Carrier != null)
                                            {
                                                string edi_997 = CarrierLoadTenderManager.Generate997(l_EDIInfo, l_Carrier, t);

                                                if (!string.IsNullOrEmpty(edi_997))
                                                {
                                                    route.SaveData("ACK", 0, edi_997, userNo);

                                                    l_SourceConnector.BaseUrl = l_SourceConnector.Url;
                                                        //SftpConnector.Execute(l_SourceConnector, false, $"{Path.GetFileNameWithoutExtension(file.Key)}-997", edi_997).GetAwaiter().GetResult();
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
                l_Data.Dispose();
            }
        }
    }
}
