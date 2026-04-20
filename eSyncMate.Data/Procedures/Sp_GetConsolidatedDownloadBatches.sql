ALTER PROCEDURE [dbo].[Sp_GetConsolidatedDownloadBatches]
	@p_UploadBatchID	NVARCHAR(500),
	@p_ItemID			NVARCHAR(100)	= NULL
AS
BEGIN
	DECLARE	@l_UploadBatchID		NVARCHAR(500)	= ISNULL(@p_UploadBatchID, ''),
			@l_ItemID				NVARCHAR(100)	= ISNULL(@p_ItemID, ''),
			@l_CustomerID			VARCHAR(500),
			@l_CurrentUploadDate	DATETIME,
			@l_PreviousUploadDate	DATETIME

	BEGIN TRY
		-- Step 1: Current upload batch ki StartDate + CustomerID nikalo
		SELECT	@l_CustomerID			= CustomerID,
				@l_CurrentUploadDate	= ISNULL(StartDate, FinishDate)
		FROM	InventoryBatchWise WITH (NOLOCK)
		WHERE	BatchID = @l_UploadBatchID
			AND Status <> 'DELETED'

		-- Step 2: Same customer ka previous upload dhoondo (current se pehle)
		SELECT TOP 1 @l_PreviousUploadDate = ISNULL(StartDate, FinishDate)
		FROM	InventoryBatchWise WITH (NOLOCK)
		WHERE	CustomerID = @l_CustomerID
			AND Status <> 'DELETED'
			AND BatchID <> @l_UploadBatchID
			AND RouteType IN (
				'WalmartUploadInventory', 'TargetPlusInventoryFeedWHSWise',
				'AmazonInventoryUpload', 'LowesWHSWInventoryUpload',
				'MacysInventoryUpload', 'MichealInventoryUpload',
				'KnotInventoryUpload', 'KnotWHSWInventoryUpload',
				'AmazonWHSWInventoryUpload'
			)
			AND ISNULL(StartDate, FinishDate) < @l_CurrentUploadDate
		ORDER BY ISNULL(StartDate, FinishDate) DESC

		-- Step 3: Download batches ko temp table may filter karo (once)
		IF OBJECT_ID('tempdb..#Downloads') IS NOT NULL DROP TABLE #Downloads
		CREATE TABLE #Downloads (
			BatchID		NVARCHAR(500),
			ItemCount	INT,
			StartDate	DATETIME,
			FinishDate	DATETIME,
			Status		VARCHAR(100),
			RouteType	VARCHAR(500)
		)

		INSERT INTO #Downloads (BatchID, ItemCount, StartDate, FinishDate, Status, RouteType)
		SELECT	IBW.BatchID, IBW.ItemCount, IBW.StartDate, IBW.FinishDate, IBW.Status, IBW.RouteType
		FROM	InventoryBatchWise IBW WITH (NOLOCK)
		WHERE	IBW.CustomerID = @l_CustomerID
			AND IBW.Status <> 'DELETED'
			AND IBW.RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
			AND ISNULL(IBW.StartDate, IBW.FinishDate) < @l_CurrentUploadDate
			-- If previous upload exists, lower-bound the range; else no lower bound
			AND (@l_PreviousUploadDate IS NULL
				 OR ISNULL(IBW.StartDate, IBW.FinishDate) > @l_PreviousUploadDate)
			-- Archive guard: batch must have live rows in SCSInventoryFeedData
			-- If ItemID filter provided, only include batches that actually contain that item
			AND EXISTS (
				SELECT 1
				FROM	SCSInventoryFeedData D WITH (NOLOCK)
				WHERE	D.BatchID = IBW.BatchID
					AND (@l_ItemID = '' OR D.ItemId LIKE '%' + @l_ItemID + '%')
			)

		-- Result Set 1: Consolidated single row (HAVING skips empty case automatically)
		SELECT	@l_CustomerID						AS CustomerID,
				MIN(StartDate)						AS StartDate,
				MAX(FinishDate)						AS FinishDate,
				'Completed'							AS Status,
				SUM(ISNULL(ItemCount, 0))			AS ItemCount,
				COUNT(*)							AS MergedCount,
				STRING_AGG(BatchID, ',')			AS BatchIDs,
				CASE
					WHEN COUNT(DISTINCT RouteType) = 1 THEN MAX(RouteType)
					ELSE 'Latest Inventory Snapshot (consolidated from '
						 + CAST(COUNT(*) AS VARCHAR) + ' feeds)'
				END									AS RouteType,
				@l_PreviousUploadDate				AS PreviousUploadDate
		FROM	#Downloads
		HAVING	COUNT(*) > 0

		-- Result Set 2: Per-type breakdown (for UI chips)
		SELECT	RouteType							AS OrignalRouteType,
				CASE RouteType
					WHEN 'SCSFullInventoryFeed'			THEN 'Full Inventory Feed Received'
					WHEN 'SCSDifferentialInventoryFeed'	THEN 'Differential Inventory Feed Received'
					ELSE RouteType
				END									AS DisplayName,
				COUNT(*)							AS BatchCount,
				SUM(ISNULL(ItemCount, 0))			AS ItemCount
		FROM	#Downloads
		GROUP BY RouteType

		IF OBJECT_ID('tempdb..#Downloads') IS NOT NULL DROP TABLE #Downloads
	END TRY
	BEGIN CATCH
		DECLARE @ErrorMessage NVARCHAR(MAX);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		PRINT 'Error Message: ' + @ErrorMessage;
		THROW;
	END CATCH;
END
