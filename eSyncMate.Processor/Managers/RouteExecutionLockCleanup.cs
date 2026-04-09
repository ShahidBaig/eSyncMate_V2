using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Hangfire job to clean stale route execution locks.
    /// Runs hourly to deactivate locks older than 2 hours (crashed/orphaned processes).
    /// </summary>
    public static class RouteExecutionLockCleanup
    {
        public static void CleanStaleLocks()
        {
            try
            {
                var lockEntity = new RouteExecutionLock();
                lockEntity.UseConnection(CommonUtils.ConnectionString);
                int cleaned = lockEntity.CleanStaleLocks(60);
                if (cleaned > 0)
                {
                    Console.WriteLine($"[RouteExecutionLockCleanup] Cleaned {cleaned} stale lock(s)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RouteExecutionLockCleanup] Error: {ex.Message}");
            }
        }
    }
}
