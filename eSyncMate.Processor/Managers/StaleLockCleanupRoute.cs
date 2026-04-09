using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Microsoft.Extensions.Configuration;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Route manager for cleaning stale RouteExecutionLock entries.
    /// Runs as a scheduled route (TypeId=600) via Hangfire like all other routes.
    /// Deactivates locks older than 60 minutes (crashed/orphaned processes).
    /// </summary>
    public class StaleLockCleanupRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;

            try
            {
                route.SaveLog(Declarations.LogTypeEnum.RouteInfo, $"[StaleLockCleanup] Started cleanup for stale route execution locks", string.Empty, userNo);

                var lockEntity = new RouteExecutionLock();
                lockEntity.UseConnection(CommonUtils.ConnectionString);

                int cleaned = lockEntity.CleanStaleLocks(60);

                if (cleaned > 0)
                {
                    route.SaveLog(Declarations.LogTypeEnum.RouteInfo, $"[StaleLockCleanup] Cleaned {cleaned} stale lock(s) older than 60 minutes", string.Empty, userNo);
                }
                else
                {
                    route.SaveLog(Declarations.LogTypeEnum.RouteInfo, $"[StaleLockCleanup] No stale locks found", string.Empty, userNo);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(Declarations.LogTypeEnum.Exception, $"[StaleLockCleanup] Error during cleanup", ex.ToString(), userNo);
            }
        }
    }
}
