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
    public class SalesInvoiceNDC : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int ID { get; set; }
        public int? InvoiceNo { get; set; }  // Nullable because it's NULLABLE in the table
        public DateTime? InvoiceDate { get; set; }  // Nullable because it's NULLABLE in the table
        public string PoNumber { get; set; }  // Matches varchar(500) in the table
        public string Status { get; set; }  // Matches varchar(100) in the table
        public string SCACCode { get; set; }  // Matches varchar(50) in the table
        public string Routing { get; set; }  // Matches varchar(100) in the table
        public DateTime? ShippingDate { get; set; }  // Nullable to match nullable datetime
        public string ShippingName { get; set; }  // Matches varchar(500) in the table
        public string ShippingToNo { get; set; }  // Matches varchar(500) in the table
        public string ShippingAddress1 { get; set; }  // Matches varchar(500) in the table
        public string ShippingAddress2 { get; set; }  // Matches varchar(500) in the table
        public string ShippingCity { get; set; }  // Matches varchar(100) in the table
        public string ShippingState { get; set; }  // Matches varchar(100) in the table
        public string ShippingZip { get; set; }  // Matches varchar(50) in the table
        public string ShippingCountry { get; set; }  // Matches varchar(100) in the table
        public string SellerID { get; set; }  // Matches varchar(100) in the table
        public string InvoiceTerms { get; set; }  // Matches varchar(100) in the table
        public decimal? Freight { get; set; }  // Nullable decimal to match
        public decimal? HandlingAmount { get; set; }  // Nullable decimal to match
        public decimal? SalesTax { get; set; }  // Nullable decimal to match
        public decimal? InvoiceAmount { get; set; }  // Nullable decimal to match
        public string TrackingNo { get; set; }  // Matches varchar(100) in the table
        public DateTime CreatedDate { get; set; }  // Not nullable
        public int CreatedBy { get; set; }  // Not nullable
        public DateTime? ModifiedDate { get; set; }  // Nullable to match
        public int? ModifiedBy { get; set; }  // Nullable to match

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SalesInvoiceNDC() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SalesInvoiceNDC(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public SalesInvoiceNDC(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(SalesInvoiceNDC.TableName))
            {
                SalesInvoiceNDC.TableName = "SalesInvoiceNDC";
            }

            if (string.IsNullOrEmpty(SalesInvoiceNDC.ViewName))
            {
                SalesInvoiceNDC.ViewName = "VW_SalesInvoiceNDC";
            }

            if (string.IsNullOrEmpty(SalesInvoiceNDC.PrimaryKeyName))
            {
                SalesInvoiceNDC.PrimaryKeyName = "CustomerID";
            }

            if (string.IsNullOrEmpty(SalesInvoiceNDC.EndingPropertyName))
            {
                SalesInvoiceNDC.EndingPropertyName = "CreatedBy";
            }

            if (SalesInvoiceNDC.DBProperties == null)
            {
                SalesInvoiceNDC.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(SalesInvoiceNDC.InsertQueryStart))
            {
                SalesInvoiceNDC.InsertQueryStart = PrepareQueries(this, SalesInvoiceNDC.TableName, SalesInvoiceNDC.EndingPropertyName, ref l_Query, SalesInvoiceNDC.DBProperties);
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
                l_Query = "SELECT * FROM [" + SalesInvoiceNDC.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + SalesInvoiceNDC.TableName + "]";
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
            string viewName = SalesInvoiceNDC.ViewName;

            // Modify criteria based on default values
            if (p_Criteria.Contains("InvoiceNo = 0"))
            {
                p_Criteria = p_Criteria.Replace("InvoiceNo = 0", "").Trim();
            }
            if (p_Criteria.Contains("InvoiceDate = '1999-01-01'"))
            {
                p_Criteria = p_Criteria.Replace("InvoiceDate = '1999-01-01'", "").Trim();
            }
            if (p_Criteria.Contains("PoNumber = 'EMPTY'"))
            {
                p_Criteria = p_Criteria.Replace("PoNumber = 'EMPTY'", "").Trim();
            }
            if (p_Criteria.Contains("Status = 'EMPTY'"))
            {
                p_Criteria = p_Criteria.Replace("Status = 'EMPTY'", "").Trim();
            }

            // Clean up any extra 'AND' or 'OR' in the criteria
            p_Criteria = p_Criteria.Replace("AND AND", "AND").Replace("WHERE AND", "WHERE").Replace("WHERE OR", "WHERE");
            p_Criteria = p_Criteria.Trim().TrimEnd(new char[] { 'A', 'N', 'D', 'O', 'R' });

            // Build the query
            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + viewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + viewName + "]";
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
            SetProperty(SalesInvoiceNDC.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceNDC.ViewName, SalesInvoiceNDC.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + SalesInvoiceNDC.PrimaryKeyName + ", '0'))) FROM " + SalesInvoiceNDC.TableName, ref l_Data))
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
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceNDC.ViewName, SalesInvoiceNDC.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceNDC.ViewName, SalesInvoiceNDC.PrimaryKeyName), true);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(SalesInvoiceNDC.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceNDC.ViewName, SalesInvoiceNDC.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, SalesInvoiceNDC.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, SalesInvoiceNDC.InsertQueryStart, SalesInvoiceNDC.EndingPropertyName, SalesInvoiceNDC.DBProperties);

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
                SalesInvoiceNDC.DBProperties = this.GetType().GetProperties()
                  .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
                  .ToList();

                SalesInvoiceNDC.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, SalesInvoiceNDC.TableName, SalesInvoiceNDC.PrimaryKeyName, SalesInvoiceNDC.EndingPropertyName, Connectors.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, "Temp_"+SalesInvoiceNDC.TableName, SalesInvoiceNDC.PrimaryKeyName);

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


        public bool GetBatchWiseData(string p_Criteria, ref DataTable p_Data)
        {
            string l_SQL = string.Empty;

            l_SQL = $"SELECT * FROM VW_ShipmentDetailFromNDC WHERE {p_Criteria}";

            return Connection.GetData(l_SQL, ref p_Data);
        }

    }
}
