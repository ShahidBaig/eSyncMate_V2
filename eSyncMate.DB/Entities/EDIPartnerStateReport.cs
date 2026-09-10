using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// One partner state eSyncMate has reported to BizMate (W2-17, E18).
    ///
    /// Append-only: the latest row for a partner is the current state, and the rows before it are
    /// how it got there. Only states BizMate accepted are written - a report that failed to send is
    /// not a report, and has to be tried again on the next pass.
    ///
    /// This is a table rather than a static dictionary because every route execution is a fresh
    /// RouteWorker process (RouteEngine:UseExternalProcess), so in-memory state is born empty on
    /// every pass. It also answers "what have we told BizMate, and when" without reading log text.
    ///
    /// Property order is the column order - see Tasks/00003/15_EDIPartnerStateReport.sql.
    /// </summary>
    public class EDIPartnerStateReport : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public long Id { get; set; }
        public string PartnerId { get; set; }
        public string State { get; set; }
        public string Detail { get; set; }
        public DateTime ReportedAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        public EDIPartnerStateReport() : base()
        {
            SetupDBEntity();
        }

        public EDIPartnerStateReport(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public EDIPartnerStateReport(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(EDIPartnerStateReport.TableName))
            {
                EDIPartnerStateReport.TableName = "EDIPartnerStateReport";
            }

            if (string.IsNullOrEmpty(EDIPartnerStateReport.ViewName))
            {
                EDIPartnerStateReport.ViewName = "EDIPartnerStateReport";
            }

            if (string.IsNullOrEmpty(EDIPartnerStateReport.PrimaryKeyName))
            {
                EDIPartnerStateReport.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(EDIPartnerStateReport.EndingPropertyName))
            {
                EDIPartnerStateReport.EndingPropertyName = "CreatedBy";
                EDIPartnerStateReport.InsertQueryStart = null;
            }

            if (EDIPartnerStateReport.DBProperties == null)
            {
                EDIPartnerStateReport.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(EDIPartnerStateReport.InsertQueryStart))
            {
                EDIPartnerStateReport.InsertQueryStart = PrepareQueries(this, EDIPartnerStateReport.TableName, EDIPartnerStateReport.EndingPropertyName, ref l_Query, EDIPartnerStateReport.DBProperties, "Id");
            }

            this.Nullables = new List<string> { "Detail" };
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
                ? "SELECT * FROM [" + EDIPartnerStateReport.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + EDIPartnerStateReport.TableName + "]";

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
            return GetList(p_Criteria, p_Fields, ref p_Data, p_OrderBy);
        }

        /// <summary>
        /// The state last reported for this partner, or empty when we have never reported one.
        ///
        /// This is the whole reason the table exists: it is what a route pass asks to decide whether
        /// a steady state such as MapDisabled has already been said. Reads the top of the partner's
        /// rows through IX_EDIPartnerStateReport_Latest rather than scanning the history.
        /// </summary>
        public string GetLatestState(string p_PartnerId)
        {
            object l_State = Connection.ExecuteScalar(
                "SELECT TOP 1 State FROM [" + EDIPartnerStateReport.TableName + "] WITH (NOLOCK) " +
                "WHERE PartnerId = '" + PublicFunctions.DoQuotes(p_PartnerId) + "' ORDER BY Id DESC");

            return l_State == null || l_State == DBNull.Value ? string.Empty : Convert.ToString(l_State);
        }

        public int GetMax()
        {
            object l_Max = Connection.ExecuteScalar("SELECT ISNULL(MAX(Id), 0) FROM [" + EDIPartnerStateReport.TableName + "]");

            return l_Max == null || l_Max == DBNull.Value ? 0 : Convert.ToInt32(l_Max);
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, EDIPartnerStateReport.TableName, EDIPartnerStateReport.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObject();
        }

        public Result GetObjectFromQuery(string p_Query, bool p_IsOnlyObject = false)
        {
            DataTable l_Data = new DataTable();

            if (!Connection.GetData(p_Query, ref l_Data) || l_Data.Rows.Count == 0)
            {
                l_Data.Dispose();

                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, EDIPartnerStateReport.EndingPropertyName);

            l_Data.Dispose();

            return Result.GetSuccessResult();
        }

        public Result SaveNew()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                string l_Query = this.PrepareInsertQuery(this, EDIPartnerStateReport.InsertQueryStart, EDIPartnerStateReport.EndingPropertyName, EDIPartnerStateReport.DBProperties, "Id");

                object l_Id = this.Connection.ExecuteScalar(l_Query + "; SELECT SCOPE_IDENTITY();");

                if (l_Id != null && l_Id != DBNull.Value)
                {
                    this.Id = Convert.ToInt64(l_Id);
                    l_Result = Result.GetSuccessResult();

                    if (l_Trans)
                    {
                        this.Connection.CommitTransaction();
                    }
                }
                else if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
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

        /// <summary>
        /// A report of what we told BizMate at a moment is not something that later becomes untrue,
        /// so there is nothing to modify. A new state is a new row.
        /// </summary>
        public Result Modify()
        {
            return Result.GetFailureResult();
        }

        /// <summary>Never removed: this is the record of what we said, and we did say it.</summary>
        public Result Delete()
        {
            return Result.GetFailureResult();
        }

        public new bool Equals(object x, object y)
        {
            return x is EDIPartnerStateReport l_Left && y is EDIPartnerStateReport l_Right && l_Left.Id == l_Right.Id;
        }

        public int GetHashCode(object obj)
        {
            return obj is EDIPartnerStateReport l_Report ? l_Report.Id.GetHashCode() : 0;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
