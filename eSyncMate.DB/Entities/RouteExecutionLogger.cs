using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// Central log for proving Hangfire job distribution + failover across multiple servers.
    /// Inserts a row when a route starts and updates the same row when it ends.
    /// Fire-and-forget: failures here are swallowed to never disrupt route execution.
    /// </summary>
    public static class RouteExecutionLogger
    {
        /// <summary>
        /// Insert a 'Started' row. Returns the new LogID for the matching LogEnd call.
        /// Returns 0 if logging itself failed (silent — never throws to caller).
        /// </summary>
        public static long LogStart(string connectionString, int routeId, string routeName,
                                    int? routeTypeId, string executionSource)
        {
            try
            {
                const string sql = @"
                    INSERT INTO [dbo].[RouteExecutionLog]
                        ([RouteID], [RouteName], [RouteTypeID], [MachineName],
                         [ProcessID], [ExecutionSource], [StartTime], [Status])
                    OUTPUT INSERTED.[LogID]
                    VALUES
                        (@RouteID, @RouteName, @RouteTypeID, @MachineName,
                         @ProcessID, @ExecutionSource, SYSUTCDATETIME(), 'Started');";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RouteID", routeId);
                    cmd.Parameters.AddWithValue("@RouteName", (object)routeName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RouteTypeID",
                        routeTypeId.HasValue ? (object)routeTypeId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@ProcessID", Process.GetCurrentProcess().Id);
                    cmd.Parameters.AddWithValue("@ExecutionSource",
                        (object)executionSource ?? DBNull.Value);

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null ? 0 : Convert.ToInt64(result);
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Update the row previously created by LogStart with end time, duration, status.
        /// status: 'Completed' or 'Error'. errorMessage optional.
        /// Fire-and-forget: failures swallowed.
        /// </summary>
        public static void LogEnd(string connectionString, long logId, string status,
                                  string errorMessage = null)
        {
            if (logId <= 0) return;

            try
            {
                const string sql = @"
                    UPDATE [dbo].[RouteExecutionLog]
                    SET    [EndTime]      = SYSUTCDATETIME(),
                           [DurationMs]   = DATEDIFF(MILLISECOND, [StartTime], SYSUTCDATETIME()),
                           [Status]       = @Status,
                           [ErrorMessage] = @ErrorMessage
                    WHERE  [LogID] = @LogID;";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    cmd.Parameters.AddWithValue("@Status", status ?? "Completed");
                    cmd.Parameters.AddWithValue("@ErrorMessage",
                        string.IsNullOrEmpty(errorMessage) ? (object)DBNull.Value : errorMessage);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // swallow — logging failures must never disrupt the route
            }
        }
    }
}
