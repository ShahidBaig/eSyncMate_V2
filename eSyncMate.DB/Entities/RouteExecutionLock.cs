using System;
using System.Data;
using System.Data.SqlClient;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// Lightweight database lock for cross-process route execution coordination.
    /// Prevents differential feed from running while full feed is executing for the same customer.
    /// </summary>
    public class RouteExecutionLock
    {
        private DBConnector _connection;

        public RouteExecutionLock() { }

        public void UseConnection(string connectionString)
        {
            _connection = new DBConnector(connectionString);
        }

        /// <summary>
        /// Attempts to acquire a lock for the given customer and route type.
        /// Returns the lock token (GUID) on success, or null if a lock already exists.
        /// Uses atomic INSERT ... WHERE NOT EXISTS with UPDLOCK to prevent race conditions.
        /// </summary>
        public string AcquireLock(string customerID, int routeTypeId, int routeId)
        {
            // Safety check: ensure table exists on client DB
            if (!EnsureTableExists()) return null;

            string lockToken = Guid.NewGuid().ToString();
            string machineName = Environment.MachineName;

            string query = @"
                INSERT INTO [RouteExecutionLock] ([CustomerID], [RouteTypeId], [RouteId], [LockToken], [AcquiredAt], [MachineName], [IsActive])
                SELECT @CustomerID, @RouteTypeId, @RouteId, @LockToken, GETDATE(), @MachineName, 1
                WHERE NOT EXISTS (
                    SELECT 1 FROM [RouteExecutionLock] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [CustomerID] = @CustomerID
                      AND [RouteTypeId] = @RouteTypeId
                      AND [IsActive] = 1
                      AND DATEDIFF(MINUTE, [AcquiredAt], GETDATE()) <= 60
                )";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CustomerID", SqlDbType.VarChar, 50) { Value = customerID },
                new SqlParameter("@RouteTypeId", SqlDbType.Int) { Value = routeTypeId },
                new SqlParameter("@RouteId", SqlDbType.Int) { Value = routeId },
                new SqlParameter("@LockToken", SqlDbType.VarChar, 50) { Value = lockToken },
                new SqlParameter("@MachineName", SqlDbType.VarChar, 100) { Value = machineName }
            };

            _connection.Execute(query, p_SQLParams: parameters);

            // Check if the row was actually inserted by querying for our token
            // Note: ExecuteScalar doesn't support SqlParameter, so use sanitized literal
            string safeToken = lockToken.Replace("'", "''");
            string checkQuery = $"SELECT COUNT(1) FROM [RouteExecutionLock] WHERE [LockToken] = '{safeToken}' AND [IsActive] = 1";
            object checkResult = _connection.ExecuteScalar(checkQuery);
            int count = Convert.ToInt32(checkResult ?? 0);

            return count > 0 ? lockToken : null;
        }

        /// <summary>
        /// Releases a lock by setting IsActive = 0, matched by lock token.
        /// Only the process that acquired the lock can release it.
        /// </summary>
        public bool ReleaseLock(string lockToken)
        {
            string query = "UPDATE [RouteExecutionLock] SET [IsActive] = 0 WHERE [LockToken] = @LockToken AND [IsActive] = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@LockToken", SqlDbType.VarChar, 50) { Value = lockToken }
            };

            return _connection.Execute(query, p_SQLParams: parameters);
        }

        /// <summary>
        /// Checks whether an active lock exists for the given customer and route type.
        /// Used by Differential Feed to check if Full Feed is running.
        /// Only considers locks within the 2-hour timeout window.
        /// </summary>
        public bool IsLocked(string customerID, int routeTypeId)
        {
            if (!EnsureTableExists()) return false;

            string safeCustomerID = (customerID ?? "").Replace("'", "''");
            string query = $@"
                SELECT COUNT(1) FROM [RouteExecutionLock] WITH (NOLOCK)
                WHERE [CustomerID] = '{safeCustomerID}'
                  AND [RouteTypeId] = {routeTypeId}
                  AND [IsActive] = 1
                  AND DATEDIFF(MINUTE, [AcquiredAt], GETDATE()) <= 60";

            object result = _connection.ExecuteScalar(query);
            int count = Convert.ToInt32(result ?? 0);
            return count > 0;
        }

        /// <summary>
        /// Deactivates all locks older than the specified timeout (default 2 hours).
        /// Called by Hangfire cleanup job to handle crashed/orphaned processes.
        /// Returns the number of stale locks cleaned.
        /// </summary>
        public int CleanStaleLocks(int timeoutMinutes = 60)
        {
            if (!EnsureTableExists()) return 0;

            string query = $@"
                UPDATE [RouteExecutionLock]
                SET [IsActive] = 0
                WHERE [IsActive] = 1
                  AND DATEDIFF(MINUTE, [AcquiredAt], GETDATE()) > {timeoutMinutes};
                SELECT @@ROWCOUNT;";

            object result = _connection.ExecuteScalar(query);
            return Convert.ToInt32(result ?? 0);
        }

        /// <summary>
        /// Verifies RouteExecutionLock table exists. If missing, auto-creates it.
        /// Prevents silent failures on client deployments where migration was not run.
        /// </summary>
        private bool EnsureTableExists()
        {
            try
            {
                string checkQuery = @"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RouteExecutionLock')
                    BEGIN
                        CREATE TABLE [RouteExecutionLock] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [CustomerID] VARCHAR(50) NOT NULL,
                            [RouteTypeId] INT NOT NULL,
                            [RouteId] INT NOT NULL,
                            [LockToken] VARCHAR(50) NOT NULL,
                            [AcquiredAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [MachineName] VARCHAR(100) NULL,
                            [IsActive] BIT NOT NULL DEFAULT 1
                        );
                        CREATE INDEX IX_RouteExecutionLock_Active ON [RouteExecutionLock] ([CustomerID], [RouteTypeId], [IsActive]) WHERE [IsActive] = 1;
                    END
                    SELECT 1;";

                _connection.ExecuteScalar(checkQuery);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
