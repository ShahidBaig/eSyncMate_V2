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
    public class Customers : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ERPCustomerID { get; set; }
        public string ISACustomerID { get; set; }
        public string ISA856ReceiverId { get; set; }
        public string ISA810ReceiverId { get; set; }
        public string Marketplace { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public List<CustomerMaps> Maps { get; set; }
        public List<CustomerConnectors> Connectors { get; set; }

        // ── Target Plus OAuth (declared AFTER ModifiedBy/Maps/Connectors so the
        //    ordinal ORM excludes them; managed via the dedicated OAuth methods below) ──
        public bool UseNewAuthentication { get; set; }
        public string OAuthClientId { get; set; }
        public string OAuthClientSecret { get; set; }
        public string OAuthAuthUrl { get; set; }
        public string OAuthTokenUrl { get; set; }
        public string OAuthRefreshToken { get; set; }
        public DateTime? OAuthRefreshTokenExpiry { get; set; }
        public string OAuthAccessToken { get; set; }
        public DateTime? OAuthAccessTokenExpiry { get; set; }
        public DateTime? OAuthTokenUpdatedDate { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Customers() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Customers(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Customers(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(Customers.TableName))
            {
                Customers.TableName = "Customers";
            }

            if (string.IsNullOrEmpty(Customers.ViewName))
            {
                Customers.ViewName = "VW_Customers";
            }

            if (string.IsNullOrEmpty(Customers.PrimaryKeyName))
            {
                Customers.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(Customers.EndingPropertyName))
            {
                Customers.EndingPropertyName = "CreatedBy";
            }

            if (Customers.DBProperties == null)
            {
                Customers.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(Customers.InsertQueryStart))
            {
                Customers.InsertQueryStart = PrepareQueries(this, Customers.TableName, Customers.EndingPropertyName, ref l_Query, Customers.DBProperties);
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
                l_Query = "SELECT * FROM [" + Customers.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Customers.TableName + "]";
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
                l_Query = "SELECT * FROM [" + Customers.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Customers.ViewName + "]";
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

        public bool GetCustomersData(ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = "SELECT ID,NAME FROM Customers WITH (NOLOCK)";

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetListPaged(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;
            string l_CountQuery = "SELECT COUNT(*) FROM [" + Customers.TableName + "]";
            if (!string.IsNullOrEmpty(p_Criteria)) l_CountQuery += " WHERE " + p_Criteria;
            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0) totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [" + Customers.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + Customers.TableName + "]";
            if (!string.IsNullOrEmpty(p_Criteria)) l_Query += " WHERE " + p_Criteria;
            l_Query += !string.IsNullOrEmpty(p_OrderBy) ? " ORDER BY " + p_OrderBy : " ORDER BY Id DESC";
            int offset = (pageNumber - 1) * pageSize;
            l_Query += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetViewListPaged(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy, int pageNumber, int pageSize, out int totalCount)
        {
            totalCount = 0;
            string l_CountQuery = "SELECT COUNT(*) FROM [" + Customers.ViewName + "]";
            if (!string.IsNullOrEmpty(p_Criteria)) l_CountQuery += " WHERE " + p_Criteria;
            var l_CountData = new DataTable();
            Connection.GetData(l_CountQuery, ref l_CountData);
            if (l_CountData.Rows.Count > 0) totalCount = Convert.ToInt32(l_CountData.Rows[0][0]);
            l_CountData.Dispose();

            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [" + Customers.ViewName + "]"
                : "SELECT " + p_Fields + " FROM [" + Customers.ViewName + "]";
            if (!string.IsNullOrEmpty(p_Criteria)) l_Query += " WHERE " + p_Criteria;
            l_Query += !string.IsNullOrEmpty(p_OrderBy) ? " ORDER BY " + p_OrderBy : " ORDER BY Id DESC";
            int offset = (pageNumber - 1) * pageSize;
            l_Query += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(Customers.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Customers.ViewName, Customers.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + Customers.PrimaryKeyName + ", '0'))) FROM " + Customers.TableName, ref l_Data))
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

            PopulateObject(this, l_Data, DBProperties, Customers.EndingPropertyName);

            l_Data.Dispose();

            if (isOnlyObject)
            {
                return Result.GetSuccessResult();
            }

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
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Customers.ViewName, Customers.PrimaryKeyName));
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly(int p_PrimaryKey)
        {
            SetProperty(Customers.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Customers.ViewName, Customers.PrimaryKeyName), true);
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Customers.ViewName, Customers.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            l_Property.FirstOrDefault<PropertyInfo>()?.SetValue(this, propertyValue);

            return GetObjectFromQuery(PrepareGetObjectQuery(this, Customers.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, Customers.InsertQueryStart, Customers.EndingPropertyName, Customers.DBProperties);

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
                Customers.DBProperties = this.GetType().GetProperties()
                 .Where(prop => prop.Name != "CreatedBy" && prop.Name != "CreatedDate")
                 .ToList();

                Customers.EndingPropertyName = "ModifiedBy";

                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, Customers.TableName, Customers.PrimaryKeyName, Customers.EndingPropertyName, Customers.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, Customers.TableName, Customers.PrimaryKeyName);

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

        #region Target Plus OAuth (dedicated direct-SQL — bypasses the ordinal ORM)

        // SQL literal helpers (encrypted tokens are base64 = safe, but escape anyway).
        private static string SqlStr(string p_Value)
        {
            return p_Value == null ? "NULL" : "N'" + p_Value.Replace("'", "''") + "'";
        }

        private static string SqlDate(DateTime? p_Value)
        {
            return p_Value.HasValue ? "'" + p_Value.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL";
        }

        /// <summary>
        /// Load the OAuth columns (flag, credentials, tokens) for a customer by Id.
        /// Token/secret values are returned AS STORED (encrypted) — decrypt in the caller.
        /// </summary>
        public bool LoadOAuth(int p_Id)
        {
            DataTable l_Data = new DataTable();

            string l_Query =
                "SELECT UseNewAuthentication, OAuthClientId, OAuthClientSecret, OAuthAuthUrl, OAuthTokenUrl, " +
                "OAuthRefreshToken, OAuthRefreshTokenExpiry, OAuthAccessToken, OAuthAccessTokenExpiry, OAuthTokenUpdatedDate " +
                "FROM Customers WITH (NOLOCK) WHERE Id = " + p_Id;

            if (!Connection.GetData(l_Query, ref l_Data) || l_Data.Rows.Count == 0)
            {
                l_Data.Dispose();
                return false;
            }

            DataRow r = l_Data.Rows[0];
            this.Id = p_Id;
            this.UseNewAuthentication = r["UseNewAuthentication"] != DBNull.Value && Convert.ToBoolean(r["UseNewAuthentication"]);
            this.OAuthClientId = r["OAuthClientId"] as string;
            this.OAuthClientSecret = r["OAuthClientSecret"] as string;
            this.OAuthAuthUrl = r["OAuthAuthUrl"] as string;
            this.OAuthTokenUrl = r["OAuthTokenUrl"] as string;
            this.OAuthRefreshToken = r["OAuthRefreshToken"] as string;
            this.OAuthRefreshTokenExpiry = r["OAuthRefreshTokenExpiry"] == DBNull.Value ? (DateTime?)null : DateTime.SpecifyKind(Convert.ToDateTime(r["OAuthRefreshTokenExpiry"]), DateTimeKind.Utc);
            this.OAuthAccessToken = r["OAuthAccessToken"] as string;
            this.OAuthAccessTokenExpiry = r["OAuthAccessTokenExpiry"] == DBNull.Value ? (DateTime?)null : DateTime.SpecifyKind(Convert.ToDateTime(r["OAuthAccessTokenExpiry"]), DateTimeKind.Utc);
            this.OAuthTokenUpdatedDate = r["OAuthTokenUpdatedDate"] == DBNull.Value ? (DateTime?)null : DateTime.SpecifyKind(Convert.ToDateTime(r["OAuthTokenUpdatedDate"]), DateTimeKind.Utc);

            l_Data.Dispose();
            return true;
        }

        /// <summary>
        /// Load the OAuth columns for a customer by ERPCustomerID (resolves and sets Id too).
        /// Token/secret values are returned AS STORED (encrypted) — decrypt in the caller.
        /// </summary>
        public bool LoadOAuthByERP(string p_ERPCustomerID)
        {
            DataTable l_Data = new DataTable();

            string l_Query =
                "SELECT Id, UseNewAuthentication, OAuthClientId, OAuthClientSecret, OAuthAuthUrl, OAuthTokenUrl, " +
                "OAuthRefreshToken, OAuthRefreshTokenExpiry, OAuthAccessToken, OAuthAccessTokenExpiry, OAuthTokenUpdatedDate " +
                "FROM Customers WITH (NOLOCK) WHERE ERPCustomerID = " + SqlStr(p_ERPCustomerID);

            if (!Connection.GetData(l_Query, ref l_Data) || l_Data.Rows.Count == 0)
            {
                l_Data.Dispose();
                return false;
            }

            DataRow r = l_Data.Rows[0];
            this.Id = Convert.ToInt32(r["Id"]);
            this.UseNewAuthentication = r["UseNewAuthentication"] != DBNull.Value && Convert.ToBoolean(r["UseNewAuthentication"]);
            this.OAuthClientId = r["OAuthClientId"] as string;
            this.OAuthClientSecret = r["OAuthClientSecret"] as string;
            this.OAuthAuthUrl = r["OAuthAuthUrl"] as string;
            this.OAuthTokenUrl = r["OAuthTokenUrl"] as string;
            this.OAuthRefreshToken = r["OAuthRefreshToken"] as string;
            this.OAuthRefreshTokenExpiry = r["OAuthRefreshTokenExpiry"] == DBNull.Value ? (DateTime?)null : DateTime.SpecifyKind(Convert.ToDateTime(r["OAuthRefreshTokenExpiry"]), DateTimeKind.Utc);
            this.OAuthAccessToken = r["OAuthAccessToken"] as string;
            this.OAuthAccessTokenExpiry = r["OAuthAccessTokenExpiry"] == DBNull.Value ? (DateTime?)null : DateTime.SpecifyKind(Convert.ToDateTime(r["OAuthAccessTokenExpiry"]), DateTimeKind.Utc);
            this.OAuthTokenUpdatedDate = r["OAuthTokenUpdatedDate"] == DBNull.Value ? (DateTime?)null : DateTime.SpecifyKind(Convert.ToDateTime(r["OAuthTokenUpdatedDate"]), DateTimeKind.Utc);

            l_Data.Dispose();
            return true;
        }

        /// <summary>
        /// Save the full token set — used after first authorize and after every refresh
        /// (refresh token ROTATES, so it is written every time). Pass ENCRYPTED values.
        /// </summary>
        public bool SaveOAuthTokens(int p_Id, string p_RefreshToken, DateTime? p_RefreshExpiry, string p_AccessToken, DateTime? p_AccessExpiry)
        {
            string l_Query =
                "UPDATE Customers SET " +
                "OAuthRefreshToken = " + SqlStr(p_RefreshToken) + ", " +
                "OAuthRefreshTokenExpiry = " + SqlDate(p_RefreshExpiry) + ", " +
                "OAuthAccessToken = " + SqlStr(p_AccessToken) + ", " +
                "OAuthAccessTokenExpiry = " + SqlDate(p_AccessExpiry) + ", " +
                "OAuthTokenUpdatedDate = GETUTCDATE() " +
                "WHERE Id = " + p_Id;

            return Connection.Execute(l_Query);
        }

        /// <summary>
        /// Save only the access token (when it expires but the refresh token was not rotated).
        /// Pass ENCRYPTED value.
        /// </summary>
        public bool SaveOAuthAccessToken(int p_Id, string p_AccessToken, DateTime? p_AccessExpiry)
        {
            string l_Query =
                "UPDATE Customers SET " +
                "OAuthAccessToken = " + SqlStr(p_AccessToken) + ", " +
                "OAuthAccessTokenExpiry = " + SqlDate(p_AccessExpiry) + ", " +
                "OAuthTokenUpdatedDate = GETUTCDATE() " +
                "WHERE Id = " + p_Id;

            return Connection.Execute(l_Query);
        }

        /// <summary>
        /// Save the static OAuth credentials + endpoints (one-time setup). Pass ENCRYPTED secret.
        /// </summary>
        public bool SaveOAuthCredentials(int p_Id, string p_ClientId, string p_ClientSecret, string p_AuthUrl, string p_TokenUrl)
        {
            string l_Query =
                "UPDATE Customers SET " +
                "OAuthClientId = " + SqlStr(p_ClientId) + ", " +
                "OAuthClientSecret = " + SqlStr(p_ClientSecret) + ", " +
                "OAuthAuthUrl = " + SqlStr(p_AuthUrl) + ", " +
                "OAuthTokenUrl = " + SqlStr(p_TokenUrl) + " " +
                "WHERE Id = " + p_Id;

            return Connection.Execute(l_Query);
        }

        /// <summary>
        /// Toggle the new-OAuth flag for a customer (ON = OAuth path, OFF = legacy headers).
        /// </summary>
        public bool SetUseNewAuthentication(int p_Id, bool p_Value)
        {
            string l_Query =
                "UPDATE Customers SET UseNewAuthentication = " + (p_Value ? "1" : "0") +
                " WHERE Id = " + p_Id;

            return Connection.Execute(l_Query);
        }

        #endregion

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
            return ((Customers)x).Id == ((Customers)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion
    }
}
