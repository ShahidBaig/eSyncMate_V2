using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// A call to BizMate that is owed but not yet made (W1-14, requirement E22).
    ///
    /// Persisted before it is attempted, so nothing is lost across a restart on either side.
    /// Ordering is FIFO within a partner and unordered across partners - see EDIOutboundQueue.sql.
    ///
    /// Property order is the column order.
    /// </summary>
    public class EDIOutboundQueue : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public long Id { get; set; }
        public long? LedgerId { get; set; }
        public string PartnerId { get; set; }

        public string Operation { get; set; }
        public string HttpMethod { get; set; }
        public string PublicPath { get; set; }
        public string UrlPathWithQuery { get; set; }
        public string Scope { get; set; }
        public string Payload { get; set; }

        public string CorrelationId { get; set; }

        public string Status { get; set; }
        public int AttemptCount { get; set; }
        public DateTime NextAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public string LastError { get; set; }
        public DateTime? CompletedAt { get; set; }

        public DateTime? ClaimedAt { get; set; }
        public string ClaimedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        public EDIOutboundQueue() : base()
        {
            SetupDBEntity();
        }

        public EDIOutboundQueue(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public EDIOutboundQueue(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(EDIOutboundQueue.TableName))
            {
                EDIOutboundQueue.TableName = "EDIOutboundQueue";
            }

            if (string.IsNullOrEmpty(EDIOutboundQueue.ViewName))
            {
                EDIOutboundQueue.ViewName = "EDIOutboundQueue";
            }

            if (string.IsNullOrEmpty(EDIOutboundQueue.PrimaryKeyName))
            {
                EDIOutboundQueue.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(EDIOutboundQueue.EndingPropertyName))
            {
                EDIOutboundQueue.EndingPropertyName = "ModifiedBy";
                EDIOutboundQueue.InsertQueryStart = null;
            }

            if (EDIOutboundQueue.DBProperties == null)
            {
                EDIOutboundQueue.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(EDIOutboundQueue.InsertQueryStart))
            {
                EDIOutboundQueue.InsertQueryStart = PrepareQueries(this, EDIOutboundQueue.TableName, EDIOutboundQueue.EndingPropertyName, ref l_Query, EDIOutboundQueue.DBProperties, "Id");
            }

            // LastError must be a real NULL when the call has not failed, because
            // CK_EDIOutboundQueue_FailedReason tests IS NOT NULL and an empty string would satisfy
            // it while telling an operator nothing.
            this.Nullables = new List<string>
            {
                "UrlPathWithQuery",
                "Payload",
                "LastError",
                "ClaimedBy"
            };
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
                ? "SELECT * FROM [" + EDIOutboundQueue.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + EDIOutboundQueue.TableName + "]";

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
        /// The partners with work that is due, oldest waiting first. The drain visits partners in
        /// this order so a partner that has been waiting longest is served first, without any
        /// partner's backlog blocking the others.
        /// </summary>
        public bool GetPartnersWithDueWork(int p_MaxPartners, ref DataTable p_Data)
        {
            string l_Query =
                "SELECT TOP " + p_MaxPartners + " PartnerId, MIN(NextAttemptAt) AS DueSince, COUNT(*) AS Depth " +
                "FROM [" + EDIOutboundQueue.TableName + "] WITH (NOLOCK) " +
                "WHERE Status = 'Pending' AND NextAttemptAt <= SYSUTCDATETIME() " +
                "GROUP BY PartnerId ORDER BY MIN(NextAttemptAt) ASC";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// How many calls are waiting for one partner. Feeds the QueueBacklog signal on the
        /// connection-state feed (W2-17) and the acceptance ceiling.
        /// </summary>
        public int GetDepth(string p_PartnerId)
        {
            object l_Depth = Connection.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + EDIOutboundQueue.TableName + "] WITH (NOLOCK) " +
                "WHERE PartnerId = '" + PublicFunctions.DoQuotes(p_PartnerId) + "' " +
                "AND Status IN ('Pending','InFlight')");

            return l_Depth == null || l_Depth == DBNull.Value ? 0 : Convert.ToInt32(l_Depth);
        }

        /// <summary>
        /// Claims the next due call for one partner, atomically, and only when nothing older for
        /// that partner is still in flight.
        ///
        /// The NOT EXISTS clause is the FIFO guarantee: a call cannot be attempted while an earlier
        /// one for the same partner is outstanding, so a document can never overtake its own
        /// predecessor. UPDATE ... OUTPUT with ROWLOCK/READPAST makes the claim safe when several
        /// workers drain at once - a row another worker holds is skipped rather than waited on.
        /// </summary>
        public Result ClaimNext(string p_PartnerId, string p_ClaimedBy)
        {
            string l_Partner = PublicFunctions.DoQuotes(p_PartnerId);
            string l_Worker = PublicFunctions.DoQuotes(p_ClaimedBy);

            string l_Query =
                "UPDATE q SET q.Status = 'InFlight', q.ClaimedAt = SYSUTCDATETIME(), q.ClaimedBy = '" + l_Worker + "' " +
                "OUTPUT INSERTED.* " +
                "FROM [" + EDIOutboundQueue.TableName + "] q WITH (ROWLOCK, READPAST) " +
                "WHERE q.Id = ( " +
                "    SELECT TOP 1 n.Id FROM [" + EDIOutboundQueue.TableName + "] n WITH (ROWLOCK, READPAST) " +
                "    WHERE n.PartnerId = '" + l_Partner + "' AND n.Status = 'Pending' " +
                "      AND n.NextAttemptAt <= SYSUTCDATETIME() " +
                "      AND NOT EXISTS ( " +
                "          SELECT 1 FROM [" + EDIOutboundQueue.TableName + "] f WITH (NOLOCK) " +
                "          WHERE f.PartnerId = n.PartnerId AND f.Status = 'InFlight' AND f.Id < n.Id) " +
                "    ORDER BY n.Id ASC)";

            DataTable l_Data = new DataTable();

            if (!Connection.GetData(l_Query, ref l_Data) || l_Data.Rows.Count == 0)
            {
                l_Data.Dispose();

                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, EDIOutboundQueue.EndingPropertyName);

            l_Data.Dispose();

            return Result.GetSuccessResult();
        }

        /// <summary>
        /// Returns claims held longer than the stale window to Pending. A worker that died
        /// mid-flight would otherwise block its partner's queue permanently, since the FIFO rule
        /// refuses to step over an InFlight row.
        /// </summary>
        public int ReclaimStale(int p_StaleMinutes)
        {
            object l_Count = Connection.ExecuteScalar(
                "UPDATE [" + EDIOutboundQueue.TableName + "] " +
                "SET Status = 'Pending', ClaimedAt = NULL, ClaimedBy = NULL, " +
                "    LastError = 'Reclaimed after the claim went stale.' " +
                "WHERE Status = 'InFlight' " +
                "AND ClaimedAt < DATEADD(MINUTE, -" + p_StaleMinutes + ", SYSUTCDATETIME()); " +
                "SELECT @@ROWCOUNT;");

            return l_Count == null || l_Count == DBNull.Value ? 0 : Convert.ToInt32(l_Count);
        }

        public int GetMax()
        {
            object l_Max = Connection.ExecuteScalar("SELECT ISNULL(MAX(Id), 0) FROM [" + EDIOutboundQueue.TableName + "]");

            return l_Max == null || l_Max == DBNull.Value ? 0 : Convert.ToInt32(l_Max);
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, EDIOutboundQueue.TableName, EDIOutboundQueue.PrimaryKeyName));
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

            PopulateObject(this, l_Data, DBProperties, EDIOutboundQueue.EndingPropertyName);

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

                string l_Query = this.PrepareInsertQuery(this, EDIOutboundQueue.InsertQueryStart, EDIOutboundQueue.EndingPropertyName, EDIOutboundQueue.DBProperties, "Id");

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

        public Result Modify()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;

            try
            {
                this.ModifiedDate = DateTime.UtcNow;

                l_Trans = this.Connection.BeginTransaction();

                string l_Query = this.PrepareUpdateQuery(this, EDIOutboundQueue.TableName, EDIOutboundQueue.PrimaryKeyName, EDIOutboundQueue.EndingPropertyName, EDIOutboundQueue.DBProperties);

                if (this.Connection.Execute(l_Query))
                {
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
        /// A queued call is never deleted while it is owed. Settled rows age out on their own
        /// schedule; removing one that has not settled is how a document goes missing.
        /// </summary>
        public Result Delete()
        {
            return Result.GetFailureResult();
        }

        public new bool Equals(object x, object y)
        {
            return x is EDIOutboundQueue l_Left && y is EDIOutboundQueue l_Right && l_Left.Id == l_Right.Id;
        }

        public int GetHashCode(object obj)
        {
            return obj is EDIOutboundQueue l_Item ? l_Item.Id.GetHashCode() : 0;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
