using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// The thing that actually crossed the wire (W2-03, requirement E10).
    ///
    /// Content is byte[] against a VARBINARY column rather than a string: the hash is over the raw
    /// bytes exactly as sent or received, and a byte-order mark, a CRLF that should have been an
    /// LF, or a re-encoding would each change it. Storing text and re-encoding on the way out makes
    /// the hash unreproducible, which defeats holding the artifact at all.
    ///
    /// Property order is the column order - see EDILedgerArtifact.sql.
    /// </summary>
    public class EDILedgerArtifact : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public long Id { get; set; }
        public long LedgerId { get; set; }

        public string Stage { get; set; }
        public string FormatLabel { get; set; }
        public string ContentEncoding { get; set; }

        public string ContentHash { get; set; }
        public long SizeBytes { get; set; }
        public byte[] Content { get; set; }
        public string ExternalRef { get; set; }

        public DateTime? ExpiresAt { get; set; }
        public DateTime? ArchivedAt { get; set; }

        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        public EDILedgerArtifact() : base()
        {
            SetupDBEntity();
        }

        public EDILedgerArtifact(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public EDILedgerArtifact(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(EDILedgerArtifact.TableName))
            {
                EDILedgerArtifact.TableName = "EDILedgerArtifact";
            }

            if (string.IsNullOrEmpty(EDILedgerArtifact.ViewName))
            {
                EDILedgerArtifact.ViewName = "EDILedgerArtifact";
            }

            if (string.IsNullOrEmpty(EDILedgerArtifact.PrimaryKeyName))
            {
                EDILedgerArtifact.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(EDILedgerArtifact.EndingPropertyName))
            {
                EDILedgerArtifact.EndingPropertyName = "CreatedBy";
                EDILedgerArtifact.InsertQueryStart = null;
            }

            if (EDILedgerArtifact.DBProperties == null)
            {
                EDILedgerArtifact.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(EDILedgerArtifact.InsertQueryStart))
            {
                EDILedgerArtifact.InsertQueryStart = PrepareQueries(this, EDILedgerArtifact.TableName, EDILedgerArtifact.EndingPropertyName, ref l_Query, EDILedgerArtifact.DBProperties, "Id");
            }

            // ExternalRef must be a real NULL when the bytes are held inline, because
            // CK_EDILedgerArtifact_Content tests for one of the two being present and an empty
            // string would pass while pointing nowhere.
            this.Nullables = new List<string> { "ExternalRef" };
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
                ? "SELECT * FROM [" + EDILedgerArtifact.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + EDILedgerArtifact.TableName + "]";

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
        /// Every artifact held for one document, newest first. Deliberately every version, not the
        /// latest per stage: a re-sent 856 must not hide the one actually sent on the disputed date.
        /// </summary>
        public bool GetForLedger(long p_LedgerId, ref DataTable p_Data)
        {
            return GetList("LedgerId = " + p_LedgerId, string.Empty, ref p_Data, "CreatedDate DESC, Id DESC");
        }

        public int GetMax()
        {
            object l_Max = Connection.ExecuteScalar("SELECT ISNULL(MAX(Id), 0) FROM [" + EDILedgerArtifact.TableName + "]");

            return l_Max == null || l_Max == DBNull.Value ? 0 : Convert.ToInt32(l_Max);
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, EDILedgerArtifact.TableName, EDILedgerArtifact.PrimaryKeyName));
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

            PopulateObject(this, l_Data, DBProperties, EDILedgerArtifact.EndingPropertyName);

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

                string l_Query = this.PrepareInsertQuery(this, EDILedgerArtifact.InsertQueryStart, EDILedgerArtifact.EndingPropertyName, EDILedgerArtifact.DBProperties, "Id");

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
        /// An artifact is written once. The only permitted change is the archive transition, which
        /// moves the bytes out to ExternalRef - the content itself is never edited in place.
        /// </summary>
        public Result Modify()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                string l_Query = this.PrepareUpdateQuery(this, EDILedgerArtifact.TableName, EDILedgerArtifact.PrimaryKeyName, EDILedgerArtifact.EndingPropertyName, EDILedgerArtifact.DBProperties);

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

        /// <summary>Artifacts age out through the retention policy, never by an ad-hoc delete.</summary>
        public Result Delete()
        {
            return Result.GetFailureResult();
        }

        public new bool Equals(object x, object y)
        {
            return x is EDILedgerArtifact l_Left && y is EDILedgerArtifact l_Right && l_Left.Id == l_Right.Id;
        }

        public int GetHashCode(object obj)
        {
            return obj is EDILedgerArtifact l_Artifact ? l_Artifact.Id.GetHashCode() : 0;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
