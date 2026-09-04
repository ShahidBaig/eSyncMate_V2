using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// One document's relationship to another (W2-10, W2-12, W3-19).
    ///
    /// A 997 acknowledges an outbound interchange, an 824 rejects a specific document, an 860
    /// changes an 850. All the same shape, so the relationship is a row rather than a nullable
    /// column per case. An arriving document that cannot be resolved to its target is still a
    /// first-class ledger record with no link, which is the visible failure W2-12 wants.
    ///
    /// Property order is the column order - see EDILedgerLink.sql.
    /// </summary>
    public class EDILedgerLink : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public long Id { get; set; }
        public long FromLedgerId { get; set; }
        public long ToLedgerId { get; set; }
        public string LinkType { get; set; }
        public string ResolvedBy { get; set; }
        public string ResolvedOn { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        public EDILedgerLink() : base()
        {
            SetupDBEntity();
        }

        public EDILedgerLink(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public EDILedgerLink(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(EDILedgerLink.TableName))
            {
                EDILedgerLink.TableName = "EDILedgerLink";
            }

            if (string.IsNullOrEmpty(EDILedgerLink.ViewName))
            {
                EDILedgerLink.ViewName = "EDILedgerLink";
            }

            if (string.IsNullOrEmpty(EDILedgerLink.PrimaryKeyName))
            {
                EDILedgerLink.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(EDILedgerLink.EndingPropertyName))
            {
                EDILedgerLink.EndingPropertyName = "CreatedBy";
                EDILedgerLink.InsertQueryStart = null;
            }

            if (EDILedgerLink.DBProperties == null)
            {
                EDILedgerLink.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(EDILedgerLink.InsertQueryStart))
            {
                EDILedgerLink.InsertQueryStart = PrepareQueries(this, EDILedgerLink.TableName, EDILedgerLink.EndingPropertyName, ref l_Query, EDILedgerLink.DBProperties, "Id");
            }

            this.Nullables = new List<string> { "ResolvedOn", "Detail" };
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
                ? "SELECT * FROM [" + EDILedgerLink.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + EDILedgerLink.TableName + "]";

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

        /// <summary>Everything said about one document - the reverse lookup the trace actually asks.</summary>
        public bool GetHistoryFor(long p_LedgerId, ref DataTable p_Data)
        {
            return GetList("ToLedgerId = " + p_LedgerId, string.Empty, ref p_Data, "CreatedDate DESC, Id DESC");
        }

        public int GetMax()
        {
            object l_Max = Connection.ExecuteScalar("SELECT ISNULL(MAX(Id), 0) FROM [" + EDILedgerLink.TableName + "]");

            return l_Max == null || l_Max == DBNull.Value ? 0 : Convert.ToInt32(l_Max);
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, EDILedgerLink.TableName, EDILedgerLink.PrimaryKeyName));
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

            PopulateObject(this, l_Data, DBProperties, EDILedgerLink.EndingPropertyName);

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

                string l_Query = this.PrepareInsertQuery(this, EDILedgerLink.InsertQueryStart, EDILedgerLink.EndingPropertyName, EDILedgerLink.DBProperties, "Id");

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
                l_Trans = this.Connection.BeginTransaction();

                string l_Query = this.PrepareUpdateQuery(this, EDILedgerLink.TableName, EDILedgerLink.PrimaryKeyName, EDILedgerLink.EndingPropertyName, EDILedgerLink.DBProperties);

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
        /// A link may be withdrawn - a correlator can get one wrong and a person can correct it -
        /// unlike the ledger rows it joins, which are never removed.
        /// </summary>
        public Result Delete()
        {
            Result l_Result = Result.GetFailureResult();

            string l_Query = PrepareDeleteQuery(this, EDILedgerLink.TableName, EDILedgerLink.PrimaryKeyName);

            if (Connection.Execute(l_Query))
            {
                l_Result = Result.GetSuccessResult();
            }

            return l_Result;
        }

        public new bool Equals(object x, object y)
        {
            return x is EDILedgerLink l_Left && y is EDILedgerLink l_Right && l_Left.Id == l_Right.Id;
        }

        public int GetHashCode(object obj)
        {
            return obj is EDILedgerLink l_Link ? l_Link.Id.GetHashCode() : 0;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
