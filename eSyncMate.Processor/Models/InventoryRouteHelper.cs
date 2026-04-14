using System.Collections.Generic;

namespace eSyncMate.Processor.Models
{
    /// <summary>
    /// Identifies inventory route types for Flow-based abort coordination.
    /// Only these route types participate in the abort mechanism.
    /// </summary>
    public static class InventoryRouteHelper
    {
        private static readonly HashSet<int> _inventoryUploadTypes = new HashSet<int>
        {
            (int)RouteTypesEnum.InventoryFeed,                    // 1
            (int)RouteTypesEnum.SCSFullInventoryFeed,             // 7
            (int)RouteTypesEnum.SCSDifferentialInventoryFeed,     // 8
            (int)RouteTypesEnum.SCSUpdateInventory,               // 19
            (int)RouteTypesEnum.WalmartUploadInventory,           // 27
            (int)RouteTypesEnum.MacysInventoryUpload,             // 33
            (int)RouteTypesEnum.TargetPlusInventoryFeedWHSWise,   // 38
            (int)RouteTypesEnum.LowesInventoryUpload,             // 43
            (int)RouteTypesEnum.AmazonInventoryUpload,            // 48
            (int)RouteTypesEnum.KnotInventoryUpload,              // 52
            (int)RouteTypesEnum.MichealInventoryUpload,           // 57
            (int)RouteTypesEnum.AmazonWHSWInventoryUpload,        // 62
            (int)RouteTypesEnum.LowesWHSWInventoryUpload,         // 64
            (int)RouteTypesEnum.KnotWHSWInventoryUpload,          // 68
        };

        /// <summary>
        /// Full Feed type IDs — these have priority and can abort other inventory routes.
        /// </summary>
        private static readonly HashSet<int> _fullFeedTypes = new HashSet<int>
        {
            (int)RouteTypesEnum.SCSFullInventoryFeed,             // 7
        };

        /// <summary>
        /// Marketplace upload route types (Walmart, Amazon, Lowes, Macys, Knot, Michaels, Target).
        /// These must wait for a running Differential to finish before executing.
        /// </summary>
        private static readonly HashSet<int> _uploadRouteTypes = new HashSet<int>
        {
            (int)RouteTypesEnum.WalmartUploadInventory,           // 27
            (int)RouteTypesEnum.MacysInventoryUpload,             // 33
            (int)RouteTypesEnum.TargetPlusInventoryFeedWHSWise,   // 38
            (int)RouteTypesEnum.LowesInventoryUpload,             // 43
            (int)RouteTypesEnum.AmazonInventoryUpload,            // 48
            (int)RouteTypesEnum.KnotInventoryUpload,              // 52
            (int)RouteTypesEnum.MichealInventoryUpload,           // 57
            (int)RouteTypesEnum.AmazonWHSWInventoryUpload,        // 62
            (int)RouteTypesEnum.LowesWHSWInventoryUpload,         // 64
            (int)RouteTypesEnum.KnotWHSWInventoryUpload,          // 68
        };

        public static bool IsInventoryRoute(int routeTypeId) => _inventoryUploadTypes.Contains(routeTypeId);

        public static bool IsFullFeedRoute(int routeTypeId) => _fullFeedTypes.Contains(routeTypeId);

        public static bool IsUploadRoute(int routeTypeId) => _uploadRouteTypes.Contains(routeTypeId);

        /// <summary>
        /// Returns just the Differential type IDs (used by upload routes to check if a diff is running).
        /// </summary>
        public static int[] GetDifferentialTypeIds() => new int[]
        {
            (int)RouteTypesEnum.SCSDifferentialInventoryFeed,    // 8
        };

        /// <summary>
        /// Returns just the Full Feed type IDs.
        /// </summary>
        public static int[] GetFullFeedTypeIds() => new int[]
        {
            (int)RouteTypesEnum.SCSFullInventoryFeed,            // 7
        };

        public static int[] GetInventoryTypeIds() => new int[]
        {
            1, 7, 8, 19, 27, 33, 38, 43, 48, 52, 57, 62, 64, 68
        };

    }
}
