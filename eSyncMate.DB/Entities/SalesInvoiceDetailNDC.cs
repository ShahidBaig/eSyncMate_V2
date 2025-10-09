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
    public class SalesInvoiceDetailNDC : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int ID { get; set; }  
        public int? SalesInvoice_ID { get; set; } 
        public int? InvoiceNo { get; set; }  // Nullable because it's NULLABLE in the table
        public string Description { get; set; }  // Matches varchar(500) in the table
        public string PoNumber { get; set; }  // Matches varchar(500) in the table
        public string Status { get; set; }  // Matches varchar(100) in the table
        public int? EDILineID { get; set; }  // Nullable because it's NULLABLE in the table
        public decimal? UnitPrice { get; set; }  // Nullable decimal with precision (12, 2)
        public string ItemID { get; set; }  // Matches varchar(100) in the table
        public int? QTY { get; set; }  // Nullable because it's NULLABLE in the table
        public string SupplierStyle { get; set; }  // Matches varchar(100) in the table
        public string UPC { get; set; }  // Matches varchar(100) in the table
        public string SKU { get; set; }  // Matches varchar(100) in the table
        public string TrackingNo { get; set; }  // Matches varchar(100) in the table
        public string SSCC { get; set; }  // Matches varchar(100) in the table
        public DateTime CreatedDate { get; set; }  // Not nullable
        public int CreatedBy { get; set; }  // Not nullable
        public DateTime? ModifiedDate { get; set; }  // Nullable to match nullable datetime
        public int? ModifiedBy { get; set; }  // Nullable to match
        public string WarehouseName { get; set; }  // Matches varchar(250) in the table
        public string UOM { get; set; }  // Matches varchar(250) in the table

        // Optional: Static properties for metadata if needed
        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SalesInvoiceDetailNDC() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SalesInvoiceDetailNDC(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SalesInvoiceDetailNDC(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(SalesInvoiceDetailNDC.TableName))
            {
                SalesInvoiceDetailNDC.TableName = "SalesInvoiceDetailNDC";
            }

            if (string.IsNullOrEmpty(SalesInvoiceDetailNDC.ViewName))
            {
                SalesInvoiceDetailNDC.ViewName = "VW_SalesInvoiceDetailNDC";
            }

            if (string.IsNullOrEmpty(SalesInvoiceDetailNDC.PrimaryKeyName))
            {
                SalesInvoiceDetailNDC.PrimaryKeyName = "CustomerID";
            }

            if (string.IsNullOrEmpty(SalesInvoiceDetailNDC.EndingPropertyName))
            {
                SalesInvoiceDetailNDC.EndingPropertyName = "CreatedBy";
            }

            if (SalesInvoiceDetailNDC.DBProperties == null)
            {
                SalesInvoiceDetailNDC.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(SalesInvoiceDetailNDC.InsertQueryStart))
            {
                SalesInvoiceDetailNDC.InsertQueryStart = PrepareQueries(this, SalesInvoiceDetailNDC.TableName, SalesInvoiceDetailNDC.EndingPropertyName, ref l_Query, SalesInvoiceDetailNDC.DBProperties);
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
                l_Query = "SELECT * FROM [" + SalesInvoiceDetailNDC.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + SalesInvoiceDetailNDC.TableName + "]";
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
                l_Query = "SELECT * FROM [" + SalesInvoiceDetailNDC.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + SalesInvoiceDetailNDC.ViewName + "]";
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

        public bool GetSalesInvoiceDetailNDCData(ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = $"SELECT DISTINCT ItemID FROM {SalesInvoiceDetailNDC.TableName} WITH (NOLOCK)";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(SalesInvoiceDetailNDC.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceDetailNDC.ViewName, SalesInvoiceDetailNDC.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + SalesInvoiceDetailNDC.PrimaryKeyName + ", '0'))) FROM " + SalesInvoiceDetailNDC.TableName, ref l_Data))
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
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceDetailNDC.ViewName, SalesInvoiceDetailNDC.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceDetailNDC.ViewName, SalesInvoiceDetailNDC.PrimaryKeyName), true);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(SalesInvoiceDetailNDC.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceDetailNDC.ViewName, SalesInvoiceDetailNDC.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceDetailNDC.ViewName, propertyName));
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

                this.ID = this.GetMax();

                l_Query = this.PrepareInsertQuery(this, SalesInvoiceDetailNDC.InsertQueryStart, SalesInvoiceDetailNDC.EndingPropertyName, SalesInvoiceDetailNDC.DBProperties);

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
                SalesInvoiceDetailNDC.DBProperties = this.GetType().GetProperties()
                  .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
                  .ToList();

                SalesInvoiceDetailNDC.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, SalesInvoiceDetailNDC.TableName, SalesInvoiceDetailNDC.PrimaryKeyName, SalesInvoiceDetailNDC.EndingPropertyName, Connectors.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, "Temp_"+SalesInvoiceDetailNDC.TableName, SalesInvoiceDetailNDC.PrimaryKeyName);

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
            return ((Connectors)x).Id == ((Connectors)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.ID;
        }
        #endregion

        
    }
}
