using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using MySqlConnector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace eSyncMate.DB.Entities
{
    public class CarrierLoadTender : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string AckStatus { get; set; }
        public string TrackStatus { get; set; }
        public DateTime TrackStatus_Updated { get; set; }
        public int CustomerId { get; set; }
        public int InboundEDIId { get; set; }
        public DateTime DocumentDate { get; set; }
        public string CarrierCode { get; set; }
        public string ShipmentId { get; set; }
        public string Purpose { get; set; }
        public string ReferenceNo { get; set; }
        public string BillToParty { get; set; }
        public string EquipmentNo { get; set; }
        public string ManualEquipmentNo { get; set; }
        public string Weight { get; set; }
        public string WeightUnitCode { get; set; }
        public string TotalUnits { get; set; }
        public string TotalUnitsCode { get; set; }
        public string ShipperNo { get; set; }
        public string PickupDate { get; set; }
        public string PickupTime { get; set; }
        public string DeliverDate { get; set; }
        public string DeliverTime { get; set; }
        public string ShipFromName { get; set; }
        public string ShipFromCode { get; set; }
        public string ShipFromAddress { get; set; }
        public string ShipFromCity { get; set; }
        public string ShipFromState { get; set; }
        public string ShipFromZip { get; set; }
        public string ShipFromCountry { get; set; }
        public string ConsigneeName { get; set; }
        public string ConsigneeCode { get; set; }
        public string ConsigneeAddress { get; set; }
        public string ConsigneeAddressMutiple { get; set; }
        public string ConsigneeCity { get; set; }
        public string ConsigneeState { get; set; }
        public string ConsigneeZip { get; set; }
        public string ConsigneeCountry { get; set; }
        public DateTime CreatedDate { get; set; }
        public string LastTrackStatus { get; set; }
        public string AFEvent { get; set; }
        public string X3Event { get; set; }
        public string D1Event { get; set; }
        public string X1Event { get; set; }
        public int CreatedBy { get; set; }
        public string CustomerName { get; set; }
        public List<CarrierLoadTenderData> Files { get; set; }
        public InboundEDI Inbound { get; set; }
        public List<OutboundEDI> Outbound { get; set; }
        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CarrierLoadTender() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CarrierLoadTender(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CarrierLoadTender(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(CarrierLoadTender.TableName))
            {
                CarrierLoadTender.TableName = "CarrierLoadTender";
            }

            if (string.IsNullOrEmpty(CarrierLoadTender.ViewName))
            {
                CarrierLoadTender.ViewName = "VW_CarrierLoadTender";
            }

            if (string.IsNullOrEmpty(CarrierLoadTender.PrimaryKeyName))
            {
                CarrierLoadTender.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(CarrierLoadTender.EndingPropertyName))
            {
                CarrierLoadTender.EndingPropertyName = "CreatedBy";
            }

            if (CarrierLoadTender.DBProperties == null)
            {
                CarrierLoadTender.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(CarrierLoadTender.InsertQueryStart))
            {
                CarrierLoadTender.InsertQueryStart = PrepareQueries(this, CarrierLoadTender.TableName, CarrierLoadTender.EndingPropertyName, ref l_Query, CarrierLoadTender.DBProperties);
            }
        }

        public void UseConnection(string p_ConnectionString, DBConnector p_Connection = null)
        {
            if (string.IsNullOrEmpty(p_ConnectionString))
            {
                Connection = p_Connection;
            }
            else
            {
                Connection = new DBConnector(p_ConnectionString);
            }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool GetAckData(ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = "SELECT * FROM CARRIERTENDERLOAD_ACK";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetFileCounts(ref DataTable p_Data, string p_Criteria)
        {
            string l_Query = string.Empty;

            l_Query = "SELECT * FROM VW_GetEDIFilesCount WHERE " + p_Criteria;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetStatesData(ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = "SELECT * FROM SCS_States WITH (NOLOCK)";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetCustomerWiseShipmentIdCLTData(string p_Criteria, ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = $"SELECT DISTINCT ShipmentId,CustomerID FROM {CarrierLoadTender.TableName} WITH (NOLOCK) WHERE " + p_Criteria;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetCustomerWiseShipperNoCLTData(string p_Criteria, ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = $"SELECT DISTINCT ShipperNo,CustomerID FROM {CarrierLoadTender.TableName} WITH (NOLOCK) WHERE " + p_Criteria;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetViewList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + CarrierLoadTender.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + CarrierLoadTender.ViewName + "]";
            }

            if (!string.IsNullOrEmpty(p_Criteria))
            {
                l_Query += " WHERE " + p_Criteria;
            }

            if (!string.IsNullOrEmpty(p_OrderBy))
            {
                l_Query += " ORDER BY " + p_OrderBy;
            }

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + CarrierLoadTender.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + CarrierLoadTender.TableName + "]";
            }

            if (!string.IsNullOrEmpty(p_Criteria))
            {
                l_Query += " WHERE " + p_Criteria;
            }

            if (!string.IsNullOrEmpty(p_OrderBy))
            {
                l_Query += " ORDER BY " + p_OrderBy;
            }

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(CarrierLoadTender.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CarrierLoadTender.ViewName, CarrierLoadTender.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + CarrierLoadTender.PrimaryKeyName + ", '0'))) FROM " + CarrierLoadTender.TableName, ref l_Data))
            {
                return l_MaxNo;
            }

            l_MaxNo = PublicFunctions.ConvertNullAsInteger(l_Data.Rows[0][0], 0) + 1;

            l_Data.Dispose();

            return l_MaxNo;
        }

        public Result GetObjectFromQuery(string p_Query, bool isOnlyObject = false)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            string l_Criteria = string.Empty;
            DataTable l_Data = new DataTable();
            CarrierLoadTenderData l_Files = new CarrierLoadTenderData();
            OutboundEDI l_Outbound = new OutboundEDI();

            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, string.Empty);

            l_Data.Dispose();
            if (isOnlyObject)
            {
                return Result.GetSuccessResult();
            }

            l_Data = new DataTable();

            this.Files = new List<CarrierLoadTenderData>();

            l_Files.UseConnection(string.Empty, this.Connection);
            l_Files.GetViewList("CarrierLoadTenderId = " + PublicFunctions.FieldToParam(this.Id, Declarations.FieldTypes.Number), "*", ref l_Data);

            foreach (DataRow l_Row in l_Data.Rows)
            {
                CarrierLoadTenderData l_File = new CarrierLoadTenderData();

                l_File.PopulateObjectFromRow(l_File, l_Data, CarrierLoadTenderData.DBProperties, string.Empty, l_Row);

                this.Files.Add(l_File);
            }

            l_Data.Dispose();

            this.Inbound = new InboundEDI();
            this.Inbound.UseConnection(string.Empty, this.Connection);
            this.Inbound.GetObject(this.InboundEDIId);

            l_Data = new DataTable();

            this.Outbound = new List<OutboundEDI>();

            l_Outbound.UseConnection(string.Empty, this.Connection);
            l_Outbound.GetViewList("OrderId = " + PublicFunctions.FieldToParam(this.Id, Declarations.FieldTypes.Number), "*", ref l_Data);

            foreach (DataRow l_Row in l_Data.Rows)
            {
                OutboundEDI l_OutboundEDI = new OutboundEDI();

                l_OutboundEDI.PopulateObjectFromRow(l_OutboundEDI, l_Data, OutboundEDI.DBProperties, string.Empty, l_Row);

                this.Outbound.Add(l_OutboundEDI);
            }

            l_Data.Dispose();

            return Result.GetSuccessResult();
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CarrierLoadTender.ViewName, CarrierLoadTender.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CarrierLoadTender.ViewName, CarrierLoadTender.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            foreach (PropertyInfo l_Prop in l_Property)
            {
                l_Prop.SetValue(this, propertyValue);
            }

            return GetObjectFromQuery(PrepareGetObjectQuery(this, CarrierLoadTender.ViewName, propertyName));
        }

        public Result SaveNew()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                this.Id = this.GetMax();

                l_Query = this.PrepareInsertQuery(this, CarrierLoadTender.InsertQueryStart, CarrierLoadTender.EndingPropertyName, CarrierLoadTender.DBProperties);

                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }

        public Result Modify()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, CarrierLoadTender.TableName, CarrierLoadTender.PrimaryKeyName, CarrierLoadTender.EndingPropertyName, CarrierLoadTender.DBProperties);

                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }

        public Result Delete()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareDeleteQuery(this, CarrierLoadTender.TableName, CarrierLoadTender.PrimaryKeyName);

                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }

        public List<CarrierLoadTender> GetUnacknowledgedCLTs(int customerId)
        {
            List<CarrierLoadTender> l_CLTs = new List<CarrierLoadTender>();
            DataTable l_Data = new DataTable();
            string l_Criteria = $"CustomerId = {customerId} AND Status = 'NEW' ";

            if (!this.GetViewList(l_Criteria, "*", ref l_Data, "CreatedDate"))
                return l_CLTs;

            foreach (DataRow l_Row in l_Data.Rows)
            {
                CarrierLoadTender l_CLT = new CarrierLoadTender();

                PopulateObjectFromRow(l_CLT, l_Data, l_Row);

                l_CLT.Files = new List<CarrierLoadTenderData>();

                DataTable l_FilesData = new DataTable();
                CarrierLoadTenderData l_Files = new CarrierLoadTenderData();

                l_Files.UseConnection(string.Empty, this.Connection);
                l_Files.GetViewList("CarrierLoadTenderId = " + PublicFunctions.FieldToParam(l_CLT.Id, Declarations.FieldTypes.Number), "*", ref l_FilesData);

                foreach (DataRow l_FRow in l_FilesData.Rows)
                {
                    CarrierLoadTenderData l_File = new CarrierLoadTenderData();

                    l_File.PopulateObjectFromRow(l_File, l_FilesData, CarrierLoadTenderData.DBProperties, string.Empty, l_FRow);

                    l_CLT.Files.Add(l_File);
                }

                l_FilesData.Dispose();

                l_CLT.Inbound = new InboundEDI();
                l_CLT.Inbound.UseConnection(string.Empty, this.Connection);
                l_CLT.Inbound.GetObject(l_CLT.InboundEDIId);

                l_CLTs.Add(l_CLT);
            }

            l_Data.Dispose();

            return l_CLTs;
        }

        public List<CarrierLoadTender> GetAcknowledgedCLTs(int customerId)
        {
            List<CarrierLoadTender> l_CLTs = new List<CarrierLoadTender>();
            DataTable l_Data = new DataTable();

            string l_Criteria = $@"
                                WITH CTE AS (
                                    SELECT *, 
                                           ROW_NUMBER() OVER (PARTITION BY ShipmentId ORDER BY ShipperNo ASC) AS rn
                                    FROM VW_CarrierLoadTender CLT
                                    WHERE CustomerId = {customerId} 
                                      AND (
                                          (Status = 'ACK' OR Status = 'ReadyToComplete') 
                                          AND ISNULL(AckStatus, '') = 'ACCEPT' 
                                          AND ISNULL(TrackStatus, '') <> '' 
                                          AND ISNULL(TrackStatus, '') <> 'X6'
                                          AND ISNULL(TrackStatus, '') <> 'X1'
										  AND ISNULL(TrackStatus, '') <> 'D1'
                                          AND ISNULL(LastTrackStatus, '') <> ISNULL(TrackStatus, '') 
                                          AND NOT (
                                              (ISNULL(AFEvent, '') = 'AF' AND TrackStatus = 'AF') OR 
                                              (ISNULL(X3Event, '') = 'X3' AND TrackStatus = 'X3') 
                                          )
                                      ) 
                                )
                                SELECT * FROM CTE WHERE rn = 1 
                                UNION ALL
                                SELECT *, NULL AS rn FROM VW_CarrierLoadTender CLT
                                WHERE CustomerId = {customerId}
                                  AND (
                                      (ISNULL(TrackStatus, '') = 'D1' OR ISNULL(TrackStatus, '') = 'X1')
                                      AND ISNULL(LastConsigneeName, '') <> ISNULL(ConsigneeName, '')
                                      AND EXISTS (
                                          SELECT 1 FROM VW_CarrierLoadTender CLT2
                                          WHERE CLT2.ShipmentId = CLT.ShipmentId  
                                            AND CLT2.ShipperNo = CLT.ShipperNo    
                                            AND ISNULL(CLT2.ConsigneeAddressMutiple, '') = ISNULL(CLT2.ConsigneeName, '') 
                                      )
                                      AND Status = 'ACK'
                                      AND ISNULL(AckStatus, '') = 'ACCEPT'
                                      AND NOT (
                                          (ISNULL(X1Event, '') = 'X1' AND TrackStatus = 'X1') OR 
                                          (ISNULL(D1Event, '') = 'D1' AND TrackStatus = 'D1')
                                      )
                                  ) 
                                ORDER BY CreatedDate;";

            if (!this.Connection.GetData(l_Criteria, ref l_Data))
                return l_CLTs;

            foreach (DataRow l_Row in l_Data.Rows)
            {
                CarrierLoadTender l_CLT = new CarrierLoadTender();
                PopulateObjectFromRow(l_CLT, l_Data, l_Row);

                // Load Files
                l_CLT.Files = new List<CarrierLoadTenderData>();
                DataTable l_FilesData = new DataTable();
                CarrierLoadTenderData l_Files = new CarrierLoadTenderData();

                l_Files.UseConnection(string.Empty, this.Connection);
                l_Files.GetViewList("CarrierLoadTenderId = " + PublicFunctions.FieldToParam(l_CLT.Id, Declarations.FieldTypes.Number), "*", ref l_FilesData);

                foreach (DataRow l_FRow in l_FilesData.Rows)
                {
                    CarrierLoadTenderData l_File = new CarrierLoadTenderData();
                    l_File.PopulateObjectFromRow(l_File, l_FilesData, CarrierLoadTenderData.DBProperties, string.Empty, l_FRow);
                    l_CLT.Files.Add(l_File);
                }
                l_FilesData.Dispose();

                // Load Inbound EDI Data
                l_CLT.Inbound = new InboundEDI();
                l_CLT.Inbound.UseConnection(string.Empty, this.Connection);
                l_CLT.Inbound.GetObject(l_CLT.InboundEDIId);

                l_CLTs.Add(l_CLT);
            }

            l_Data.Dispose();
            return l_CLTs;
        }

        public List<CarrierLoadTender> GetX6AcknowledgedCLTs(int customerId)
        {
            List<CarrierLoadTender> l_CLTs = new List<CarrierLoadTender>();
            DataTable l_Data = new DataTable();

            string l_Query = $@"
                        WITH CTE AS (
                            SELECT *, 
                                   ROW_NUMBER() OVER (PARTITION BY ShipmentId ORDER BY ShipperNo ASC) AS rn
                            FROM [{CarrierLoadTender.ViewName}]
                            WHERE CustomerId = {customerId}
                              AND (Status = 'ACK' OR Status = 'ReadyToComplete')
                              AND ISNULL(AckStatus, '') = 'ACCEPT'
                              AND ISNULL(TrackStatus, '') <> ''
                        )
                        SELECT * FROM CTE WHERE rn = 1
                        ORDER BY CreatedDate";

            if (!this.Connection.GetData(l_Query, ref l_Data))
                return l_CLTs;

            foreach (DataRow l_Row in l_Data.Rows)
            {
                CarrierLoadTender l_CLT = new CarrierLoadTender();
                PopulateObjectFromRow(l_CLT, l_Data, l_Row);

                // Load Files
                l_CLT.Files = new List<CarrierLoadTenderData>();
                DataTable l_FilesData = new DataTable();
                CarrierLoadTenderData l_Files = new CarrierLoadTenderData();

                l_Files.UseConnection(string.Empty, this.Connection);
                l_Files.GetViewList("CarrierLoadTenderId = " + PublicFunctions.FieldToParam(l_CLT.Id, Declarations.FieldTypes.Number), "*", ref l_FilesData);

                foreach (DataRow l_FRow in l_FilesData.Rows)
                {
                    CarrierLoadTenderData l_File = new CarrierLoadTenderData();
                    l_File.PopulateObjectFromRow(l_File, l_FilesData, CarrierLoadTenderData.DBProperties, string.Empty, l_FRow);
                    l_CLT.Files.Add(l_File);
                }
                l_FilesData.Dispose();

                // Load Inbound EDI Data
                l_CLT.Inbound = new InboundEDI();
                l_CLT.Inbound.UseConnection(string.Empty, this.Connection);
                l_CLT.Inbound.GetObject(l_CLT.InboundEDIId);

                l_CLTs.Add(l_CLT);
            }

            l_Data.Dispose();
            return l_CLTs;
        }

        public bool UpdateTrackStatusForX1AndD1(int tenderID, string trackStatus, string shipmentId, string shipperNo, string consigneeName = "")
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection);

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = @"
                            UPDATE [CarrierLoadTender] 
                            SET 
                                [LastTrackStatus] = @TrackStatus, 
                                [TrackStatus_Updated] = @TrackDate,
                                [LastConsigneeName] = @ConsigneeName, 
                                [X1Event] = CASE WHEN @TrackStatus = 'X1' THEN @TrackStatus ELSE [X1Event] END,
                                [D1Event] = CASE WHEN @TrackStatus = 'D1' THEN @TrackStatus ELSE [D1Event] END
                            WHERE [ShipmentId] = @ShipmentId AND shipperNo = @ShipperNo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TrackStatus", trackStatus);
                        command.Parameters.AddWithValue("@TenderID", tenderID);
                        command.Parameters.AddWithValue("@TrackDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ConsigneeName", consigneeName);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);
                        command.Parameters.AddWithValue("@ShipperNo", shipperNo);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }

                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateTrackStatus(int tenderID, string trackStatus, string shipmentId = "", string consigneeName = "")
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection);

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = @"
                            UPDATE [CarrierLoadTender] 
                            SET 
                                [LastTrackStatus] = @TrackStatus, 
                                [TrackStatus_Updated] = @TrackDate,
                                [LastConsigneeName] = @ConsigneeName, 
                                [AFEvent] = CASE WHEN @TrackStatus = 'AF' THEN @TrackStatus ELSE [AFEvent] END,
                                [X3Event] = CASE WHEN @TrackStatus = 'X3' THEN @TrackStatus ELSE [X3Event] END
                            WHERE [ShipmentId] = @ShipmentId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TrackStatus", trackStatus);
                        command.Parameters.AddWithValue("@TenderID", tenderID);
                        command.Parameters.AddWithValue("@TrackDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ConsigneeName", consigneeName);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }

                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateAddresses(int tenderID, string consigneeAddress, string consigneeCity, string consigneeState, string consigneeZip, string consigneeCountry, string equipmentNo, string manualequipmentNo)
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection); // Assuming this.Connection is your existing connection

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [CarrierLoadTender] " +
                                   "SET [ConsigneeAddress] = @consigneeAddress, [ConsigneeCity] = @consigneeCity , [ConsigneeState] = @consigneeState  " +
                                   ",[ConsigneeZip] = @consigneeZip ,[ConsigneeCountry] = @consigneeCountry,[EquipmentNo] = @equipmentNo,[ManualEquipmentNo] = @manualequipmentNo" +
                                   " WHERE [Id] = @TenderID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@consigneeAddress", consigneeAddress);
                        command.Parameters.AddWithValue("@consigneeCity", consigneeCity);
                        command.Parameters.AddWithValue("@consigneeState", consigneeState);
                        command.Parameters.AddWithValue("@consigneeZip", consigneeZip);
                        command.Parameters.AddWithValue("@consigneeCountry", consigneeCountry);
                        command.Parameters.AddWithValue("@equipmentNo", equipmentNo);
                        command.Parameters.AddWithValue("@manualequipmentNo", @manualequipmentNo);
                        command.Parameters.AddWithValue("@TenderID", tenderID);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateAckStatus(string shipmentId, string ackStatus)
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection); // Assuming this.Connection is your existing connection

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [CarrierLoadTender] " +
                                   "SET [AckStatus] = @AckStatus " + (ackStatus == "ACCEPT" ? ", TrackStatus = 'X6', TrackStatus_Updated=GETDATE()" : "") +
                                   "WHERE [ShipmentID] = @ShipmentId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AckStatus", ackStatus);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateStatus(string shipmentId, string status)
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection); // Assuming this.Connection is your existing connection

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [CarrierLoadTender] " +
                                   "SET [Status] = @Status " +
                                   "WHERE [ShipmentID] = @ShipmentId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool CLTUpdateAddresses(int p_UserNo)
        {
            bool l_Result = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            try
            {
                l_Query = "EXEC SP_CLTUpdateAddress ";
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;

                l_Result = Connection.Execute(l_Query);
            }
            catch (Exception ex)
            {
                throw;
            }

            return l_Result;
        }

        public bool UpdateAckStatus(string shipmentId, string ShipperNo, string status)
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection); // Assuming this.Connection is your existing connection

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [CarrierLoadTender] " +
                                   "SET [Status] = @Status,[AckStatus] = 'ACCEPT',[TrackStatus] = 'X6',[TrackStatus_Updated] = GETDATE() " +
                                   "WHERE [ShipmentID] = @ShipmentId AND [ShipperNo] = @ShipperNo AND Status <> 'COMPLETE'";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);
                        command.Parameters.AddWithValue("@ShipperNo", ShipperNo);


                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }
        
        public bool UpdateAckStatusForAllShipmentId(string shipmentId, string status)
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection);

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [CarrierLoadTender] " +
                                   "SET [Status] = @Status,[AckStatus] = 'ACCEPT',[TrackStatus] = 'X6',[TrackStatus_Updated] = GETDATE() " +
                                   "WHERE [ShipmentID] = @ShipmentId AND Status <> 'COMPLETE'";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateAcknowledgeStatus(string shipmentId, string status)
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection); // Assuming this.Connection is your existing connection

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [CarrierLoadTender] " +
                                   "SET [Status] = @Status " +
                                   "WHERE [ShipmentID] = @ShipmentId AND Status NOT IN ('ACK','COMPLETE')";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateConsigneeAddressMutipleStatus(string shipmentId, string allAddresses)
        {
            bool isUpdated = false;
            CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();
            l_CarrierLoadTenderData.UseConnection(string.Empty, this.Connection);

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CarrierLoadTenderData.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [CarrierLoadTender] " +
                                   "SET [ConsigneeAddressMutiple] = @allAddresses " +
                                   "WHERE [ShipmentID] = @ShipmentId AND Status IN ('NEW')";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@allAddresses", allAddresses);
                        command.Parameters.AddWithValue("@ShipmentId", shipmentId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public bool CLTUpdateStatus()
        {
            string UpdateQuery = $"UPDATE CarrierLoadTender SET Status = 'COMPLETE' ";

            UpdateQuery += $"WHERE Status = 'ReadyToComplete'";

            return this.Connection.Execute(UpdateQuery);
        }

        public Result SetCLTStatus(int cltId, string status, string CompletionDate = "")
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                if (!string.IsNullOrEmpty(CompletionDate) && CompletionDate != "1900-01-01")
                {
                    l_Query = $"UPDATE CarrierLoadTender SET Status = '{status}' , CompletionDate = '{CompletionDate}' WHERE Id = {cltId}";

                }
                else
                {
                    l_Query = $"UPDATE CarrierLoadTender SET Status = '{status}' WHERE Id = {cltId}";
                }


                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }



        public static void InsertMySqlData(string CustomerName, string ShipperNo, string ShipmentID, string EquipmentNo, string ConsigneeName,string LastConsigneeName, string ConnectionString)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    try
                    {
                        conn.Open();
                        string query = "INSERT INTO transactions_edi (cutomer_name, shipment_shipper_no,shipment_id,plates, lastlocation, first_stop) VALUES (@CustomerName, @ShipperNo, @ShipmentID, @EquipmentNo, @ConsigneeName , @LastConsigneeName)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@CustomerName", CustomerName);
                        cmd.Parameters.AddWithValue("@ShipperNo", ShipperNo);
                        cmd.Parameters.AddWithValue("@ShipmentID", ShipmentID);
                        cmd.Parameters.AddWithValue("@EquipmentNo", EquipmentNo);
                        cmd.Parameters.AddWithValue("@ConsigneeName", ConsigneeName);
                        cmd.Parameters.AddWithValue("@LastConsigneeName", LastConsigneeName);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        Console.WriteLine($"{rowsAffected} row(s) inserted.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                // TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                // TODO: set large fields to null.
            }

            disposedValue = true;
        }

        // TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
        // Protected Overrides Sub Finalize()
        // ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        // Dispose(False)
        // MyBase.Finalize()
        // End Sub

        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region IEqualityComparer Support
        public new bool Equals(object x, object y)
        {
            return ((CarrierLoadTender)x).Id == ((CarrierLoadTender)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion
    }
}
