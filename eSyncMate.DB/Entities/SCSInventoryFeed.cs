using Microsoft.VisualBasic;
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
using System.Threading.Tasks;
using System.Xml.Linq;

namespace eSyncMate.DB.Entities
{
    public class SCSInventoryFeed : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string ItemId { get; set; }
        public string ETA_Date { get; set; }
        public int ETA_Qty { get; set; }
        public int ATS_L10 { get; set; }
        public int ATS_L21 { get; set; }
        public int ATS_L28 { get; set; }
        public int ATS_L30 { get; set; }
        public int ATS_L34 { get; set; }
        public int ATS_L35 { get; set; }
        public int ATS_L36 { get; set; }
        public int ATS_L37 { get; set; }
        public int ATS_L40 { get; set; }
        public int ATS_L41 { get; set; }
        public int ATS_L55 { get; set; }
        public int ATS_L60 { get; set; }
        public int ATS_L70 { get; set; }
        public int ATS_L91 { get; set; }
        public int ATS_L56 { get; set; }
        public int ATS_L57 { get; set; }
        public string Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }

        public string ConnectorType { get; set; }

        public string Party { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SCSInventoryFeed() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SCSInventoryFeed(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SCSInventoryFeed(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(SCSInventoryFeed.TableName))
            {
                SCSInventoryFeed.TableName = "SCSInventoryFeed";
            }

            if (string.IsNullOrEmpty(SCSInventoryFeed.ViewName))
            {
                SCSInventoryFeed.ViewName = "VW_TempSCSInventoryFeed";
            }

            if (string.IsNullOrEmpty(SCSInventoryFeed.PrimaryKeyName))
            {
                SCSInventoryFeed.PrimaryKeyName = "CustomerID";
            }

            if (string.IsNullOrEmpty(SCSInventoryFeed.EndingPropertyName))
            {
                SCSInventoryFeed.EndingPropertyName = "CreatedBy";
            }

            if (SCSInventoryFeed.DBProperties == null)
            {
                SCSInventoryFeed.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(SCSInventoryFeed.InsertQueryStart))
            {
                SCSInventoryFeed.InsertQueryStart = PrepareQueries(this, SCSInventoryFeed.TableName, SCSInventoryFeed.EndingPropertyName, ref l_Query, SCSInventoryFeed.DBProperties);
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
        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + SCSInventoryFeed.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + SCSInventoryFeed.TableName + "]";
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

        public bool GetViewList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + SCSInventoryFeed.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + SCSInventoryFeed.ViewName + "]";
            }

            if (!string.IsNullOrEmpty(p_Criteria))
            {
                l_Query += " WHERE " + p_Criteria;
            }

            if (!string.IsNullOrEmpty(p_OrderBy))
            {
                l_Query += " ORDER BY " + p_OrderBy ;
            }

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(SCSInventoryFeed.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SCSInventoryFeed.ViewName, SCSInventoryFeed.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + SCSInventoryFeed.PrimaryKeyName + ", '0'))) FROM " + SCSInventoryFeed.TableName, ref l_Data))
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

            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, string.Empty);

            l_Data.Dispose();

            return Result.GetSuccessResult();
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SCSInventoryFeed.ViewName, SCSInventoryFeed.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SCSInventoryFeed.ViewName, SCSInventoryFeed.PrimaryKeyName), true);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(SCSInventoryFeed.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SCSInventoryFeed.ViewName, SCSInventoryFeed.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, SCSInventoryFeed.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, SCSInventoryFeed.InsertQueryStart, SCSInventoryFeed.EndingPropertyName, SCSInventoryFeed.DBProperties);

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
                SCSInventoryFeed.DBProperties = this.GetType().GetProperties()
                  .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
                  .ToList();

                SCSInventoryFeed.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, SCSInventoryFeed.TableName, SCSInventoryFeed.PrimaryKeyName, SCSInventoryFeed.EndingPropertyName, Connectors.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, "Temp_"+SCSInventoryFeed.TableName, SCSInventoryFeed.PrimaryKeyName);

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

        // ── CustomerID → log table name via SP (pattern-based, DB-validated) ──
        public static string GetLogTableName(string connectionString, string customerID)
        {
            if (string.IsNullOrEmpty(customerID)) return null;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "EXEC [dbo].[Sp_GetInventoryLogTableName] @p_CustomerID", conn))
                    {
                        cmd.Parameters.AddWithValue("@p_CustomerID", customerID);
                        var result = cmd.ExecuteScalar();
                        return (result == null || result == DBNull.Value) ? null : result.ToString();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // ── Returns inventory data columns for a log table via SP ─────────
        private static string[] GetLogTableColumns(string connectionString, string logTableName)
        {
            try
            {
                var columns = new List<string>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "EXEC [dbo].[Sp_GetInventoryLogTableColumns] @p_TableName", conn))
                    {
                        cmd.Parameters.AddWithValue("@p_TableName", logTableName);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                columns.Add(reader.GetString(0));
                        }
                    }
                }
                return columns.ToArray();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // ── Bulk insert a historical snapshot into a per-customer log table ──
        // batchID  : InventoryBatchWise.BatchID
        // logType  : "DOWNLOAD"  (ERP → eSyncMate)  or  "UPLOAD" (eSyncMate → Partner)
        // dataTable: the source DataTable (columns are matched by name; missing cols stay NULL)
        public static void BulkInsertToLogTable(
            string    connectionString,
            string    logTableName,
            DataTable dataTable,
            string    batchID,
            string    logType)
        {
            if (dataTable == null || dataTable.Rows.Count == 0) return;
            if (string.IsNullOrEmpty(logTableName))             return;
            if (string.IsNullOrEmpty(batchID))                  return;

            try
            {
                string[] inventoryCols = GetLogTableColumns(connectionString, logTableName);
                int      colCount      = inventoryCols.Length;

                // ── Pre-resolve column mapping ONCE (not per row) ─────────────────
                // resolvedCols[i] = actual source column name to read from srcRow
                //                   null = column handled specially (Status)
                // statusIdx       = index of Status column, or -1 if absent
                string[] resolvedCols = new string[colCount];
                int      statusIdx    = -1;
                string   statusValue  = logType.Equals("DOWNLOAD", StringComparison.OrdinalIgnoreCase)
                                        ? "Updated" : "Synced";

                for (int i = 0; i < colCount; i++)
                {
                    string col = inventoryCols[i];

                    if (col.Equals("Status", StringComparison.OrdinalIgnoreCase))
                    {
                        statusIdx       = i;
                        resolvedCols[i] = null; // handled via statusValue
                    }
                    else if (dataTable.Columns.Contains(col))
                    {
                        resolvedCols[i] = col;
                    }
                    else
                    {
                        // Case-insensitive fallback — paid once per column, never per row
                        DataColumn found = dataTable.Columns
                            .Cast<DataColumn>()
                            .FirstOrDefault(c => string.Equals(c.ColumnName, col,
                                StringComparison.OrdinalIgnoreCase));
                        resolvedCols[i] = found?.ColumnName;
                    }
                }

                // ── Build log DataTable schema ────────────────────────────────────
                DataTable logData = new DataTable();
                logData.Columns.Add("BatchID", typeof(string));
                logData.Columns.Add("LogType", typeof(string));

                for (int i = 0; i < colCount; i++)
                {
                    Type colType = (i == statusIdx || resolvedCols[i] == null)
                        ? typeof(string)
                        : dataTable.Columns[resolvedCols[i]].DataType;
                    logData.Columns.Add(inventoryCols[i], colType);
                }

                // ── Fill rows — inner loop is index comparisons only ──────────────
                logData.BeginLoadData();
                foreach (DataRow srcRow in dataTable.Rows)
                {
                    DataRow logRow = logData.NewRow();
                    logRow["BatchID"] = batchID;
                    logRow["LogType"] = logType;

                    for (int i = 0; i < colCount; i++)
                    {
                        if (i == statusIdx)
                        {
                            logRow[i + 2] = statusValue;
                        }
                        else
                        {
                            string src = resolvedCols[i];
                            logRow[i + 2] = src != null ? srcRow[src] ?? DBNull.Value : DBNull.Value;
                        }
                    }

                    logData.Rows.Add(logRow);
                }
                logData.EndLoadData();

                // ── SqlBulkCopy ───────────────────────────────────────────────────
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlBulkCopy bulk = new SqlBulkCopy(conn))
                    {
                        bulk.DestinationTableName = logTableName;
                        bulk.BulkCopyTimeout      = 600;
                        bulk.BatchSize            = 5000;

                        foreach (DataColumn col in logData.Columns)
                            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

                        bulk.WriteToServer(logData);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // ── Update a single item's Status in the log table (e.g. Walmart node errors → 'Error') ──
        public bool UpdateLogItemStatus(string logTableName, string batchID, string itemId, string status)
        {
            if (string.IsNullOrEmpty(logTableName)) return false;

            try
            {
                string query = $"UPDATE [{logTableName}] SET [Status] = @Status WHERE BatchID = @BatchID AND ItemId = @ItemId";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Status",  SqlDbType.VarChar)   { Value = status  },
                    new SqlParameter("@BatchID", SqlDbType.NVarChar)  { Value = batchID },
                    new SqlParameter("@ItemId",  SqlDbType.VarChar)   { Value = itemId  }
                };

                return this.Connection.Execute(query, p_SQLParams: parameters);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // ── Bulk insert per-item ERP/upload data into customer-wise SCSInventoryFeedData table ──
        // customerID  : used to resolve table SCSInventoryFeedData_[customerID]
        // sourceTable : must have ItemId and CustomerId columns
        // type        : 'ERP-RVD' (download) or any other type
        // perItemJson : actual JSON received/sent per item — same index order as sourceTable rows
        public static void BulkInsertFeedData(
            string        connectionString,
            string        customerID,
            DataTable     sourceTable,
            string        batchID,
            string        type,
            IList<string> perItemJson)
        {
            if (sourceTable == null || sourceTable.Rows.Count == 0) return;
            if (string.IsNullOrEmpty(customerID))                   return;
            if (string.IsNullOrEmpty(batchID))                      return;
            if (perItemJson == null || perItemJson.Count == 0)       return;

            try
            {
                string feedTable = "SCSInventoryFeedData_" + customerID;

                // Validate customer table exists
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand checkCmd = new SqlCommand(
                        "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME=@t", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@t", feedTable);
                        int exists = (int)checkCmd.ExecuteScalar();
                        if (exists == 0) return;
                    }

                    // Build feed DataTable
                    DataTable feedData = new DataTable();
                    feedData.Columns.Add("CustomerId",  typeof(string));
                    feedData.Columns.Add("ItemId",      typeof(string));
                    feedData.Columns.Add("Type",        typeof(string));
                    feedData.Columns.Add("Data",        typeof(string));
                    feedData.Columns.Add("BatchID",     typeof(string));
                    feedData.Columns.Add("CreatedDate", typeof(DateTime));
                    feedData.Columns.Add("CreatedBy",   typeof(int));

                    int rowCount = Math.Min(sourceTable.Rows.Count, perItemJson.Count);

                    feedData.BeginLoadData();
                    for (int i = 0; i < rowCount; i++)
                    {
                        DataRow src = sourceTable.Rows[i];
                        DataRow row = feedData.NewRow();

                        row["CustomerId"]  = src["CustomerID"]?.ToString() ?? customerID;
                        row["ItemId"]      = src["ItemId"]?.ToString() ?? string.Empty;
                        row["Type"]        = type;
                        row["Data"]        = perItemJson[i] ?? string.Empty;
                        row["BatchID"]     = batchID;
                        row["CreatedDate"] = DateTime.Now;
                        row["CreatedBy"]   = 1;

                        feedData.Rows.Add(row);
                    }
                    feedData.EndLoadData();

                    using (SqlBulkCopy bulk = new SqlBulkCopy(conn))
                    {
                        bulk.DestinationTableName = feedTable;
                        bulk.BulkCopyTimeout      = 600;
                        bulk.BatchSize            = 5000;

                        foreach (DataColumn col in feedData.Columns)
                            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

                        bulk.WriteToServer(feedData);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void BulkInsert(string connectionString, string destinationTableName, DataTable dataTable)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = destinationTableName + TableName;
                    bulkCopy.BulkCopyTimeout = 600; 

                    try
                    {
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
        }

        public bool UpdateItemStatus(string p_ItemID,string p_CustomerId )
        {
            string DeleteQuery = $"UPDATE {SCSInventoryFeed.TableName} SET Status = 'SYNCED', ModifiedDate = GETDATE() ";

            DeleteQuery += $"WHERE ItemID = '{p_ItemID}' AND CustomerID = '{p_CustomerId}'";

            return this.Connection.Execute(DeleteQuery);
        }
        public bool UpdateItemStatusError(string p_ItemID, string p_CustomerId)
        {
            string DeleteQuery = $"UPDATE {SCSInventoryFeed.TableName} SET Status = 'ERROR', ModifiedDate = GETDATE() ";

            DeleteQuery += $"WHERE ItemID = '{p_ItemID}' AND CustomerID = '{p_CustomerId}'";

            return this.Connection.Execute(DeleteQuery);
        }

        /// <summary>
        /// Bulk update item status for multiple items in a single database call
        /// </summary>
        public void BulkUpdateItemStatus(string connectionString, DataTable items)
        {
            if (items == null || items.Rows.Count == 0)
                return;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Build comma-separated list of ItemId and CustomerId pairs
                StringBuilder whereClause = new StringBuilder();
                bool first = true;

                foreach (DataRow row in items.Rows)
                {
                    string itemId = row["ItemId"]?.ToString()?.Replace("'", "''") ?? "";
                    string customerId = row["CustomerId"]?.ToString()?.Replace("'", "''") ?? "";

                    if (!first)
                        whereClause.Append(" OR ");

                    whereClause.Append($"(ItemID = '{itemId}' AND CustomerID = '{customerId}')");
                    first = false;
                }

                string updateQuery = $@"UPDATE {SCSInventoryFeed.TableName}
                                        SET Status = 'SYNCED', ModifiedDate = GETDATE()
                                        WHERE {whereClause}";

                using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                {
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public bool InsertInventoryBatchWise(InventoryBatchWise p_InventoryBatchWise)
        {
            string insertQuery = "INSERT INTO InventoryBatchWise (BatchID, StartDate, FinishDate, Status, RouteType,CustomerID) " +
                        "VALUES (@BatchID, GETDATE(), NULL, @Status, @RouteType,@CustomerID)";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@BatchID", SqlDbType.VarChar) { Value = p_InventoryBatchWise.BatchID },
                new SqlParameter("@Status", SqlDbType.VarChar) { Value = p_InventoryBatchWise.Status },
                new SqlParameter("@RouteType", SqlDbType.VarChar) { Value = p_InventoryBatchWise.RouteType },
                new SqlParameter("@CustomerID", SqlDbType.VarChar) { Value = p_InventoryBatchWise.CustomerID }

            };

            return this.Connection.Execute(insertQuery, p_SQLParams: parameters);
        }

        public bool APIShipNode(string APIName, ref DataTable p_dataTable)
        {
            string l_Query = string.Empty;

            l_Query = "SELECT * FROM WalmartShipNodes";

            return this.Connection.GetData(l_Query, ref p_dataTable);
        }

        public bool TargetPlusShipNode(string CustomerID,ref DataTable p_dataTable)
        {
            string l_Query = string.Empty;

            l_Query = $"SELECT * FROM TargetPlusShipNodes WHERE CustomerID = '{CustomerID}'";

            return this.Connection.GetData(l_Query, ref p_dataTable);
        }

        public string GetLowesStockImportHeader()
        {
            DataTable dt = new DataTable();
            this.Connection.GetDataSP("Sp_Lowes_GetStockImportHeader", ref dt);

            if (dt.Rows.Count > 0)
                return dt.Rows[0]["HeaderRow"].ToString();

            return string.Empty;
        }

        public bool UpdateInventoryBatchWise(InventoryBatchWise p_InventoryBatchWise)
        {
            string updateQuery = "UPDATE InventoryBatchWise SET FinishDate = GETDATE(), Status = @Status WHERE BatchID = @BatchID";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Status", SqlDbType.VarChar) { Value = p_InventoryBatchWise.Status },
                new SqlParameter("@BatchID", SqlDbType.VarChar) { Value = p_InventoryBatchWise.BatchID }
            };

            return this.Connection.Execute(updateQuery, p_SQLParams: parameters);
        }


        public bool UpdateInventoryBatchWisePageCount(InventoryBatchWise p_InventoryBatchWise)
        {
            string UpdateQuery = $"UPDATE InventoryBatchWise SET PageCount = {p_InventoryBatchWise.PageCount} ";
            UpdateQuery += $"WHERE BatchID = '{p_InventoryBatchWise.BatchID}'";

            return this.Connection.Execute(UpdateQuery);
        }



        public bool InsertInventoryBatchWiseFeedDetail(string BatchID,string Status,string FeedDocumentID,string CustomerID)
        {
            try
            {
                string insertQuery = "INSERT INTO InventoryBatchWiseFeedDetail (BatchID, CreatedDate, Status, FeedDocumentID,CustomerID) " +
                        "VALUES (@BatchID, GETDATE(), @Status, @FeedDocumentID,@CustomerID)";

                SqlParameter[] parameters = new SqlParameter[]
                {
                new SqlParameter("@BatchID", SqlDbType.VarChar) { Value = BatchID },
                new SqlParameter("@Status", SqlDbType.VarChar) { Value = Status },
                new SqlParameter("@FeedDocumentID", SqlDbType.VarChar) { Value = FeedDocumentID },
                new SqlParameter("@CustomerID", SqlDbType.VarChar) { Value = CustomerID }

                };

                return this.Connection.Execute(insertQuery, p_SQLParams: parameters);
            }
            catch (Exception)
            {
                throw;
            }
        }


        public bool UpdateInventoryBatchWiseFeedDetail(string BatchID, string FeedDocumentID, string Status, string Data)
        {
            string updateQuery = "UPDATE InventoryBatchWiseFeedDetail SET Status = @Status, Data = @Data " +
                                 "WHERE BatchID = @BatchID AND FeedDocumentID = @FeedDocumentID";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@BatchID", SqlDbType.VarChar) { Value = BatchID },
                new SqlParameter("@FeedDocumentID", SqlDbType.VarChar) { Value = FeedDocumentID },
                new SqlParameter("@Status", SqlDbType.VarChar) { Value = Status },
                new SqlParameter("@Data", SqlDbType.NVarChar) { Value = Data }
            };

            return this.Connection.Execute(updateQuery, p_SQLParams: parameters);
        }

        public void BulkNewInsertData(string connectionString, string destinationTableName, DataTable dataTable)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = destinationTableName;
                    bulkCopy.BulkCopyTimeout = 600;

                    try
                    {
                        // Only map the columns you want to insert into the destination table
                        bulkCopy.ColumnMappings.Add("CustomerId", "CustomerId");
                        bulkCopy.ColumnMappings.Add("ItemId", "ItemId");
                        bulkCopy.ColumnMappings.Add("Type", "Type");
                        bulkCopy.ColumnMappings.Add("Data", "Data");
                        bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                        bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                        bulkCopy.ColumnMappings.Add("BatchID", "BatchID");

                        // Perform bulk insert
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error during bulk insert", ex);
                    }
                }
            }
        }

        public void BulkAmazonFeedData(string connectionString, string destinationTableName, DataTable dataTable)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = destinationTableName;
                    bulkCopy.BulkCopyTimeout = 600;

                    try
                    {
                        
                        bulkCopy.ColumnMappings.Add("BatchID", "BatchID");
                        bulkCopy.ColumnMappings.Add("ItemID", "ItemID");
                        bulkCopy.ColumnMappings.Add("CustomerID", "CustomerID");
                        bulkCopy.ColumnMappings.Add("MessageID", "MessageID");
                        bulkCopy.ColumnMappings.Add("FeedDocumentID", "FeedDocumentID");
                        bulkCopy.ColumnMappings.Add("Data", "Data");

                        // Perform bulk insert
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error during bulk insert", ex);
                    }
                }
            }
        }

        public void BulkLowesFeedData(string connectionString, string destinationTableName, DataTable dataTable)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = destinationTableName;
                    bulkCopy.BulkCopyTimeout = 600;

                    try
                    {
                        bulkCopy.ColumnMappings.Add("BatchID", "BatchID");
                        bulkCopy.ColumnMappings.Add("ItemID", "ItemID");
                        bulkCopy.ColumnMappings.Add("CustomerID", "CustomerID");
                        bulkCopy.ColumnMappings.Add("ImportId", "ImportId");
                        bulkCopy.ColumnMappings.Add("Data", "Data");

                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error during bulk insert", ex);
                    }
                }
           }
        }

        public bool UpdateSCSLowesFeedData(string p_BatchID, string p_ImportId, string p_Guid)
        {
            string updateQuery = $"UPDATE SCSLowesFeedData SET ImportId = '{p_ImportId}' ";
            updateQuery += $"WHERE ImportId = '{p_Guid}' AND BatchID = '{p_BatchID}'";

            return this.Connection.Execute(updateQuery);
        }

        public bool LowesUpdateStatusSCSInventoryFeed(string CustomerID, string BatchID, string ImportId)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_Lowes_UpdateStatusSCSInventoryFeed";

                PublicFunctions.FieldToParam(CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;

                PublicFunctions.FieldToParam(BatchID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                PublicFunctions.FieldToParam(ImportId, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                return this.Connection.Execute(l_Query);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // ===== KNOTT-SPECIFIC METHODS =====

        public string GetKnotStockImportHeader()
        {
            DataTable dt = new DataTable();
            this.Connection.GetDataSP("Sp_Knot_GetStockImportHeader", ref dt);

            if (dt.Rows.Count > 0)
                return dt.Rows[0]["HeaderRow"].ToString();

            return string.Empty;
        }

        public void BulkKnotFeedData(string connectionString, string destinationTableName, DataTable dataTable)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = destinationTableName;
                    bulkCopy.BulkCopyTimeout = 600;

                    bulkCopy.ColumnMappings.Add("BatchID", "BatchID");
                    bulkCopy.ColumnMappings.Add("ItemID", "ItemID");
                    bulkCopy.ColumnMappings.Add("CustomerID", "CustomerID");
                    bulkCopy.ColumnMappings.Add("ImportId", "ImportId");
                    bulkCopy.ColumnMappings.Add("Data", "Data");

                    try
                    {
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error during bulk insert", ex);
                    }
                }
            }
        }

        public bool UpdateSCSKnotFeedData(string p_BatchID, string p_ImportId, string p_Guid)
        {
            string updateQuery = $"UPDATE SCSKnotFeedData SET ImportId = '{p_ImportId}' ";
            updateQuery += $"WHERE ImportId = '{p_Guid}' AND BatchID = '{p_BatchID}'";

            return this.Connection.Execute(updateQuery);
        }

        public bool KnotUpdateStatusSCSInventoryFeed(string CustomerID, string BatchID, string ImportId)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_Knot_UpdateStatusSCSInventoryFeed";

                PublicFunctions.FieldToParam(CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;

                PublicFunctions.FieldToParam(BatchID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                PublicFunctions.FieldToParam(ImportId, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                return this.Connection.Execute(l_Query);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool UpdateSCSAmazonFeedData(string p_BatchID, string p_FeedDocumentID,string p_Guid)
        {
            string updateQuery = $"UPDATE SCSAmazonFeedData SET FeedDocumentID = '{p_FeedDocumentID}' ";

            updateQuery += $"WHERE FeedDocumentID = '{p_Guid}' AND BatchID = '{p_BatchID}'";

            return this.Connection.Execute(updateQuery);
        }
        public Result SaveData(string type, string CustomerId, string ItemId, string Data, int userNo, string batchID)
        {
            Result l_Result = Result.GetFailureResult();

            try
            {
                string feedTable = "SCSInventoryFeedData_" + CustomerId;

                string query = $@"INSERT INTO [{feedTable}]
                    (CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy)
                    VALUES (@CustomerId, @ItemId, @Type, @Data, @BatchID, GETDATE(), @CreatedBy)";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@CustomerId", SqlDbType.VarChar)   { Value = CustomerId ?? string.Empty },
                    new SqlParameter("@ItemId",     SqlDbType.VarChar)   { Value = ItemId     ?? string.Empty },
                    new SqlParameter("@Type",       SqlDbType.VarChar)   { Value = type       ?? string.Empty },
                    new SqlParameter("@Data",       SqlDbType.NVarChar)  { Value = Data       ?? string.Empty },
                    new SqlParameter("@BatchID",    SqlDbType.NVarChar)  { Value = batchID    ?? string.Empty },
                    new SqlParameter("@CreatedBy",  SqlDbType.Int)       { Value = userNo }
                };

                bool success = this.Connection.Execute(query, p_SQLParams: parameters);
                if (success) l_Result = Result.GetSuccessResult();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }

            return l_Result;
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
            return ((Connectors)x).Id == ((Connectors)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion
    }
}
