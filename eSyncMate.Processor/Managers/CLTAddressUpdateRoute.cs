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
using static eSyncMate.Processor.Models.SCSCancelOrderResponse;
using static eSyncMate.Processor.Models.InputCancellationLinesModel;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using MySqlX.XDevAPI.Relational;
using System.Diagnostics;

namespace eSyncMate.Processor.Managers
{
    public class CLTAddressUpdateRoute
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
            CarrierLoadTender l_CarrierLoadTender = new CarrierLoadTender();
            DataTable l_Data = new DataTable();
            DataTable l_PrepareTable   = new DataTable();
            DataTable dataTable = new DataTable();
            bool l_Process = false;
            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
                
                CLTAddressUpdateRoute.PrepareDataTableColumn(ref l_PrepareTable);
                l_CarrierLoadTender.UseConnection(l_SourceConnector.ConnectionString);

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


                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    if (l_SourceConnector.CommandType == "QUERY")
                    {
                        dataTable = GetDataTable(l_SourceConnector.ConnectionString);

                        if (dataTable.Rows.Count > 0)
                        {
                            foreach (DataRow row in dataTable.Rows)
                            {
                                DataRow l_row = l_PrepareTable.NewRow();

                                l_row["ShipmentId"] = row["shipment_id"];
                                l_row["ShipperNo"] = row["shipment_shipper_no"];
                                l_row["TrackStatus"] = row["ack_type"];
                                l_row["ShipFromAddress"] = row["address"];
                                l_row["ShipFromCity"] = row["city"];
                                l_row["ShipFromState"] = row["state"];
                                l_row["ShipFromZip"] = row["zip"];
                                l_row["ShipFromCountry"] = row["country"];
                                l_row["VehiclePlateNo"] = row["plates"];
                                l_row["geofence"] = row["geofence"];

                                l_PrepareTable.Rows.Add(l_row);
                            }

                            PublicFunctions.BulkInsert(l_DestinationConnector.ConnectionString, "Temp_CLTUpdateAddress", l_PrepareTable);
                        }

                        //l_CarrierLoadTender.GetViewList($"Status = 'ACK' ", string.Empty, ref l_Data, "Id DESC");
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_DestinationConnector.ConnectionString);
                    DataTable l_desData = new DataTable();

                    l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@USERNO@", userNo.ToString());

                    if (l_DestinationConnector.CommandType == "SP")
                    {
                        l_Process = connection.Execute(l_DestinationConnector.Command);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }



                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally 
            {
                l_dataTable.Dispose();
                l_PrepareTable.Dispose();
                dataTable.Dispose();
            }
        }


        static DataTable GetDataTable(string connectionString)
        {
            DataTable dataTable = new DataTable();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM transactions_edi WHERE IFNULL(completed,0) = 0";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return dataTable;
        }

        private static DataTable PrepareDataTableColumn(ref DataTable p_DataTable)
        {
            p_DataTable.Columns.Add("ShipmentId", typeof(string));
            p_DataTable.Columns.Add("ShipperNo", typeof(string));
            p_DataTable.Columns.Add("TrackStatus", typeof(string));
            p_DataTable.Columns.Add("ShipFromAddress", typeof(string));
            p_DataTable.Columns.Add("ShipFromCity", typeof(string));
            p_DataTable.Columns.Add("ShipFromState", typeof(string));
            p_DataTable.Columns.Add("ShipFromZip", typeof(string));
            p_DataTable.Columns.Add("ShipFromCountry", typeof(string));
            p_DataTable.Columns.Add("VehiclePlateNo", typeof(string));
            p_DataTable.Columns.Add("geofence", typeof(string));

            return p_DataTable;
        }
    }
}
