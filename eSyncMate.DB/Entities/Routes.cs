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
using static eSyncMate.DB.Declarations;

namespace eSyncMate.DB.Entities
{
    public class Routes : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public string Status { get; set; }
        public int SourcePartyId { get; set; }
        public int DestinationPartyId { get; set; }
        public int SourceConnectorId { get; set; }
        public int DestinationConnectorId { get; set; }
        public int MapId { get; set; }
        public int PartyGroupId { get; set; }
        public string FrequencyType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RepeatCount { get; set; }
        public string WeekDays { get; set; }
        public string OnDay { get; set; }
        public string ExecutionTime { get; set; }
        public string JobID { get; set; }
        public string RouteGroup { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public string SourceParty { get; set; }
        public string DestinationParty { get; set; }
        public string SourceConnector { get; set; }
        public string DestinationConnector { get; set; }
        public string Map { get; set; }
        public string PartyGroup { get; set; }
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
        public Routes() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Routes(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Routes(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(Routes.TableName))
            {
                Routes.TableName = "Routes";
            }

            if (string.IsNullOrEmpty(Routes.ViewName))
            {
                Routes.ViewName = "VW_Routes";
            }

            if (string.IsNullOrEmpty(Routes.PrimaryKeyName))
            {
                Routes.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(Routes.EndingPropertyName))
            {
                Routes.EndingPropertyName = "CreatedBy";
            }

            if (Routes.DBProperties == null)
            {
                Routes.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(Routes.InsertQueryStart))
            {
                Routes.InsertQueryStart = PrepareQueries(this, Routes.TableName, Routes.EndingPropertyName, ref l_Query, Routes.DBProperties);
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
                l_Query = "SELECT * FROM [" + Routes.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Routes.TableName + "]";
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
                l_Query = "SELECT * FROM [" + Routes.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Routes.ViewName + "]";
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
            SetProperty(Routes.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Routes.ViewName, Routes.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + Routes.PrimaryKeyName + ", '0'))) FROM " + Routes.TableName, ref l_Data))
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

            this.SourcePartyObject = new Customers();
            this.SourcePartyObject.UseConnection(string.Empty, Connection);
            this.SourcePartyObject.Id = this.SourcePartyId;
            this.SourcePartyObject.GetObjectOnly();

            this.DestinationPartyObject = new Customers();
            this.DestinationPartyObject.UseConnection(string.Empty, Connection);
            this.DestinationPartyObject.Id = this.DestinationPartyId;
            this.DestinationPartyObject.GetObjectOnly();

            this.SourceConnectorObject = new Connectors();
            this.SourceConnectorObject.UseConnection(string.Empty, Connection);
            this.SourceConnectorObject.Id = this.SourceConnectorId;
            this.SourceConnectorObject.GetObjectOnly();

            this.DestinationConnectorObject = new Connectors();
            this.DestinationConnectorObject.UseConnection(string.Empty, Connection);
            this.DestinationConnectorObject.Id = this.DestinationConnectorId;
            this.DestinationConnectorObject.GetObjectOnly();

            if (this.MapId > 0)
            {
                this.MapObject = new Maps();
                this.MapObject.UseConnection(string.Empty, Connection);
                this.MapObject.Id = this.MapId;
                this.MapObject.GetObjectOnly();
            }

            return Result.GetSuccessResult();
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Routes.ViewName, Routes.PrimaryKeyName));
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(Routes.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Routes.ViewName, Routes.PrimaryKeyName), true);
        }
        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Routes.ViewName, Routes.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, Routes.ViewName, propertyName));
        }

        public Result GetObject(string propertyName1, object propertyValue1, string propertyName2, object propertyValue2)
        {
            // Set values to properties
            var prop1 = this.GetType().GetProperty(propertyName1);
            var prop2 = this.GetType().GetProperty(propertyName2);

            prop1?.SetValue(this, propertyValue1);
            prop2?.SetValue(this, propertyValue2);

            string criteria = $" WHERE [{propertyName1}] = '{propertyValue1.ToString().Replace("'", "''")}' AND [{propertyName2}] = '{propertyValue2.ToString().Replace("'", "''")}'";
            string query = $"SELECT * FROM [{Routes.ViewName}]{criteria}";

            return GetObjectFromQuery(query);
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

                if (Routes.InsertQueryStart.Contains(",[ModifiedDate],[ModifiedBy]"))
                {
                    Routes.InsertQueryStart = Routes.InsertQueryStart.Replace(",[ModifiedDate],[ModifiedBy]", string.Empty);
                }

                l_Query = this.PrepareInsertQuery(this, Routes.InsertQueryStart, Routes.EndingPropertyName, Routes.DBProperties, "ModifiedDate,ModifiedBy");

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
                Routes.DBProperties = this.GetType().GetProperties()
                .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
                .ToList();

                Routes.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, Routes.TableName, Routes.PrimaryKeyName, Routes.EndingPropertyName, Routes.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, Routes.TableName, Routes.PrimaryKeyName);

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

        public Result SaveData(string type, int OrderId, string Data, int userNo)
        {
            Result l_Result = Result.GetSuccessResult();

            try
            {
                RouteData routeData = new RouteData();

                routeData.UseConnection(string.Empty, this.Connection);

                routeData.CreatedBy = userNo;
                routeData.CreatedDate = DateTime.Now;
                routeData.Data = Data;
                routeData.OrderId = OrderId;
                routeData.Type = type;
                routeData.RouteId = this.Id;

                l_Result = routeData.SaveNew();
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

        public Result SaveLog(LogTypeEnum type, string Message, string Details, int userNo)
        {
            Result l_Result = Result.GetSuccessResult();

            try
            {
                // Only insert logs for Error, Exception, and Info types
                if (type == LogTypeEnum.Error || type == LogTypeEnum.Exception || type == LogTypeEnum.Info)
                {
                    RouteLog routeLog = new RouteLog();

                    routeLog.UseConnection(string.Empty, this.Connection);

                    routeLog.CreatedBy = userNo;
                    routeLog.CreatedDate = DateTime.Now;
                    routeLog.Message = Message;
                    routeLog.Details = Details;
                    routeLog.Type = Convert.ToInt32(type);
                    routeLog.RouteId = this.Id;

                    l_Result = routeLog.SaveNew();
                }
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
            return ((Routes)x).Id == ((Routes)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }

       
        #endregion
    }
}
