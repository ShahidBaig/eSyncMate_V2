using eSyncMate.Processor.Helpers;
using eSyncMate.Processor.Models;
using System.Data;
using System.Data.SqlClient;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// An access token an external platform handed over when an account (for Shopify: a store) authorized our app.
    /// It is NOT tied to a customer when it arrives: the user picks it on the customer (Customers > Edit > Tokens),
    /// which saves its Id in Customers.IntegrationTokenId. The customer's routes then use that token.
    /// </summary>
    public class IntegrationToken
    {
        public int Id { get; set; }
        public string Platform { get; set; } = string.Empty;            // Shopify (TikTok, ... later)
        public string Name { get; set; } = string.Empty;                // shown in the Tokens list
        public string AccountKey { get; set; } = string.Empty;          // Shopify: <store>.myshopify.com
        public string ClientId { get; set; } = string.Empty;            // the app
        public string AccessToken { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public DateTime? AccessTokenExpiryUtc { get; set; }             // null = does not expire
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpiryUtc { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    /// <summary>
    /// Table IntegrationTokens (Task 00006). Tokens are stored encrypted (ENC:...) with the same key as the
    /// connectors. A database without the table / column behaves as before: no token is found.
    /// </summary>
    public static class IntegrationTokenStore
    {
        public const string PlatformShopify = "Shopify";

        private const int InvalidObjectName = 208;
        private const int InvalidColumnName = 207;

        private const string Columns = "T.Id, T.Platform, T.Name, T.AccountKey, T.ClientId, T.AccessToken, T.Scope, T.AccessTokenExpiryUtc, " +
                                       "T.RefreshToken, T.RefreshTokenExpiryUtc, T.ModifiedDate";

        private static bool IsNotInstalled(SqlException ex) => ex.Number == InvalidObjectName || ex.Number == InvalidColumnName;

        /// <summary>The token selected on the customer (Customers.IntegrationTokenId), or null.</summary>
        public static IntegrationToken? GetForCustomer(string erpCustomerId, string platform)
        {
            if (string.IsNullOrWhiteSpace(erpCustomerId))
                return null;

            return GetOne("SELECT " + Columns + @"
                             FROM dbo.Customers C WITH (NOLOCK)
                                  INNER JOIN dbo.IntegrationTokens T WITH (NOLOCK) ON T.Id = C.IntegrationTokenId
                            WHERE C.ERPCustomerID = @P1 AND T.Platform = @P2", erpCustomerId, platform);
        }

        /// <summary>The token of an account (store) + app, whoever it is linked to.</summary>
        public static IntegrationToken? GetByAccount(string platform, string accountKey, string clientId)
        {
            return GetOne("SELECT " + Columns + @"
                             FROM dbo.IntegrationTokens T WITH (NOLOCK)
                            WHERE T.Platform = @P1 AND T.AccountKey = @P2 AND T.ClientId = @P3", platform, accountKey, clientId);
        }

        private static IntegrationToken? GetOne(string query, params string[] values)
        {
            try
            {
                using SqlConnection l_Connection = new SqlConnection(CommonUtils.ConnectionString);
                using SqlCommand l_Command = new SqlCommand(query, l_Connection);

                for (int i = 0; i < values.Length; i++)
                    l_Command.Parameters.AddWithValue("@P" + (i + 1), values[i] ?? string.Empty);

                l_Connection.Open();

                using SqlDataReader l_Reader = l_Command.ExecuteReader();

                return l_Reader.Read() ? Read(l_Reader) : null;
            }
            catch (SqlException ex) when (IsNotInstalled(ex))
            {
                // V2_02 not run on this database yet
                return null;
            }
        }

        private static IntegrationToken Read(SqlDataReader reader)
        {
            return new IntegrationToken
            {
                Id = Convert.ToInt32(reader["Id"]),
                Platform = Convert.ToString(reader["Platform"]) ?? string.Empty,
                Name = Convert.ToString(reader["Name"]) ?? string.Empty,
                AccountKey = Convert.ToString(reader["AccountKey"]) ?? string.Empty,
                ClientId = Convert.ToString(reader["ClientId"]) ?? string.Empty,
                AccessToken = EncryptionHelper.Decrypt(Convert.ToString(reader["AccessToken"]) ?? string.Empty, CommonUtils.EncryptionKey),
                Scope = Convert.ToString(reader["Scope"]) ?? string.Empty,
                AccessTokenExpiryUtc = reader["AccessTokenExpiryUtc"] as DateTime?,
                RefreshToken = EncryptionHelper.Decrypt(Convert.ToString(reader["RefreshToken"]) ?? string.Empty, CommonUtils.EncryptionKey),
                RefreshTokenExpiryUtc = reader["RefreshTokenExpiryUtc"] as DateTime?,
                ModifiedDate = Convert.ToDateTime(reader["ModifiedDate"])
            };
        }

        /// <summary>
        /// Insert, or update the row of the same platform + account + app (a store that authorizes again keeps its
        /// Id, so the customers linked to it stay linked). Returns the Id.
        /// </summary>
        public static int Save(IntegrationToken token)
        {
            const string l_Query = @"DECLARE @Id INT = (SELECT Id FROM dbo.IntegrationTokens WHERE Platform = @Platform AND AccountKey = @AccountKey AND ClientId = @ClientId);

                                     IF @Id IS NULL
                                     BEGIN
                                         INSERT INTO dbo.IntegrationTokens
                                                (Platform, Name, AccountKey, ClientId, AccessToken, Scope, AccessTokenExpiryUtc, RefreshToken, RefreshTokenExpiryUtc, CreatedDate, ModifiedDate)
                                         VALUES (@Platform, @Name, @AccountKey, @ClientId, @AccessToken, @Scope, @AccessTokenExpiryUtc, @RefreshToken, @RefreshTokenExpiryUtc, GETDATE(), GETDATE());

                                         SET @Id = SCOPE_IDENTITY();
                                     END
                                     ELSE
                                         UPDATE dbo.IntegrationTokens
                                            SET AccessToken = @AccessToken, Scope = @Scope, AccessTokenExpiryUtc = @AccessTokenExpiryUtc,
                                                RefreshToken = @RefreshToken, RefreshTokenExpiryUtc = @RefreshTokenExpiryUtc, ModifiedDate = GETDATE()
                                          WHERE Id = @Id;

                                     SELECT @Id;";

            using SqlConnection l_Connection = new SqlConnection(CommonUtils.ConnectionString);
            using SqlCommand l_Command = new SqlCommand(l_Query, l_Connection);

            l_Command.Parameters.AddWithValue("@Platform", token.Platform);
            l_Command.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(token.Name) ? token.Platform + " - " + token.AccountKey : token.Name);
            l_Command.Parameters.AddWithValue("@AccountKey", token.AccountKey);
            l_Command.Parameters.AddWithValue("@ClientId", token.ClientId);
            l_Command.Parameters.AddWithValue("@AccessToken", EncryptionHelper.Encrypt(token.AccessToken, CommonUtils.EncryptionKey));
            l_Command.Parameters.AddWithValue("@Scope", token.Scope ?? string.Empty);
            l_Command.Parameters.AddWithValue("@AccessTokenExpiryUtc", (object?)token.AccessTokenExpiryUtc ?? DBNull.Value);
            l_Command.Parameters.AddWithValue("@RefreshToken", string.IsNullOrEmpty(token.RefreshToken)
                ? DBNull.Value : EncryptionHelper.Encrypt(token.RefreshToken, CommonUtils.EncryptionKey));
            l_Command.Parameters.AddWithValue("@RefreshTokenExpiryUtc", (object?)token.RefreshTokenExpiryUtc ?? DBNull.Value);

            l_Connection.Open();

            token.Id = Convert.ToInt32(l_Command.ExecuteScalar());

            return token.Id;
        }

        /// <summary>
        /// Rows for the Tokens list of the customer screen: no token value, plus the customers that use each one.
        /// </summary>
        public static DataTable List()
        {
            const string l_Query = @"SELECT T.Id, T.Platform, T.Name, T.AccountKey, T.Scope, T.AccessTokenExpiryUtc, T.CreatedDate, T.ModifiedDate,
                                            ISNULL(STUFF((SELECT ', ' + C.ERPCustomerID
                                                            FROM dbo.Customers C WITH (NOLOCK)
                                                           WHERE C.IntegrationTokenId = T.Id
                                                           ORDER BY C.ERPCustomerID
                                                             FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), '') AS LinkedCustomers
                                       FROM dbo.IntegrationTokens T WITH (NOLOCK)
                                      ORDER BY T.Platform, T.Name";

            DataTable l_Data = new DataTable();

            try
            {
                using SqlConnection l_Connection = new SqlConnection(CommonUtils.ConnectionString);
                using SqlDataAdapter l_Adapter = new SqlDataAdapter(l_Query, l_Connection);

                l_Adapter.Fill(l_Data);
            }
            catch (SqlException ex) when (IsNotInstalled(ex))
            {
                // V2_02 not run on this database yet: an empty list
            }

            return l_Data;
        }

        /// <summary>Id of the token selected on the customer, 0 = none.</summary>
        public static int GetCustomerTokenId(int customerId)
        {
            try
            {
                using SqlConnection l_Connection = new SqlConnection(CommonUtils.ConnectionString);
                using SqlCommand l_Command = new SqlCommand("SELECT ISNULL(IntegrationTokenId, 0) FROM dbo.Customers WITH (NOLOCK) WHERE Id = @Id", l_Connection);

                l_Command.Parameters.AddWithValue("@Id", customerId);
                l_Connection.Open();

                object? l_Value = l_Command.ExecuteScalar();

                return l_Value == null || l_Value == DBNull.Value ? 0 : Convert.ToInt32(l_Value);
            }
            catch (SqlException ex) when (IsNotInstalled(ex))
            {
                return 0;
            }
        }

        /// <summary>Links a token to the customer; tokenId 0 removes the link. False = customer or token not found.</summary>
        public static bool SetCustomerToken(int customerId, int tokenId)
        {
            const string l_Query = @"UPDATE C
                                        SET C.IntegrationTokenId = NULLIF(@TokenId, 0)
                                       FROM dbo.Customers C
                                      WHERE C.Id = @CustomerId
                                        AND (@TokenId = 0 OR EXISTS (SELECT 1 FROM dbo.IntegrationTokens T WHERE T.Id = @TokenId));";

            using SqlConnection l_Connection = new SqlConnection(CommonUtils.ConnectionString);
            using SqlCommand l_Command = new SqlCommand(l_Query, l_Connection);

            l_Command.Parameters.AddWithValue("@CustomerId", customerId);
            l_Command.Parameters.AddWithValue("@TokenId", tokenId);

            l_Connection.Open();

            return l_Command.ExecuteNonQuery() > 0;
        }
    }
}
