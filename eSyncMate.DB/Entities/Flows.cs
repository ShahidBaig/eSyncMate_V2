using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    public class Flows : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public long Id { get; set; }
        public string CustomerID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
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

        public Flows() : base()
        {
            SetupDBEntity();
        }

        public Flows(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public Flows(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(Flows.TableName))
            {
                Flows.TableName = "Flows";
            }

            if (string.IsNullOrEmpty(Flows.ViewName))
            {
                Flows.ViewName = "Flows";
            }

            if (string.IsNullOrEmpty(Flows.PrimaryKeyName))
            {
                Flows.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(Flows.EndingPropertyName))
            {
                Flows.EndingPropertyName = "CreatedBy";
            }

            if (Flows.DBProperties == null)
            {
                Flows.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(Flows.InsertQueryStart))
            {
                Flows.InsertQueryStart = PrepareQueries(this, Flows.TableName, Flows.EndingPropertyName, ref l_Query, Flows.DBProperties, "Id");
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
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + Flows.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Flows.TableName + "]";
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
                l_Query = "SELECT * FROM [" + Flows.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Flows.ViewName + "]";
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

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + Flows.PrimaryKeyName + ", '0'))) FROM " + Flows.TableName, ref l_Data))
            {
                return l_MaxNo;
            }

            l_MaxNo = PublicFunctions.ConvertNullAsInteger(l_Data.Rows[0][0], 0) + 1;

            l_Data.Dispose();

            return l_MaxNo;
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, Flows.ViewName, propertyName));
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Flows.ViewName, Flows.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Flows.TableName, Flows.PrimaryKeyName));
        }

        public Result GetObjectFromQuery(string p_Query, bool isOnlyObject = false)
        {
            DataTable l_Data = new DataTable();

            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, Flows.EndingPropertyName);

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

                l_Query = this.PrepareInsertQuery(this, Flows.InsertQueryStart, Flows.EndingPropertyName, Flows.DBProperties, "Id");

                l_Process = this.Connection.Execute(l_Query);

                if (l_Process)
                {
                    l_Result = Result.GetSuccessResult();
                    if (l_Trans)
                    {
                        this.Connection.CommitTransaction();
                    }
                }
                else
                {
                    if (l_Trans)
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
                Flows.EndingPropertyName = "ModifiedBy";
                l_Trans = this.Connection.BeginTransaction();
                l_Query = this.PrepareUpdateQuery(this, Flows.TableName, Flows.PrimaryKeyName, Flows.EndingPropertyName, Flows.DBProperties);
                l_Process = this.Connection.Execute(l_Query);

                if (l_Process)
                {
                    l_Result = Result.GetSuccessResult();
                    if (l_Trans)
                    {
                        this.Connection.CommitTransaction();
                    }
                }
                else
                {
                    if (l_Trans)
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
                l_Query = this.PrepareDeleteQuery(this, Flows.TableName, Flows.PrimaryKeyName);
                l_Process = this.Connection.Execute(l_Query);

                if (l_Process)
                {
                    l_Result = Result.GetSuccessResult();
                    if (l_Trans)
                    {
                        this.Connection.CommitTransaction();
                    }
                }
                else
                {
                    if (l_Trans)
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
                disposedValue = true;
            }
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
            return ((Flows)x).Id == ((Flows)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return (int)this.Id;
        }
        #endregion
    }
}
