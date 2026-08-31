using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace eSyncMate.DB.Entities
{
    public class CustomerProductCatalog : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int ProductId { get; set; }
        public string Id { get; set; }
        public string CustomerID { get; set; }
        public string Brand { get; set; }
        public string ItemID { get; set; }
        public string UPC { get; set; }
        public string ItemTypeName { get; set; }
        public string ProductRelation { get; set; }
        public string ParentID { get; set; }
        public string ListPrice { get; set; }
        public string MapPrice { get; set; }
        public string OffPrice { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }
        public Customers SourcePartyObject { get; set; }
        public Customers DestinationPartyObject { get; set; }
        public Connectors SourceConnectorObject { get; set; }
        public Connectors DestinationConnectorObject { get; set; }
        public Maps MapObject { get; set; }


        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CustomerProductCatalog() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CustomerProductCatalog(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CustomerProductCatalog(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(CustomerProductCatalog.TableName))
            {
                CustomerProductCatalog.TableName = "SCS_CustomerProductCatalog";
            }

            if (string.IsNullOrEmpty(CustomerProductCatalog.ViewName))
            {
                CustomerProductCatalog.ViewName = "VW_SCS_CustomerProductCatalog";
            }

            if (string.IsNullOrEmpty(CustomerProductCatalog.PrimaryKeyName))
            {
                CustomerProductCatalog.PrimaryKeyName = "ProductId";
            }

            if (string.IsNullOrEmpty(CustomerProductCatalog.EndingPropertyName))
            {
                CustomerProductCatalog.EndingPropertyName = "CreatedBy";
            }

            if (CustomerProductCatalog.DBProperties == null)
            {
                CustomerProductCatalog.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(CustomerProductCatalog.InsertQueryStart))
            {
                CustomerProductCatalog.InsertQueryStart = PrepareQueries(this, CustomerProductCatalog.TableName, CustomerProductCatalog.EndingPropertyName, ref l_Query, CustomerProductCatalog.DBProperties, CustomerProductCatalog.PrimaryKeyName);
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
                l_Query = "SELECT * FROM [" + CustomerProductCatalog.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + CustomerProductCatalog.TableName + "]";
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
                l_Query = "SELECT * FROM [" + CustomerProductCatalog.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + CustomerProductCatalog.ViewName + "]";
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

        public bool GetViewListPaged(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;

            string l_CountQuery = "SELECT COUNT(*) FROM [" + CustomerProductCatalog.ViewName + "]";
            if (!string.IsNullOrEmpty(p_Criteria))
                l_CountQuery += " WHERE " + p_Criteria;

            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0)
                totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [" + CustomerProductCatalog.ViewName + "]"
                : "SELECT " + p_Fields + " FROM [" + CustomerProductCatalog.ViewName + "]";

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            l_Query += !string.IsNullOrEmpty(p_OrderBy)
                ? " ORDER BY " + p_OrderBy
                : " ORDER BY Id DESC";

            int offset = (pageNumber - 1) * pageSize;
            l_Query += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(CustomerProductCatalog.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerProductCatalog.ViewName, CustomerProductCatalog.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + CustomerProductCatalog.PrimaryKeyName + ", '0'))) FROM " + CustomerProductCatalog.TableName, ref l_Data))
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
            if (isOnlyObject)
            {
                return Result.GetSuccessResult();
            }

            return Result.GetSuccessResult();
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerProductCatalog.ViewName, CustomerProductCatalog.PrimaryKeyName));
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(CustomerProductCatalog.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerProductCatalog.ViewName, CustomerProductCatalog.PrimaryKeyName), true);
        }
        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerProductCatalog.ViewName, CustomerProductCatalog.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerProductCatalog.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, CustomerProductCatalog.InsertQueryStart, CustomerProductCatalog.EndingPropertyName, CustomerProductCatalog.DBProperties, CustomerProductCatalog.PrimaryKeyName);

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
                CustomerProductCatalog.DBProperties = this.GetType().GetProperties()
               .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
               .ToList();

                CustomerProductCatalog.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, CustomerProductCatalog.TableName, CustomerProductCatalog.PrimaryKeyName, CustomerProductCatalog.EndingPropertyName, CustomerProductCatalog.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, CustomerProductCatalog.TableName, CustomerProductCatalog.PrimaryKeyName);

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
            return ((CustomerProductCatalog)x).ProductId == ((CustomerProductCatalog)y).ProductId;
        }

        public new int GetHashCode(object obj)
        {
            return this.ProductId;
        }
        #endregion

        public async Task BulkInsertSqlServer(DataTable dataTable, string connectionString, string ERPCustomerID, string FileName, Int16 CreatedBy)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand("SP_" + CustomerProductCatalog.TableName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@p_ERPCustomerID", SqlDbType.NVarChar).Value = ERPCustomerID;
                        command.Parameters.Add("@p_FileName", SqlDbType.NVarChar).Value = FileName;
                        command.Parameters.Add("@p_CreatedBy", SqlDbType.Int).Value = CreatedBy;

                        SqlParameter parameterDataTable = new SqlParameter("@p_DataTable", SqlDbType.Structured)
                        {
                            Value = dataTable,
                            TypeName = CustomerProductCatalog.TableName + "Type"
                        };
                        command.Parameters.Add(parameterDataTable);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid file data: " + ex.Message);
                throw;
            }
        }

        public  bool HistoryCustomerProductCatalog(string p_ERPCustomerID, ref DataTable p_Data)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();
            
            l_SQL = "SELECT ERPCustomerID,FileName,CreatedDate,CreatedBy FROM CustomerProductCatalog_Log WITH (NOLOCK)";
            PublicFunctions.FieldToParam(p_ERPCustomerID, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += " WHERE ERPCustomerID = " + l_Param;


            return Connection.GetData(l_SQL, ref p_Data);
        }


        public DataTable ProductCatalogFileHeaderColumn(string p_CustomerID, string p_ItemTypeID, int p_UserNo,ref DataTable p_Data )
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_ProductCatalogFileHeaderColumn";

                PublicFunctions.FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query +=  l_Param;
                PublicFunctions.FieldToParam(p_ItemTypeID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param ;

                this.Connection.GetDataSP(l_Query,ref p_Data);
            }
            catch (Exception ex)
            {
                throw;
            }

            return p_Data;
        }


        public DataTable ProductPricesFileHeaderColumn(string p_CustomerID, int p_UserNo, ref DataTable p_Data)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_ProductPricesFileHeaderColumn";

                PublicFunctions.FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                this.Connection.GetDataSP(l_Query, ref p_Data);
            }
            catch (Exception ex)
            {
                throw;
            }

            return p_Data;
        }


        public DataTable SaveCustomerProductCatalog(string p_CustomerID, int p_UserNo,ref DataTable l_Data)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            Boolean result = false;

            try
            {
                l_Query = "EXEC Sp_SaveCustomerProductCatalog";

                PublicFunctions.FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;
               
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                this.Connection.GetDataSP(l_Query, ref l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }

            return l_Data;
        }

        public DataTable SaveProductPrices(string p_CustomerID, int p_UserNo,ref DataTable l_Data )
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            Boolean result = false;

            try
            {
                l_Query = "EXEC Sp_ProductPrices";

                PublicFunctions.FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;

                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

               this.Connection.GetDataSP(l_Query, ref l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }

            return l_Data;
        }

        public bool UpdateStatus(string p_ItemID , string p_VariationType,string p_Status,string id = "",string CustomerID = "",int RetryCount = 0)
        {
            string updateQuery = $"UPDATE SCS_CustomerProductCatalog " +
                     $"SET SyncStatus = '{p_Status}' , RetryCount = {RetryCount} ";

            if (!string.IsNullOrEmpty(id))
            {
                updateQuery += $", id = '{id}' ";
            }

            updateQuery += $"WHERE ItemID = '{p_ItemID}' AND CustomerID = '{CustomerID}'";

            return this.Connection.Execute(updateQuery);
        }

        public bool DeleteCustomerProductCatalog(string p_ItemID, string p_VariationType,string CustomerID)
        {
            string DeleteQuery = $"UPDATE SCS_CustomerProductCatalog SET SyncStatus = 'DELETED', ModifiedDate = GETDATE() ";

            DeleteQuery += $"WHERE ItemID = '{p_ItemID}' AND CustomerID = '{CustomerID}'";

            return this.Connection.Execute(DeleteQuery);
        }

        /// <summary>
        /// Marks product as DELETED only if inventory was uploaded via TargetPlusInventoryFeedWHSWise.
        /// Returns true if the product was marked DELETED, false if inventory not yet uploaded.
        /// </summary>
        public bool DeleteIfInventorySynced(string p_ItemID, string CustomerID)
        {
            try
            {
                DataTable dt = new DataTable();
                string l_Query = $"EXEC Sp_DeleteProductCatalogIfInventorySynced '{p_ItemID.Replace("'", "''")}', '{CustomerID.Replace("'", "''")}'";
                this.Connection.GetData(l_Query, ref dt);

                if (dt.Rows.Count > 0)
                {
                    return Convert.ToInt32(dt.Rows[0]["Deleted"]) == 1;
                }

                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool CustomerProductCatalogPrices(string CustomerID,string p_ItemID,string ProductID,string Status)
        {
            try
            {
                string InsertQuery = $@"MERGE INTO CustomerProductCatalogPrices AS D
                                        USING(SELECT '{CustomerID}', '{p_ItemID}', '{ProductID}', '{Status}') AS S(CustomerID, ItemId, ProductID, [Status])
                                        ON(D.CustomerID = S.CustomerID AND D.ItemiD = S.ItemID)
                                        WHEN MATCHED THEN
                                            UPDATE SET D.ProductID = S.ProductID, D.[Status] = S.[Status]
                                        WHEN NOT MATCHED THEN
                                            INSERT(CustomerID, ItemId, ProductID, Status)
                                            VALUES('{CustomerID}', '{p_ItemID}', '{ProductID}', '{Status}'); ";

                return this.Connection.Execute(InsertQuery);
            }
            catch (Exception)
            {
            }

            return false;
        }

        public Result SaveData(string type, string Data, int userNo)
        {
            Result l_Result = Result.GetFailureResult();

            try
            {
                SCS_CustomerProductCatalogData SCS_CustomerProductCatalogData = new SCS_CustomerProductCatalogData();

                SCS_CustomerProductCatalogData.UseConnection(string.Empty, this.Connection);

                SCS_CustomerProductCatalogData.CreatedBy = userNo;
                SCS_CustomerProductCatalogData.CreatedDate = DateTime.Now;
                SCS_CustomerProductCatalogData.Data = Data;
                SCS_CustomerProductCatalogData.Type = type;
                SCS_CustomerProductCatalogData.ProductId = this.ProductId;

                l_Result = SCS_CustomerProductCatalogData.SaveNew();
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

        public Result DeleteWithType(int ProductId, string Type)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Process = this.Connection.Execute($"DELETE FROM SCS_CustomerProductCatalogData WHERE ProductId = {ProductId} AND [Type] = '{Type}'");

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
            }
            finally
            {
            }

            return l_Result;
        }

        /// <summary>
        /// Same paged list, plus the latest error payload for each row so the grid can show it
        /// on hover. Errors live in SCS_CustomerProductCatalogData as RSP-ERR (marketplace),
        /// REQ-ERR (request rejected) or Internal (validation done here).
        /// The page is taken first, so the lookup only runs for the rows being displayed.
        /// </summary>
        public bool GetViewListPagedWithError(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;

            string l_CountQuery = "SELECT COUNT(*) FROM [" + CustomerProductCatalog.ViewName + "]";
            if (!string.IsNullOrEmpty(p_Criteria))
                l_CountQuery += " WHERE " + p_Criteria;

            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0)
                totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_OrderBy = !string.IsNullOrEmpty(p_OrderBy) ? p_OrderBy : "ProductId DESC";
            int offset = (pageNumber - 1) * pageSize;

            string l_Query = "WITH PagedCatalog AS (SELECT "
                           + (string.IsNullOrEmpty(p_Fields) ? "*" : p_Fields)
                           + " FROM [" + CustomerProductCatalog.ViewName + "]";

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            l_Query += $" ORDER BY {l_OrderBy} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY)";

            // A rejection carries no *-ERR row: the reasons sit inside the RSP-JSON response, so that
            // payload is used for REJECTED and ERROR rows, and elsewhere only when it really carries
            // errors. PENDING is left out on purpose -- a pending item is still being validated, so its
            // previous attempt's errors are not reported. Empty payloads are skipped so a blank RSP-ERR
            // row cannot shadow a usable one.
            // The status compared here is the view's, which folds SYNCED/DELETED into 'Published'
            // and APPROVED/APPROVED_PR into 'Approved'.
            // A transport failure (timeout, gateway, rate limit) says nothing about the product, so it
            // must not become the item's reported error -- the marketplace's real rejection reason, or
            // a validation finding of ours, has to win. IsTransientResponse makes the same call in the
            // route but the verdict is never stored: both kinds are written as REQ-ERR/RSP-ERR carrying
            // only the response body, so matching the text is all that is available here.
            // Data is NVARCHAR(MAX); the 300-char head keeps this to the first LOB page.
            const string l_TransientNoise = " AND NOT (D.Type IN ('REQ-ERR', 'RSP-ERR')"
                                          + " AND (H.DataHead LIKE '%upstream%'"
                                          + "      OR H.DataHead LIKE '%timeout%'"
                                          + "      OR H.DataHead LIKE '%timed out%'"
                                          + "      OR H.DataHead LIKE '%gateway%'"
                                          + "      OR H.DataHead LIKE '%service unavailable%'"
                                          + "      OR H.DataHead LIKE '%temporarily unavailable%'"
                                          + "      OR H.DataHead LIKE '%internal server error%'"
                                          + "      OR H.DataHead LIKE '%connection%refused%'"
                                          + "      OR H.DataHead LIKE '%too many requests%'"
                                          + "      OR H.DataHead LIKE '%rate limit%'))";

            l_Query += " SELECT P.*, ERR.ErrorData, ERR.ErrorType, ERR.ErrorDate FROM PagedCatalog P"
                    + " OUTER APPLY (SELECT TOP 1 LEFT(D.Data, 8000) AS ErrorData, D.Type AS ErrorType, D.CreatedDate AS ErrorDate"
                    + " FROM [SCS_CustomerProductCatalogData] D"
                    + " CROSS APPLY (SELECT DataHead = LOWER(SUBSTRING(D.Data, 1, 300))) H"
                    + " WHERE D.ProductId = P.ProductId"
                    + " AND DATALENGTH(D.Data) > 0"
                    + l_TransientNoise
                    // PRD/UNL/STA/LOG-ERR are the per-operation types; REQ-ERR and RSP-ERR are their
                    // predecessors and stay listed so rows written before the split still show.
                    + " AND (D.Type IN ('PRD-ERR', 'UNL-ERR', 'STA-ERR', 'LOG-ERR', 'RSP-ERR', 'REQ-ERR', 'Internal')"
                    + "      OR (D.Type IN ('STA-RSP', 'RSP-JSON')"
                    + "          AND (P.SyncStatus IN ('REJECTED','Error')"
                    // A successful status response still carries the key as an empty array
                    // ("errors":[]), so matching the key alone reports every approval as an error.
                    // Only a populated array ("errors":[{...) is a real finding. Whitespace is stripped
                    // first so the test holds whether or not the marketplace pretty-prints its JSON --
                    // missing a genuine rejection would be worse than the false positive this replaces.
                    + "               OR (CHARINDEX('\"errors\":[{',"
                    + "                      REPLACE(REPLACE(REPLACE(REPLACE(LEFT(D.Data, 8000), ' ', ''), CHAR(13), ''), CHAR(10), ''), CHAR(9), '')) > 0"
                    + "                   AND P.SyncStatus NOT IN ('Published', 'Approved', 'Pending')))))"
                    + " ORDER BY CASE WHEN D.Type IN ('STA-RSP', 'RSP-JSON') THEN 1 ELSE 0 END, D.CreatedDate DESC, D.Id DESC) ERR"
                    + $" ORDER BY {l_OrderBy}";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// Builds a quoted, comma-separated IN() list from the supplied item ids.
        /// Single quotes are doubled so a value cannot break the statement.
        /// </summary>
        private static string BuildItemIdList(IEnumerable<string> p_ItemIDs)
        {
            var l_Values = new List<string>();

            foreach (string l_Item in p_ItemIDs)
            {
                if (string.IsNullOrWhiteSpace(l_Item)) continue;
                l_Values.Add("'" + l_Item.Trim().Replace("'", "''") + "'");
            }

            return string.Join(",", l_Values);
        }

        /// <summary>
        /// What a delete would touch, for the confirmation step: one row per item id found,
        /// with how many catalog rows and how many customers it appears under.
        /// </summary>
        public bool GetProductsByItemIDs(List<string> p_ItemIDs, ref DataTable p_Data)
        {
            string l_List = BuildItemIdList(p_ItemIDs);

            if (string.IsNullOrEmpty(l_List))
            {
                return false;
            }

            string l_Query = "SELECT ItemID, COUNT(*) AS ProductCount, COUNT(DISTINCT CustomerID) AS CustomerCount,"
                           + " STUFF((SELECT ', ' + C2.CustomerID FROM [" + CustomerProductCatalog.TableName + "] C2"
                           + "        WHERE C2.ItemID = C1.ItemID GROUP BY C2.CustomerID FOR XML PATH('')), 1, 2, '') AS Customers"
                           + " FROM [" + CustomerProductCatalog.TableName + "] C1"
                           + " WHERE ItemID IN (" + l_List + ")"
                           + " GROUP BY ItemID ORDER BY ItemID";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// Permanently removes catalog rows for the supplied item ids across every customer,
        /// together with their detail rows. Header and detail go in one transaction so the
        /// 2M-row data table can never be left with orphans.
        /// </summary>
        public Result DeleteProductsByItemIDs(List<string> p_ItemIDs, out int p_DeletedProducts, out int p_DeletedData)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            p_DeletedProducts = 0;
            p_DeletedData = 0;

            string l_List = BuildItemIdList(p_ItemIDs);

            if (string.IsNullOrEmpty(l_List))
            {
                return l_Result;
            }

            try
            {
                // Count first so the caller can report what was removed
                DataTable l_Counts = new DataTable();
                string l_CountQuery = "SELECT"
                    + " (SELECT COUNT(*) FROM [" + CustomerProductCatalog.TableName + "] WHERE ItemID IN (" + l_List + ")) AS ProductCount,"
                    + " (SELECT COUNT(*) FROM [SCS_CustomerProductCatalogData] WHERE ProductId IN"
                    + "   (SELECT ProductId FROM [" + CustomerProductCatalog.TableName + "] WHERE ItemID IN (" + l_List + "))) AS DataCount";

                if (Connection.GetData(l_CountQuery, ref l_Counts) && l_Counts.Rows.Count > 0)
                {
                    p_DeletedProducts = PublicFunctions.ConvertNullAsInteger(l_Counts.Rows[0]["ProductCount"], 0);
                    p_DeletedData = PublicFunctions.ConvertNullAsInteger(l_Counts.Rows[0]["DataCount"], 0);
                }
                l_Counts.Dispose();

                if (p_DeletedProducts == 0)
                {
                    return Result.GetNoRecordResult();
                }

                l_Trans = this.Connection.BeginTransaction();

                // Detail rows first — they are keyed on the products about to go
                bool l_Process = this.Connection.Execute(
                    "DELETE FROM [SCS_CustomerProductCatalogData] WHERE ProductId IN"
                  + " (SELECT ProductId FROM [" + CustomerProductCatalog.TableName + "] WHERE ItemID IN (" + l_List + "))");

                if (l_Process)
                {
                    l_Process = this.Connection.Execute(
                        "DELETE FROM [" + CustomerProductCatalog.TableName + "] WHERE ItemID IN (" + l_List + ")");
                }

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
                        p_DeletedProducts = 0;
                        p_DeletedData = 0;
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                p_DeletedProducts = 0;
                p_DeletedData = 0;

                throw;
            }

            return l_Result;
        }

        public bool GetProductsData(int ID, ref DataTable p_Data)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();

            l_SQL = "SELECT * FROM  VW_SCS_CustomerProductCatalogData WITH (NOLOCK)";
            PublicFunctions.FieldToParam(ID, ref l_Param, Declarations.FieldTypes.Number);
            l_SQL += " WHERE ProductId = " + l_Param;

            return Connection.GetData(l_SQL, ref p_Data);
        }

        public DataTable RejectedProductCatalog(string p_CustomerID, int p_UserNo, ref DataTable p_Data)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_ProductCatalogRejected";

                PublicFunctions.FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                this.Connection.GetDataSP(l_Query, ref p_Data);
            }
            catch (Exception ex)
            {
                throw;
            }

            return p_Data;
        }

        public bool DeleteScheduleProductCatalog(string CustomerID)
        {
            string DeleteQuery = $"DELETE SCS_CustomerProductCatalog ";

            DeleteQuery += $"WHERE SyncStatus = 'DELETED' AND ModifiedDate <= DATEADD(HOUR, -24, GETDATE()) AND CustomerID = '{CustomerID}'";

            return this.Connection.Execute(DeleteQuery);
        }

        public bool GetPrepareItemData(int UserID,string CustomerID,string ItemType, ref DataTable p_Data)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();

            l_SQL = $"SELECT * FROM  SCS_PrepareItemData WITH (NOLOCK) WHERE CustomerID = '{CustomerID}' AND ItemTypeID = '{ItemType}' AND CSVData IS NOT NULL ";
            
            return Connection.GetData(l_SQL, ref p_Data);
        }

        public bool InsertPrepareItemData(int UserID, string CustomerID, string ItemType)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();

            string insertQuery = "INSERT INTO SCS_PrepareItemData (UserID, ItemTypeID, Status, CustomerID, CreatedDate) " +
                        "VALUES (@UserID, @ItemTypeID, 'NEW', @CustomerID, GETDATE())";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", SqlDbType.Int) { Value = UserID },
                new SqlParameter("@ItemTypeID", SqlDbType.VarChar) { Value =  ItemType },
                new SqlParameter("@CustomerID", SqlDbType.VarChar) { Value = CustomerID }
            };

            return this.Connection.Execute(insertQuery, p_SQLParams: parameters);
        }

        public bool GetPrepareItemDataNewStatus(string CustomerID,ref DataTable p_Data)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();

            l_SQL = $"SELECT * FROM  SCS_PrepareItemData WITH (NOLOCK) WHERE  CustomerID = '{CustomerID}'";

            return Connection.GetData(l_SQL, ref p_Data);
        }

        public bool GetCustomerProductCatalogPrices(string CustomerID, ref DataTable p_Data)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();

            l_SQL = $"SELECT * FROM  CustomerProductCatalogPrices WITH (NOLOCK) WHERE  CustomerID = '{CustomerID}'";

            return Connection.GetData(l_SQL, ref p_Data);
        }

        //public Result UpdateItemsDataStatus(string ItemTypeID, string CustomerID = "",int UserID  = 0,string FileName = "",string CsvData = "")
        //{
        //    Result l_Result = Result.GetFailureResult();
        //    bool l_Trans = false;
        //    bool l_Process = false;

        //    try
        //    {
        //        l_Trans = this.Connection.BeginTransaction();

        //        l_Process = this.Connection.Execute($"UPDATE SCS_PrepareItemData SET Status = 'COMPLETED',FileName = '{FileName}',CSVData = {CsvData} WHERE ItemTypeID = '{ItemTypeID}' AND [CustomerID] = '{CustomerID}' AND [UserID] = '{UserID}'");

        //        if (l_Trans)
        //        {
        //            if (l_Process)
        //            {
        //                this.Connection.CommitTransaction();
        //                l_Result = Result.GetSuccessResult();
        //            }
        //            else
        //            {
        //                this.Connection.RollbackTransaction();
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        if (l_Trans)
        //        {
        //            this.Connection.RollbackTransaction();
        //        }

        //        throw;
        //    }
        //    finally
        //    {
        //    }

        //    return l_Result;
        //}




        public bool UpdateItemsDataStatus(string ItemTypeID, string CustomerID = "", string FileName = "", string CsvData = "")
        {
            bool isUpdated = false;
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            l_CustomerProductCatalog.UseConnection(string.Empty, this.Connection); // Assuming this.Connection is your existing connection

            try
            {
                using (SqlConnection connection = new SqlConnection(l_CustomerProductCatalog.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [SCS_PrepareItemData] " +
                                   "SET FileName = @FileName , CSVData = @CSVData,ModifiedDate = GETDATE() " +
                                   " WHERE ItemTypeID = @ItemTypeID  AND CustomerID = @CustomerID" ;

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@FileName", SqlDbType.NVarChar).Value = FileName;
                        command.Parameters.Add("@CSVData", SqlDbType.VarChar).Value = CsvData;
                        command.Parameters.Add("@ItemTypeID", SqlDbType.NVarChar).Value = ItemTypeID;
                        command.Parameters.Add("@CustomerID", SqlDbType.NVarChar).Value = CustomerID;

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


        public DataTable DownloadItemsData(string p_CustomerID, int p_UserID, string p_ItemType, ref DataTable p_Data)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_DownloadItemsData";

                PublicFunctions.FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;

                PublicFunctions.FieldToParam(p_ItemType, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                PublicFunctions.FieldToParam(p_UserID, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;

                this.Connection.GetDataSP(l_Query, ref p_Data);
            }
            catch (Exception ex)
            {
                throw;
            }

            return p_Data;
        }

        public bool DeleteItemsData(int p_UserID,string p_CustomerID,string ItemType)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_DeleteItemsData";

                PublicFunctions.FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;

                PublicFunctions.FieldToParam(ItemType, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                PublicFunctions.FieldToParam(p_UserID, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;

                

                return this.Connection.Execute(l_Query);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool GetProductViewList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [VW_SCS_ProductPrices]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [VW_SCS_ProductPrices]";
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

        public bool GetProductViewListPaged(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;

            string l_CountQuery = "SELECT COUNT(*) FROM [VW_SCS_ProductPrices]";
            if (!string.IsNullOrEmpty(p_Criteria))
                l_CountQuery += " WHERE " + p_Criteria;

            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0)
                totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [VW_SCS_ProductPrices]"
                : "SELECT " + p_Fields + " FROM [VW_SCS_ProductPrices]";

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            l_Query += !string.IsNullOrEmpty(p_OrderBy)
                ? " ORDER BY " + p_OrderBy
                : " ORDER BY Id DESC";

            int offset = (pageNumber - 1) * pageSize;
            l_Query += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            return Connection.GetData(l_Query, ref p_Data);
        }



        public bool UpdateSCSProductStatus(string p_ItemID, string p_VariationType, string p_Status, string id = "", string CustomerID = "")
        {
            string updateQuery = $"UPDATE SCS_ProductPrices " +
                     $"SET SyncStatus = '{p_Status}' ";

            if (!string.IsNullOrEmpty(id))
            {
                updateQuery += $", id = '{id}' ";
            }

            updateQuery += $"WHERE ItemID = '{p_ItemID}' AND CustomerID = '{CustomerID}'";

            return this.Connection.Execute(updateQuery);
        }


        public bool DeleteSCSProductStatus(string p_ItemID, string p_VariationType, string CustomerID)
        {
            string DeleteQuery = $"UPDATE SCS_ProductPrices SET SyncStatus = 'DELETED', ModifiedDate = GETDATE() ";

            DeleteQuery += $"WHERE ItemID = '{p_ItemID}' AND CustomerID = '{CustomerID}'";

            return this.Connection.Execute(DeleteQuery);
        }

        public bool DeleteSCSItemsData(string p_ItemTypeID)
        {
            string DeleteQuery = $"DELETE SCS_ItemData";

            DeleteQuery += $"WHERE item_type_id = '{p_ItemTypeID}'";

            return this.Connection.Execute(DeleteQuery);
        }

        public bool DeleteProductCatalogDiscrepencies(string ItemID)
        {
            string DeleteQuery = $"DELETE ProductCatalogDiscrepencies";

            DeleteQuery += $" WHERE ItemID = '{ItemID}'";

            return this.Connection.Execute(DeleteQuery);
        }


        public bool UpdateErrorResolveDate()
        {
            string updateQuery = $"UPDATE ApplicationSettings " +
                     $"SET TagValue = FORMAT(GETDATE(), 'yyyy-MM-dd HH:mm:ss') ";

            updateQuery += $"WHERE TagName = 'ProductMarkErrorResolve' ";

            return this.Connection.Execute(updateQuery);
        }

        public bool UpdateInventoryBacthwiseStatus(string p_batchID, string p_FeedDocumentID, string p_Status, string CustomerID = "",string Data = "")
        {
            return UpdateInventoryBatchWiseStatus(p_batchID, p_FeedDocumentID, p_Status, CustomerID, Data);
        }

        public bool UpdateInventoryBatchWiseStatus(string p_batchID, string p_FeedDocumentID, string p_Status, string CustomerID = "", string Data = "")
        {
            string updateQuery = "UPDATE InventoryBatchWiseFeedDetail " +
                     "SET Status = @Status, Data = @Data " +
                     "WHERE batchID = @BatchID AND CustomerID = @CustomerID AND FeedDocumentID = @FeedDocumentID";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@BatchID", SqlDbType.VarChar) { Value = p_batchID },
                new SqlParameter("@FeedDocumentID", SqlDbType.VarChar) { Value = p_FeedDocumentID },
                new SqlParameter("@Status", SqlDbType.VarChar) { Value = p_Status },
                new SqlParameter("@CustomerID", SqlDbType.VarChar) { Value = CustomerID },
                new SqlParameter("@Data", SqlDbType.NVarChar) { Value = Data }
            };

            return this.Connection.Execute(updateQuery, p_SQLParams: parameters);
        }

        public bool UpdateStatusSCSInventoryFeed(string CustomerID, string BatchID, string FeedDocumentID, long MessageID)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;

            try
            {
                l_Query = "EXEC Sp_UpdateStatusSCSInventoryFeed";

                PublicFunctions.FieldToParam(CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;

                PublicFunctions.FieldToParam(BatchID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                PublicFunctions.FieldToParam(FeedDocumentID, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                
                PublicFunctions.FieldToParam(MessageID, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;


                return this.Connection.Execute(l_Query);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
