using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    public class FlowDetails : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public long Id { get; set; }
        public long FlowId { get; set; }
        public int? RouteId { get; set; }
        public string Status { get; set; }
        public string In_Out { get; set; }
        public string FrequencyType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RepeatCount { get; set; }
        public string WeekDays { get; set; }
        public string OnDay { get; set; }
        public string ExecutionTime { get; set; }
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

        public FlowDetails() : base()
        {
            SetupDBEntity();
        }

        public FlowDetails(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public FlowDetails(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(FlowDetails.TableName))
            {
                FlowDetails.TableName = "FlowDetails";
            }

            if (string.IsNullOrEmpty(FlowDetails.ViewName))
            {
                FlowDetails.ViewName = "FlowDetails";
            }

            if (string.IsNullOrEmpty(FlowDetails.PrimaryKeyName))
            {
                FlowDetails.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(FlowDetails.EndingPropertyName))
            {
                FlowDetails.EndingPropertyName = "CreatedBy";
            }

            if (FlowDetails.DBProperties == null)
            {
                FlowDetails.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(FlowDetails.InsertQueryStart))
            {
                FlowDetails.InsertQueryStart = PrepareQueries(this, FlowDetails.TableName, FlowDetails.EndingPropertyName, ref l_Query, FlowDetails.DBProperties, "Id");
            }
        }

        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + FlowDetails.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + FlowDetails.TableName + "]";
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
                l_Query = "SELECT * FROM [" + FlowDetails.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + FlowDetails.ViewName + "]";
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
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + FlowDetails.PrimaryKeyName + ", '0'))) FROM " + FlowDetails.TableName, ref l_Data))
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

            return GetObjectFromQuery(PrepareGetObjectQuery(this, FlowDetails.ViewName, propertyName));
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, FlowDetails.ViewName, FlowDetails.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, FlowDetails.TableName, FlowDetails.PrimaryKeyName));
        }

        public Result GetObjectFromQuery(string p_Query, bool isOnlyObject = false)
        {
            DataTable l_Data = new DataTable();

            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, FlowDetails.EndingPropertyName);

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

                l_Query = this.PrepareInsertQuery(this, FlowDetails.InsertQueryStart, FlowDetails.EndingPropertyName, FlowDetails.DBProperties, "Id");

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
                FlowDetails.EndingPropertyName = "ModifiedBy";
                l_Trans = this.Connection.BeginTransaction();
                l_Query = this.PrepareUpdateQuery(this, FlowDetails.TableName, FlowDetails.PrimaryKeyName, FlowDetails.EndingPropertyName, FlowDetails.DBProperties);
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
                l_Query = this.PrepareDeleteQuery(this, FlowDetails.TableName, FlowDetails.PrimaryKeyName);
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
            return ((FlowDetails)x).Id == ((FlowDetails)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return (int)this.Id;
        }
        #endregion
    }
}
