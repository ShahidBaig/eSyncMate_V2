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
    public class ShipmentDetailFromNDC : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int ID { get; set; }
        public int ShipmentFromNDC_ID { get; set; }
        public string ShipmentID { get; set; }
        public string PoNumber { get; set; }
        public string Status { get; set; }
        public int EDILineID { get; set; }
        public string ItemID { get; set; }
        public int QTY { get; set; }
        public string SupplierStyle { get; set; }
        public string UPC { get; set; }
        public string SKU { get; set; }
        public string TrackingNo { get; set; }
        public string SSCC { get; set; }
        public string BOLNO { get; set; }
        public string LotNumber { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string ShipStationStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public ShipmentDetailFromNDC() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public ShipmentDetailFromNDC(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public ShipmentDetailFromNDC(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(ShipmentDetailFromNDC.TableName))
            {
                ShipmentDetailFromNDC.TableName = "ShipmentDetailFromNDC";
            }

            if (string.IsNullOrEmpty(ShipmentDetailFromNDC.ViewName))
            {
                ShipmentDetailFromNDC.ViewName = "VW_ShipmentDetailFromNDC";
            }

            if (string.IsNullOrEmpty(ShipmentDetailFromNDC.PrimaryKeyName))
            {
                ShipmentDetailFromNDC.PrimaryKeyName = "CustomerID";
            }

            if (string.IsNullOrEmpty(ShipmentDetailFromNDC.EndingPropertyName))
            {
                ShipmentDetailFromNDC.EndingPropertyName = "CreatedBy";
            }

            if (ShipmentDetailFromNDC.DBProperties == null)
            {
                ShipmentDetailFromNDC.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(ShipmentDetailFromNDC.InsertQueryStart))
            {
                ShipmentDetailFromNDC.InsertQueryStart = PrepareQueries(this, ShipmentDetailFromNDC.TableName, ShipmentDetailFromNDC.EndingPropertyName, ref l_Query, ShipmentDetailFromNDC.DBProperties);
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
                l_Query = "SELECT * FROM [" + ShipmentDetailFromNDC.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + ShipmentDetailFromNDC.TableName + "]";
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
                l_Query = "SELECT * FROM [" + ShipmentDetailFromNDC.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + ShipmentDetailFromNDC.ViewName + "]";
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

        public bool GetShipmentDetailFromNDCData(ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = $"SELECT DISTINCT ItemID FROM {ShipmentDetailFromNDC.TableName} WITH (NOLOCK)";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetShipmentDetailFromNDCShipStationStatus(ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = $"SELECT DISTINCT ItemID, QTY, WarehouseName,LotNumber,ExpirationDate,ShipStationStatus FROM {ShipmentDetailFromNDC.TableName} WITH (NOLOCK) WHERE ShipStationStatus = 'NEW'";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(ShipmentDetailFromNDC.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipmentDetailFromNDC.ViewName, ShipmentDetailFromNDC.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + ShipmentDetailFromNDC.PrimaryKeyName + ", '0'))) FROM " + ShipmentDetailFromNDC.TableName, ref l_Data))
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
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipmentDetailFromNDC.ViewName, ShipmentDetailFromNDC.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipmentDetailFromNDC.ViewName, ShipmentDetailFromNDC.PrimaryKeyName), true);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(ShipmentDetailFromNDC.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipmentDetailFromNDC.ViewName, ShipmentDetailFromNDC.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipmentDetailFromNDC.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, ShipmentDetailFromNDC.InsertQueryStart, ShipmentDetailFromNDC.EndingPropertyName, ShipmentDetailFromNDC.DBProperties);

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
                ShipmentDetailFromNDC.DBProperties = this.GetType().GetProperties()
                  .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
                  .ToList();

                ShipmentDetailFromNDC.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, ShipmentDetailFromNDC.TableName, ShipmentDetailFromNDC.PrimaryKeyName, ShipmentDetailFromNDC.EndingPropertyName, Connectors.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, "Temp_"+ShipmentDetailFromNDC.TableName, ShipmentDetailFromNDC.PrimaryKeyName);

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
