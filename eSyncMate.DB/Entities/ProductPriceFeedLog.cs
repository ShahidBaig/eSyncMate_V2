using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace eSyncMate.DB.Entities
{
    public class ProductPriceFeedLog : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public int RouteID { get; set; }
        public string CustomerID { get; set; }
        public string ItemID { get; set; }
        public string ProductID { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        public ProductPriceFeedLog() : base()
        {
            SetupDBEntity();
        }

        public ProductPriceFeedLog(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public ProductPriceFeedLog(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(ProductPriceFeedLog.TableName))
            {
                ProductPriceFeedLog.TableName = "ProductPriceFeedLog";
            }

            if (string.IsNullOrEmpty(ProductPriceFeedLog.ViewName))
            {
                ProductPriceFeedLog.ViewName = "VW_ProductPriceFeedLog";
            }

            if (string.IsNullOrEmpty(ProductPriceFeedLog.PrimaryKeyName))
            {
                ProductPriceFeedLog.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(ProductPriceFeedLog.EndingPropertyName))
            {
                ProductPriceFeedLog.EndingPropertyName = "CreatedDate";
            }

            if (ProductPriceFeedLog.DBProperties == null)
            {
                ProductPriceFeedLog.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(ProductPriceFeedLog.InsertQueryStart))
            {
                ProductPriceFeedLog.InsertQueryStart = PrepareQueries(this, ProductPriceFeedLog.TableName, ProductPriceFeedLog.EndingPropertyName, ref l_Query, ProductPriceFeedLog.DBProperties, ProductPriceFeedLog.PrimaryKeyName);
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
        /// Convenience helper: writes one audit row (request or response) for a product.
        /// Type is the log kind, e.g. "REQ-SNT" (price sent) or "RSP-RVD" (response received).
        /// </summary>
        public Result SaveData(int p_RouteID, string p_CustomerID, string p_ItemID, string p_ProductID, string p_Type, string p_Data, int p_UserNo)
        {
            this.RouteID = p_RouteID;
            this.CustomerID = p_CustomerID;
            this.ItemID = p_ItemID;
            this.ProductID = p_ProductID;
            this.Type = p_Type;
            this.Data = p_Data;
            this.CreatedBy = p_UserNo;
            this.CreatedDate = DateTime.Now;

            return this.SaveNew();
        }

        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + ProductPriceFeedLog.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + ProductPriceFeedLog.TableName + "]";
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
                l_Query = "SELECT * FROM [" + ProductPriceFeedLog.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + ProductPriceFeedLog.ViewName + "]";
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

        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(ProductPriceFeedLog.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ProductPriceFeedLog.ViewName, ProductPriceFeedLog.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + ProductPriceFeedLog.PrimaryKeyName + ", '0'))) FROM " + ProductPriceFeedLog.TableName, ref l_Data))
            {
                return l_MaxNo;
            }

            l_MaxNo = PublicFunctions.ConvertNullAsInteger(l_Data.Rows[0][0], 0) + 1;

            l_Data.Dispose();

            return l_MaxNo;
        }

        public Result GetObjectFromQuery(string p_Query, bool isOnlyObject = false)
        {
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
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ProductPriceFeedLog.ViewName, ProductPriceFeedLog.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ProductPriceFeedLog.ViewName, ProductPriceFeedLog.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, ProductPriceFeedLog.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, ProductPriceFeedLog.InsertQueryStart, ProductPriceFeedLog.EndingPropertyName, ProductPriceFeedLog.DBProperties, ProductPriceFeedLog.PrimaryKeyName);

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

                l_Query = this.PrepareUpdateQuery(this, ProductPriceFeedLog.TableName, ProductPriceFeedLog.PrimaryKeyName, ProductPriceFeedLog.EndingPropertyName, ProductPriceFeedLog.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, ProductPriceFeedLog.TableName, ProductPriceFeedLog.PrimaryKeyName);

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
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region IEqualityComparer Support
        public new bool Equals(object x, object y)
        {
            return ((ProductPriceFeedLog)x).Id == ((ProductPriceFeedLog)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion
    }
}
