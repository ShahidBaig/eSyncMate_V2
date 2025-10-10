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
    public class PurchaseOrderDetail : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ItemID { get; set; }
        public string UPC { get; set; }
        public string Description { get; set; }
        public int LineNo { get; set; }
        public string Status { get; set; }
        public decimal UnitPrice { get; set; }
        public int OrderQty { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public string ManufacturerName { get; set; }
        public string NDCItemID { get; set; } = string.Empty;
        public string PrimaryCategoryName { get; set; }
        public string SecondaryCategoryName { get; set; }
        public string ProductName { get; set; }
        public decimal ExtendedPrice { get; set; }
        public string UOM { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public PurchaseOrderDetail() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public PurchaseOrderDetail(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public PurchaseOrderDetail(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(PurchaseOrderDetail.TableName))
            {
                PurchaseOrderDetail.TableName = "PurchaseOrderDetail";
            }

            if (string.IsNullOrEmpty(PurchaseOrderDetail.ViewName))
            {
                PurchaseOrderDetail.ViewName = "VW_PurchaseOrderDetail";
            }

            if (string.IsNullOrEmpty(PurchaseOrderDetail.PrimaryKeyName))
            {
                PurchaseOrderDetail.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(PurchaseOrderDetail.EndingPropertyName))
            {
                PurchaseOrderDetail.EndingPropertyName = "CreatedBy";
            }

            if (PurchaseOrderDetail.DBProperties == null)
            {
                PurchaseOrderDetail.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(PurchaseOrderDetail.InsertQueryStart))
            {
                PurchaseOrderDetail.InsertQueryStart = PrepareQueries(this, PurchaseOrderDetail.TableName, PurchaseOrderDetail.EndingPropertyName, ref l_Query, PurchaseOrderDetail.DBProperties);
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
                l_Query = "SELECT * FROM [" + PurchaseOrderDetail.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + PurchaseOrderDetail.TableName + "]";
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
                l_Query = "SELECT * FROM [" + PurchaseOrderDetail.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + PurchaseOrderDetail.ViewName + "]";
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
            SetProperty(PurchaseOrderDetail.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrderDetail.ViewName, PurchaseOrderDetail.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + PurchaseOrderDetail.PrimaryKeyName + ", '0'))) FROM " + PurchaseOrderDetail.TableName, ref l_Data))
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
            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrderDetail.ViewName, PurchaseOrderDetail.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrderDetail.ViewName, PurchaseOrderDetail.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrderDetail.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, PurchaseOrderDetail.InsertQueryStart, PurchaseOrderDetail.EndingPropertyName, PurchaseOrderDetail.DBProperties);

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

                l_Query = this.PrepareUpdateQuery(this, PurchaseOrderDetail.TableName, PurchaseOrderDetail.PrimaryKeyName, PurchaseOrderDetail.EndingPropertyName, PurchaseOrderDetail.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, PurchaseOrderDetail.TableName, PurchaseOrderDetail.PrimaryKeyName);

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

        public Result DeleteItems(int OrderId, int LineNo)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Process = this.Connection.Execute($"DELETE FROM PurchaseOrderDetail WHERE OrderId = {OrderId} AND [LineNo] = {LineNo}");

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

        public Result ResetASNQty(int OrderId, int LineNo)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Process = this.Connection.Execute($"UPDATE PurchaseOrderDetail SET ASNQty = 0 WHERE OrderId = {OrderId} AND [LineNo] = {LineNo}");

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

        public Result UpdateASNInfo(int OrderId, int LineNo, int Qty, string trackingNo, string shippingMethod, string shippedDate)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                for (int i = 0; i < Qty; i++)
                {
                    l_Process = this.Connection.Execute($@"UPDATE PurchaseOrderDetail SET ASNQty = 1, Status = 'ASNRVD', TrackingNo = '{trackingNo}',
                                                                ShippingMethod = '{shippingMethod}', ShippedDate = '{shippedDate}'
                                                           WHERE Id = (SELECT TOP 1 Id 
                                                                        FROM PurchaseOrderDetail 
                                                                        WHERE OrderId = {OrderId} AND [LineNo] = {LineNo} AND Status = 'NEW')");
                    l_Process = true;
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

        public Result UpdateASNSent(int OrderId, string trackingNo)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Process = this.Connection.Execute($@"UPDATE PurchaseOrderDetail SET Status = 'ASNSNT'
                                           WHERE OrderId = {OrderId} AND TrackingNo = '{trackingNo}' AND Status = 'ASNRVD'");

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

        private int GetAvailableQty(int OrderId, int LineNo, int Qty)
        {
            int availableQty = 0;
            DataTable dt = new DataTable();

            if (this.GetList($"OrderId = {OrderId} AND [LineNo] = {LineNo} AND Status = 'NEW'", $"SUM(LineQty) As AvailableQty", ref dt))
            {
                availableQty = Convert.ToInt32(dt.Rows[0]["AvailableQty"].ToString());
            }

            dt.Dispose();

            return availableQty;
        }

        public Result UpdateCancelQty(int OrderId, int LineNo = 0, int Qty = 0)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                if (LineNo == 0 && Qty == 0)
                {
                    l_Process = this.Connection.Execute($"UPDATE PurchaseOrderDetail SET CancelQty = 1, Status = 'CANRVD' WHERE OrderId = {OrderId} AND Status IN ('NEW')");
                }
                else if (LineNo > 0)
                {
                    l_Process = this.Connection.Execute($@"UPDATE PurchaseOrderDetail SET CancelQty = 1, Status = 'CANRVD'
                                                               WHERE OrderId = {OrderId} AND [LineNo] = {LineNo} AND Status = 'NEW'");
                    l_Process = true;
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

        public Result UpdatePurchaseOrderDetailLineNo(int OrderId, int LineNo = 0, int UpdatedLineNo = 0)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Process = this.Connection.Execute($"UPDATE PurchaseOrderDetail SET LineNo = {UpdatedLineNo} WHERE OrderId = {OrderId} AND [LineNo] = {LineNo}");

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

        public Result UpdatePurchaseOrderDetailStatus(int OrderId, int LineNo = 0)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Process = this.Connection.Execute($"UPDATE PurchaseOrderDetail SET Status = 'CANSNT' WHERE OrderId = {OrderId} AND [LineNo] = {LineNo} AND Status = 'CANRVD'");

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

        public bool UpdateTrackStatus(int tenderID, string trackStatus)
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
                                   "SET [LastTrackStatus] = @TrackStatus, [TrackStatus_Updated] = @TrackDate " +
                                   "WHERE [Id] = @TenderID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TrackStatus", trackStatus);
                        command.Parameters.AddWithValue("@TenderID", tenderID);
                        command.Parameters.AddWithValue("@TrackDate", DateTime.Now);

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

        public Result OrderQuantityUpdate(int Qty, string ItemID, string action)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                string sqlQuery = action.ToUpper() switch
                {
                    "ADD" => $"UPDATE InvFeedFromNDC SET Qty = Qty - {Qty} WHERE ItemID = '{ItemID}'",
                    "DELETE" => $"UPDATE InvFeedFromNDC SET Qty = Qty + {Qty} WHERE ItemID = '{ItemID}'",
                    "CANCEL" => $"UPDATE InvFeedFromNDC SET Qty = Qty + {Qty} WHERE ItemID = '{ItemID}'",
                    _ => throw new ArgumentException("Invalid action specified.")
                };

                l_Process = this.Connection.Execute(sqlQuery);

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

        #region 
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
            return ((PurchaseOrderDetail)x).Id == ((PurchaseOrderDetail)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion
    }
}
