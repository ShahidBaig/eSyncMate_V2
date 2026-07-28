using System;
using System.Data;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// Ship node mappings used by the inventory feed routes: one warehouse of one customer maps to
    /// exactly one ship node.
    ///
    /// Two tables share this shape and are driven by the same Setup screen:
    ///   TargetPlusShipNodes — ID, WHSID, ShipNode, CustomerID
    ///   WalmartShipNodes    — ID, WHSID, ShipNode, CustomerID, APIName
    ///
    /// APIName is Walmart-only and is still read by the Walmart inventory routes, so it is written
    /// alongside CustomerID. ID is IDENTITY in both tables, so statements are written explicitly
    /// here rather than through the reflection-based DBEntity query builders.
    /// </summary>
    public class ShipNodes : DBEntity, IDisposable
    {
        public const string SourceTargetPlus = "TARGETPLUS";
        public const string SourceWalmart = "WALMART";

        public const string DefaultWalmartApiName = "WalmartAPI";

        public int ID { get; set; }
        public string WHSID { get; set; }
        public string ShipNode { get; set; }
        public string CustomerID { get; set; }

        /// <summary>Walmart only — ignored for Target Plus rows.</summary>
        public string APIName { get; set; }

        private readonly string m_TableName;
        private readonly bool m_HasApiName;

        public ShipNodes(string p_Source = SourceTargetPlus) : base()
        {
            m_HasApiName = IsWalmart(p_Source);
            m_TableName = m_HasApiName ? "WalmartShipNodes" : "TargetPlusShipNodes";
        }

        public static bool IsWalmart(string p_Source)
        {
            return string.Equals(p_Source, SourceWalmart, StringComparison.OrdinalIgnoreCase);
        }

        public string TableName { get { return m_TableName; } }

        public bool HasApiName { get { return m_HasApiName; } }

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

        /// <summary>Single quotes are doubled so values with apostrophes cannot break the statement.</summary>
        private static string Escape(string p_Value)
        {
            return (p_Value ?? string.Empty).Replace("'", "''").Trim();
        }

        private string SelectColumns()
        {
            return m_HasApiName
                ? "ID, WHSID, ShipNode, CustomerID, APIName"
                : "ID, WHSID, ShipNode, CustomerID, '' AS APIName";
        }

        public bool GetListPaged(string p_Criteria, ref DataTable p_Data, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;

            string l_CountQuery = "SELECT COUNT(*) FROM [" + m_TableName + "]";
            if (!string.IsNullOrEmpty(p_Criteria))
                l_CountQuery += " WHERE " + p_Criteria;

            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0)
                totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_Query = "SELECT " + SelectColumns() + " FROM [" + m_TableName + "]";

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            int offset = (pageNumber - 1) * pageSize;
            l_Query += " ORDER BY CustomerID, WHSID"
                     + $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// Both tables as one result set, tagged with the table they came from.
        /// Requires WalmartShipNodes.CustomerID (deployment script 56).
        /// </summary>
        private const string CombinedSource =
            "(SELECT ID, WHSID, ShipNode, CustomerID, '' AS APIName, '" + SourceTargetPlus + "' AS Source FROM [TargetPlusShipNodes]"
          + " UNION ALL "
          + "SELECT ID, WHSID, ShipNode, CustomerID, APIName, '" + SourceWalmart + "' AS Source FROM [WalmartShipNodes]) AS N";

        public bool GetCombinedListPaged(string p_Criteria, ref DataTable p_Data, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;

            string l_CountQuery = "SELECT COUNT(*) FROM " + CombinedSource;
            if (!string.IsNullOrEmpty(p_Criteria))
                l_CountQuery += " WHERE " + p_Criteria;

            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0)
                totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_Query = "SELECT ID, WHSID, ShipNode, CustomerID, APIName, Source FROM " + CombinedSource;

            if (!string.IsNullOrEmpty(p_Criteria))
                l_Query += " WHERE " + p_Criteria;

            int offset = (pageNumber - 1) * pageSize;
            l_Query += " ORDER BY CustomerID, WHSID"
                     + $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetCombinedCustomers(ref DataTable p_Data)
        {
            string l_Query = "SELECT DISTINCT CustomerID FROM " + CombinedSource
                           + " WHERE ISNULL(CustomerID, '') <> '' ORDER BY CustomerID";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetCombinedWarehouses(ref DataTable p_Data)
        {
            string l_Query = "SELECT DISTINCT WHSID FROM " + CombinedSource
                           + " WHERE ISNULL(WHSID, '') <> '' ORDER BY WHSID";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public Result GetObject(int p_PrimaryKey)
        {
            DataTable l_Data = new DataTable();

            string l_Query = "SELECT " + SelectColumns() + " FROM [" + m_TableName + "] WHERE ID = " + p_PrimaryKey;

            if (!Connection.GetData(l_Query, ref l_Data) || l_Data.Rows.Count == 0)
            {
                l_Data.Dispose();
                return Result.GetNoRecordResult();
            }

            DataRow l_Row = l_Data.Rows[0];
            this.ID = PublicFunctions.ConvertNullAsInteger(l_Row["ID"], 0);
            this.WHSID = PublicFunctions.ConvertNullAsString(l_Row["WHSID"], string.Empty);
            this.ShipNode = PublicFunctions.ConvertNullAsString(l_Row["ShipNode"], string.Empty);
            this.CustomerID = PublicFunctions.ConvertNullAsString(l_Row["CustomerID"], string.Empty);
            this.APIName = PublicFunctions.ConvertNullAsString(l_Row["APIName"], string.Empty);

            l_Data.Dispose();

            return Result.GetSuccessResult();
        }

        /// <summary>Warehouses already in use — feeds the WHSID dropdown on the Setup screen.</summary>
        public bool GetWarehouses(ref DataTable p_Data)
        {
            string l_Query = "SELECT DISTINCT WHSID FROM [" + m_TableName + "]"
                           + " WHERE ISNULL(WHSID, '') <> '' ORDER BY WHSID";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>Customers that actually have ship nodes — feeds the list screen filter.</summary>
        public bool GetCustomers(ref DataTable p_Data)
        {
            string l_Query = "SELECT DISTINCT CustomerID FROM [" + m_TableName + "]"
                           + " WHERE ISNULL(CustomerID, '') <> '' ORDER BY CustomerID";

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// A customer's warehouse may hold only one ship node, and that ship node may not be reused
        /// on another warehouse of the same customer. Returns the clashing column name
        /// ("WHSID" / "ShipNode") or an empty string when the combination is free.
        /// </summary>
        public string GetDuplicate(string p_CustomerID, string p_WHSID, string p_ShipNode, int p_ExcludeID = 0)
        {
            DataTable l_Data = new DataTable();
            string l_Exclude = p_ExcludeID > 0 ? " AND ID <> " + p_ExcludeID : string.Empty;

            string l_Query = "SELECT TOP 1 CASE WHEN WHSID = '" + Escape(p_WHSID) + "' THEN 'WHSID' ELSE 'ShipNode' END AS Clash"
                           + " FROM [" + m_TableName + "]"
                           + " WHERE CustomerID = '" + Escape(p_CustomerID) + "'"
                           + " AND (WHSID = '" + Escape(p_WHSID) + "' OR ShipNode = '" + Escape(p_ShipNode) + "')"
                           + l_Exclude;

            if (!Connection.GetData(l_Query, ref l_Data) || l_Data.Rows.Count == 0)
            {
                l_Data.Dispose();
                return string.Empty;
            }

            string l_Clash = PublicFunctions.ConvertNullAsString(l_Data.Rows[0]["Clash"], string.Empty);
            l_Data.Dispose();

            return l_Clash;
        }

        public Result SaveNew()
        {
            string l_Columns = "WHSID, ShipNode, CustomerID";
            string l_Values = "'" + Escape(this.WHSID) + "', "
                            + "'" + Escape(this.ShipNode) + "', "
                            + "'" + Escape(this.CustomerID) + "'";

            if (m_HasApiName)
            {
                l_Columns += ", APIName";
                l_Values += ", '" + Escape(string.IsNullOrEmpty(this.APIName) ? DefaultWalmartApiName : this.APIName) + "'";
            }

            // ID is IDENTITY — never supplied
            return RunInTransaction("INSERT INTO [" + m_TableName + "] (" + l_Columns + ") VALUES (" + l_Values + ")");
        }

        public Result Modify()
        {
            string l_Query = "UPDATE [" + m_TableName + "] SET "
                           + "WHSID = '" + Escape(this.WHSID) + "', "
                           + "ShipNode = '" + Escape(this.ShipNode) + "', "
                           + "CustomerID = '" + Escape(this.CustomerID) + "'";

            // APIName is not exposed in the UI — leave the stored value alone unless one was supplied
            if (m_HasApiName && !string.IsNullOrEmpty(this.APIName))
            {
                l_Query += ", APIName = '" + Escape(this.APIName) + "'";
            }

            l_Query += " WHERE ID = " + this.ID;

            return RunInTransaction(l_Query);
        }

        public Result Delete()
        {
            return RunInTransaction("DELETE FROM [" + m_TableName + "] WHERE ID = " + this.ID);
        }

        private Result RunInTransaction(string p_Query)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                bool l_Process = this.Connection.Execute(p_Query);

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
    }
}
