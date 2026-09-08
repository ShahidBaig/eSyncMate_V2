using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// The per-document record (W2-01, requirements E1, E10, E11, E12, E13, E17, E18).
    ///
    /// One row per document, both directions, every carrier, written BEFORE the document is
    /// translated so a document that dies still dies visibly (W2-02).
    ///
    /// Property order is the column order and must match EDILedger.sql: the base class builds its
    /// insert and update queries by reflecting over the public properties up to EndingPropertyName.
    /// Anything that is not a column belongs after ModifiedBy, or must not be a public property.
    /// </summary>
    public class EDILedger : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public long Id { get; set; }

        // Identity linkage (W2-06). BizMate's keys are echoed, never invented here.
        public string TransmissionReference { get; set; }
        public string PartnerId { get; set; }
        public string PartnerControlNo { get; set; }

        /// <summary>
        /// The control number that actually crossed the wire. Ours on an outbound interchange
        /// (EQ-01), the partner's on an inbound one. An inbound 997 arrives quoting this, so it is
        /// what W2-10 correlates on - which is why it cannot share PartnerControlNo's column.
        /// </summary>
        public string InterchangeControlNo { get; set; }

        public string CustomerNo { get; set; }
        public string CorrelationId { get; set; }
        public long? BizMateMessageId { get; set; }
        public bool BizMateDuplicate { get; set; }

        // Classification. Mechanism is derived from Format, never set independently - see
        // CK_EDILedger_MechanismDerivation and BizMateFormats.ToMechanism.
        public string Direction { get; set; }
        public string DocumentType { get; set; }
        public string Family { get; set; }
        public string Format { get; set; }
        public string Mechanism { get; set; }
        public string Channel { get; set; }
        public string Provenance { get; set; }

        // Translation
        public string MapName { get; set; }
        public string MapVersion { get; set; }
        public string Outcome { get; set; }
        public string ErrorDetail { get; set; }

        // Hop timestamps (W2-07), all UTC.
        public DateTime ReceivedAt { get; set; }
        public DateTime? TranslatedAt { get; set; }
        public DateTime? HandedToBizMateAt { get; set; }
        public DateTime? DeliveredToPartnerAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// eSyncMate's own reference to the artifact, which is what the trace contract means by
        /// rawArtifactRef ("BizMate does not dereference it"): 'inbound-edi:{id}',
        /// 'outbound-edi:{id}' or 'artifact:{id}'. BizMate's own reference is BizMateRawFileRef.
        /// </summary>
        public string RawArtifactRef { get; set; }

        // All nullable on purpose: the ledger covers every carrier, and an inbound document need
        // not have become an order to be recorded. (F-3 as first stated was retracted by F-14 -
        // InboundEDI already gave X12 that independence - but the columns stay nullable.)
        public int? OrderId { get; set; }
        public int? RouteId { get; set; }
        public int? CustomerId { get; set; }

        // AD-02 (2026-09-08): for X12 the raw artifact is the row eSyncMate already keeps, not a copy.
        // Inbound links InboundEDI (with its InboundEDIInfo identity rows); outbound links OutboundEDI.
        // Both nullable: the flat-file and DB-map carriers have no such row and use EDILedgerArtifact.
        // CK_EDILedger_RawLinkDirection stops an inbound row pointing at OutboundEDI and vice versa.
        public int? InboundEDIId { get; set; }
        public int? OutboundEDIId { get; set; }

        /// <summary>
        /// The rawFileRef BizMate returned from POST /raw. Kept apart from RawArtifactRef, which
        /// the contract defines as OUR reference; the first revision stored BizMate's there (F-21).
        /// </summary>
        public long? BizMateRawFileRef { get; set; }

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

        public EDILedger() : base()
        {
            SetupDBEntity();
        }

        public EDILedger(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        public EDILedger(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(EDILedger.TableName))
            {
                EDILedger.TableName = "EDILedger";
            }

            if (string.IsNullOrEmpty(EDILedger.ViewName))
            {
                EDILedger.ViewName = "EDILedger";
            }

            if (string.IsNullOrEmpty(EDILedger.PrimaryKeyName))
            {
                EDILedger.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(EDILedger.EndingPropertyName))
            {
                EDILedger.EndingPropertyName = "ModifiedBy";
                EDILedger.InsertQueryStart = null;
            }

            if (EDILedger.DBProperties == null)
            {
                EDILedger.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(EDILedger.InsertQueryStart))
            {
                EDILedger.InsertQueryStart = PrepareQueries(this, EDILedger.TableName, EDILedger.EndingPropertyName, ref l_Query, EDILedger.DBProperties, "Id");
            }

            // Written as SQL NULL rather than an empty string when unset. This is not cosmetic:
            // CK_EDILedger_ErrorDetail tests ErrorDetail IS NOT NULL, and an empty string would
            // satisfy the constraint while telling an operator nothing. The same reasoning applies
            // to every optional identity column - a blank PartnerControlNo is not a control number.
            this.Nullables = new List<string>
            {
                "PartnerControlNo",
                "InterchangeControlNo",
                "CustomerNo",
                "Family",
                "MapName",
                "MapVersion",
                "ErrorDetail",
                "RawArtifactRef"
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
            string l_Query;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + EDILedger.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + EDILedger.TableName + "]";
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
            string l_Query;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + EDILedger.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + EDILedger.ViewName + "]";
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

        public Result GetObject()
        {
            string l_Query = PrepareGetObjectQuery(this, EDILedger.TableName, EDILedger.PrimaryKeyName);

            return GetObjectByQuery(l_Query);
        }

        /// <summary>
        /// Trace lookup 2 (esyncmate-trace-api.md): by partner and the partner's control number.
        /// Served by IX_EDILedger_PartnerControl.
        /// </summary>
        public Result GetByPartnerControlNo(string p_PartnerId, string p_PartnerControlNo, string p_Direction)
        {
            string l_Query =
                "SELECT TOP 1 * FROM [" + EDILedger.TableName + "] WITH (NOLOCK) " +
                "WHERE PartnerId = '" + PublicFunctions.DoQuotes(p_PartnerId) + "' " +
                "AND PartnerControlNo = '" + PublicFunctions.DoQuotes(p_PartnerControlNo) + "' ";

            if (!string.IsNullOrEmpty(p_Direction))
            {
                l_Query += "AND Direction = '" + PublicFunctions.DoQuotes(p_Direction) + "' ";
            }

            l_Query += "ORDER BY ReceivedAt DESC";

            return GetObjectByQuery(l_Query);
        }

        /// <summary>
        /// Trace lookup 1 (esyncmate-trace-api.md): by our own transmission reference.
        /// Served by UX_EDILedger_TransmissionReference.
        /// </summary>
        public Result GetByTransmissionReference(string p_TransmissionReference)
        {
            string l_Query =
                "SELECT TOP 1 * FROM [" + EDILedger.TableName + "] WITH (NOLOCK) " +
                "WHERE TransmissionReference = '" + PublicFunctions.DoQuotes(p_TransmissionReference) + "'";

            return GetObjectByQuery(l_Query);
        }

        /// <summary>
        /// Resolves an inbound 997 or CONTRL to the outbound transmission it acknowledges (W2-10),
        /// by the control number that actually crossed the wire. Served by
        /// IX_EDILedger_InterchangeControlNo.
        /// </summary>
        public Result GetOutboundByInterchangeControlNo(string p_PartnerId, string p_InterchangeControlNo)
        {
            string l_Query =
                "SELECT TOP 1 * FROM [" + EDILedger.TableName + "] WITH (NOLOCK) " +
                "WHERE PartnerId = '" + PublicFunctions.DoQuotes(p_PartnerId) + "' " +
                "AND InterchangeControlNo = '" + PublicFunctions.DoQuotes(p_InterchangeControlNo) + "' " +
                "AND Direction = 'Out' " +
                "ORDER BY DeliveredToPartnerAt DESC, Id DESC";

            return GetObjectByQuery(l_Query);
        }

        public Result GetObjectByQuery(string p_Query)
        {
            DataTable l_Data = new DataTable();

            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            if (l_Data.Rows.Count == 0)
            {
                l_Data.Dispose();

                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, EDILedger.EndingPropertyName);

            l_Data.Dispose();

            return Result.GetSuccessResult();
        }

        public Result SaveNew()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            string l_Query;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareInsertQuery(this, EDILedger.InsertQueryStart, EDILedger.EndingPropertyName, EDILedger.DBProperties, "Id");

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
            string l_Query;

            try
            {
                this.ModifiedDate = DateTime.UtcNow;

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, EDILedger.TableName, EDILedger.PrimaryKeyName, EDILedger.EndingPropertyName, EDILedger.DBProperties);

                if (this.Connection.Execute(l_Query))
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

        public int GetMax()
        {
            object l_Max = Connection.ExecuteScalar("SELECT ISNULL(MAX(Id), 0) FROM [" + EDILedger.TableName + "]");

            return l_Max == null || l_Max == DBNull.Value ? 0 : Convert.ToInt32(l_Max);
        }

        public Result GetObjectFromQuery(string p_Query, bool p_IsOnlyObject = false)
        {
            return GetObjectByQuery(p_Query);
        }

        public Result GetObjectOnly()
        {
            return GetObject();
        }

        /// <summary>
        /// A ledger row is never deleted. It is the record that a document existed at all, and both
        /// E10 (the dispute promise) and E12 (failures are visible, not absent) depend on that
        /// record surviving whatever happened to the document it describes. Retention ages the
        /// artifact bytes out; it never removes the record of the exchange.
        /// </summary>
        public Result Delete()
        {
            return Result.GetFailureResult();
        }

        public new bool Equals(object x, object y)
        {
            return x is EDILedger l_Left && y is EDILedger l_Right && l_Left.Id == l_Right.Id;
        }

        public int GetHashCode(object obj)
        {
            return obj is EDILedger l_Ledger ? l_Ledger.Id.GetHashCode() : 0;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
