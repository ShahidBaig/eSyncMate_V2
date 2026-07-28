using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// Customer-wise shipping-method mapping that drives SP_OrdersData's ASN branch
    /// (raw OrderDetail.ShippingMethod -> LevelOfService / ShipMethodCode). Mirrors the
    /// RouteTypes entity: non-identity Id via GetMax(), SaveNew/Modify/Delete over DBEntity.
    /// </summary>
    public class ShipCodeMappings : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string SourceMethod { get; set; }
        public string MatchType { get; set; }
        public string LevelOfService { get; set; }
        public string ShippingMethod { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }

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

        public ShipCodeMappings() : base()
        {
            SetupDBEntity();
        }

        public ShipCodeMappings(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public ShipCodeMappings(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(ShipCodeMappings.TableName))
            {
                ShipCodeMappings.TableName = "ShipCodeMappings";
            }

            if (string.IsNullOrEmpty(ShipCodeMappings.ViewName))
            {
                ShipCodeMappings.ViewName = "VW_ShipCodeMappings";
            }

            if (string.IsNullOrEmpty(ShipCodeMappings.PrimaryKeyName))
            {
                ShipCodeMappings.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(ShipCodeMappings.EndingPropertyName))
            {
                ShipCodeMappings.EndingPropertyName = "CreatedBy";
            }

            if (ShipCodeMappings.DBProperties == null)
            {
                ShipCodeMappings.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(ShipCodeMappings.InsertQueryStart))
            {
                ShipCodeMappings.InsertQueryStart = PrepareQueries(this, ShipCodeMappings.TableName, ShipCodeMappings.EndingPropertyName, ref l_Query, ShipCodeMappings.DBProperties);
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

        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [" + ShipCodeMappings.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + ShipCodeMappings.TableName + "]";

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            if (!string.IsNullOrEmpty(p_OrderBy))
                l_Query += " ORDER BY " + p_OrderBy;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetViewListPaged(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;

            string l_CountQuery = "SELECT COUNT(*) FROM [" + ShipCodeMappings.ViewName + "]";
            if (!string.IsNullOrEmpty(p_Criteria))
                l_CountQuery += " WHERE " + p_Criteria;

            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0)
                totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [" + ShipCodeMappings.ViewName + "]"
                : "SELECT " + p_Fields + " FROM [" + ShipCodeMappings.ViewName + "]";

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            l_Query += !string.IsNullOrEmpty(p_OrderBy)
                ? " ORDER BY " + p_OrderBy
                : " ORDER BY Id DESC";

            int offset = (pageNumber - 1) * pageSize;
            l_Query += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetViewList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [" + ShipCodeMappings.ViewName + "]"
                : "SELECT " + p_Fields + " FROM [" + ShipCodeMappings.ViewName + "]";

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            if (!string.IsNullOrEmpty(p_OrderBy))
                l_Query += " ORDER BY " + p_OrderBy;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(ShipCodeMappings.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipCodeMappings.ViewName, ShipCodeMappings.PrimaryKeyName));
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipCodeMappings.ViewName, ShipCodeMappings.PrimaryKeyName));
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);
            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipCodeMappings.ViewName, propertyName));
        }

        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(ShipCodeMappings.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipCodeMappings.ViewName, ShipCodeMappings.PrimaryKeyName), true);
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ShipCodeMappings.ViewName, ShipCodeMappings.PrimaryKeyName), true);
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + ShipCodeMappings.PrimaryKeyName + ", '0'))) FROM " + ShipCodeMappings.TableName, ref l_Data))
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

                l_Query = this.PrepareInsertQuery(this, ShipCodeMappings.InsertQueryStart, ShipCodeMappings.EndingPropertyName, ShipCodeMappings.DBProperties);

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

                l_Query = this.PrepareUpdateQuery(this, ShipCodeMappings.TableName, ShipCodeMappings.PrimaryKeyName, ShipCodeMappings.EndingPropertyName, ShipCodeMappings.DBProperties);

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

                // Hard delete — this is a config table with no Status column, so the default
                // soft-delete (UPDATE ... SET Status = 'DELETED') would fail. Custom = DELETE FROM.
                l_Query = this.PrepareDeleteQueryCustom(this, ShipCodeMappings.TableName, ShipCodeMappings.PrimaryKeyName);

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
            return ((ShipCodeMappings)x).Id == ((ShipCodeMappings)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion
    }
}
