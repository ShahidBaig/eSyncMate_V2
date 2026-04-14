using System;
using System.Data;

namespace eSyncMate.DB.Entities
{
    /// <summary>
    /// Flow-based inventory route coordination.
    /// Checks if other inventory routes in the same Flow are running.
    /// Used by RouteEngine to skip or wait before starting an inventory route.
    /// </summary>
    public class RouteAbortFlag
    {
        private DBConnector _connection;

        public void UseConnection(string connectionString)
        {
            _connection = new DBConnector(connectionString);
        }

        /// <summary>
        /// Get the FlowId for a given RouteId from FlowDetails table.
        /// Returns 0 if route is not part of any flow.
        /// Uses SP: Sp_GetFlowIdForRoute
        /// </summary>
        public long GetFlowIdForRoute(int routeId)
        {
            try
            {
                DataTable dt = new DataTable();
                _connection.GetDataSP($"Sp_GetFlowIdForRoute @RouteId={routeId}", ref dt);
                return dt.Rows.Count > 0 ? Convert.ToInt64(dt.Rows[0]["FlowId"]) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if any OTHER inventory route in the same Flow is currently running.
        /// Joins RouteExecutionLock + FlowDetails + Routes to find active locks
        /// for inventory routes in the same Flow (excluding the current route).
        /// Uses SP: Sp_IsOtherFlowInventoryRouteRunning
        /// </summary>
        public bool IsOtherFlowInventoryRouteRunning(long flowId, int excludeRouteId, int[] inventoryTypeIds)
        {
            try
            {
                string typeIdList = string.Join(",", inventoryTypeIds);
                DataTable dt = new DataTable();
                _connection.GetDataSP($"Sp_IsOtherFlowInventoryRouteRunning @FlowId={flowId}, @ExcludeRouteId={excludeRouteId}, @InventoryTypeIds='{typeIdList}'", ref dt);
                return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["RunningCount"]) > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
