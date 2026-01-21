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
    public class CustomerAlerts : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int AlertId { get; set; }
        public string FrequencyType { get; set; }
        public int RepeatCount { get; set; }
        public string ExecutionTime { get; set; }
        public string WeekDays { get; set; }
        public string DayOfMonth { get; set; }
        public string Emails { get; set; }
        public string JobID { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public List<CustomerMaps> Maps { get; set; }
        public List<CustomerConnectors> Connectors { get; set; }
        public AlertsConfiguration AlertsConfiguration { get; set; }
        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; } = "CreatedBy";
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CustomerAlerts() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CustomerAlerts(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public CustomerAlerts(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(CustomerAlerts.TableName))
            {
                CustomerAlerts.TableName = "CustomerAlerts";
            }

            if (string.IsNullOrEmpty(CustomerAlerts.ViewName))
            {
                CustomerAlerts.ViewName = "VW_CustomerAlerts";
            }

            if (string.IsNullOrEmpty(CustomerAlerts.PrimaryKeyName))
            {
                CustomerAlerts.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(CustomerAlerts.EndingPropertyName))
            {
                CustomerAlerts.EndingPropertyName = "CreatedBy";
            }

            if (CustomerAlerts.DBProperties == null)
            {
                CustomerAlerts.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(CustomerAlerts.InsertQueryStart))
            {
                CustomerAlerts.InsertQueryStart = PrepareQueries(this, CustomerAlerts.TableName, CustomerAlerts.EndingPropertyName, ref l_Query, CustomerAlerts.DBProperties);
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
                l_Query = "SELECT * FROM [" + CustomerAlerts.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + CustomerAlerts.ViewName + "]";
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
                l_Query = "SELECT * FROM [" + CustomerAlerts.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + CustomerAlerts.ViewName + "]";
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

        public bool GetCustomerAlertsData(ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = "SELECT ID,NAME FROM CustomerAlerts WITH (NOLOCK)";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(CustomerAlerts.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerAlerts.ViewName, CustomerAlerts.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + CustomerAlerts.PrimaryKeyName + ", '0'))) FROM " + CustomerAlerts.TableName, ref l_Data))
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
            CustomerMaps l_Maps = new CustomerMaps();
            CustomerConnectors l_Connectors = new CustomerConnectors();

            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, CustomerAlerts.EndingPropertyName);

            l_Data.Dispose();

            if (isOnlyObject)
            {
                return Result.GetSuccessResult();
            }

            this.AlertsConfiguration = new AlertsConfiguration();
            this.AlertsConfiguration.UseConnection(string.Empty, Connection);
            this.AlertsConfiguration.AlertID = this.AlertId;
            this.AlertsConfiguration.GetObjectOnly();

            l_Data = new DataTable();

            this.Maps = new List<CustomerMaps>();

            l_Maps.UseConnection(string.Empty, this.Connection);
            l_Maps.GetViewList("CustomerId = " + PublicFunctions.FieldToParam(this.Id, Declarations.FieldTypes.Number), "*", ref l_Data);

            foreach (DataRow l_Row in l_Data.Rows)
            {
                CustomerMaps l_Map = new CustomerMaps();

                l_Map.PopulateObjectFromRow(l_Map, l_Data, CustomerMaps.DBProperties, string.Empty, l_Row);

                this.Maps.Add(l_Map);
            }

            l_Data.Dispose();
            l_Data = new DataTable();

            this.Connectors = new List<CustomerConnectors>();

            l_Connectors.UseConnection(string.Empty, this.Connection);
            l_Connectors.GetViewList("CustomerId = " + PublicFunctions.FieldToParam(this.Id, Declarations.FieldTypes.Number), "*", ref l_Data);

            foreach (DataRow l_Row in l_Data.Rows)
            {
                CustomerConnectors l_Connector = new CustomerConnectors();

                l_Connector.PopulateObjectFromRow(l_Connector, l_Data, CustomerConnectors.DBProperties, string.Empty, l_Row);

                this.Connectors.Add(l_Connector);
            }

            l_Data.Dispose();
            return Result.GetSuccessResult();
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerAlerts.ViewName, CustomerAlerts.PrimaryKeyName));
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(CustomerAlerts.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerAlerts.ViewName, CustomerAlerts.PrimaryKeyName), true);
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerAlerts.ViewName, CustomerAlerts.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, CustomerAlerts.ViewName, propertyName));
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

                CustomerAlerts.DBProperties = this.GetType().GetProperties()
                 .Where(prop => prop.Name != "ModifiedBy" && prop.Name != "ModifiedDate")
                 .ToList();

                CustomerAlerts.EndingPropertyName = "CreatedBy";

                this.Id = this.GetMax();

                l_Query = this.PrepareInsertQuery(this, CustomerAlerts.InsertQueryStart, CustomerAlerts.EndingPropertyName, CustomerAlerts.DBProperties);

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
                CustomerAlerts.DBProperties = this.GetType().GetProperties()
                 .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
                 .ToList();

                CustomerAlerts.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, CustomerAlerts.TableName, CustomerAlerts.PrimaryKeyName, CustomerAlerts.EndingPropertyName, CustomerAlerts.DBProperties);

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

                l_Query = this.PrepareDeleteQueryCustom(this, CustomerAlerts.TableName, CustomerAlerts.PrimaryKeyName);

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

        public Result UpdateAlertJobID(int Id, string JobID)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Process = this.Connection.Execute($@"UPDATE CustomerAlerts SET JobID = '{JobID}'
                                           WHERE Id = {Id} ");

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
            return ((CustomerAlerts)x).Id == ((CustomerAlerts)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion
    }
}
