ALTER PROCEDURE [dbo].[Sp_GetInventoryFeedSummary]
	@p_ItemID		NVARCHAR(100)	= NULL,
	@p_CustomerID	NVARCHAR(MAX)	= NULL,
	@p_StartDate	NVARCHAR(50)	= NULL,
	@p_FinishDate	NVARCHAR(50)	= NULL,
	@p_Status		NVARCHAR(50)	= NULL,
	@p_PageNumber	INT				= 1,
	@p_PageSize		INT				= 10
AS
BEGIN
	DECLARE	@l_ItemID		NVARCHAR(100)	= ISNULL(@p_ItemID, ''),
			@l_CustomerID	NVARCHAR(MAX)	= ISNULL(@p_CustomerID, ''),
			@l_StartDate	NVARCHAR(50)	= ISNULL(@p_StartDate, ''),
			@l_FinishDate	NVARCHAR(50)	= ISNULL(@p_FinishDate, ''),
			@l_Status		NVARCHAR(50)	= ISNULL(@p_Status, ''),
			@l_PageNumber	INT				= @p_PageNumber,
			@l_PageSize		INT				= @p_PageSize,
			@l_Offset		INT,
			@l_Where		NVARCHAR(MAX)	= N'',
			@l_Sql			NVARCHAR(MAX)

	BEGIN TRY
		IF @l_PageNumber IS NULL OR @l_PageNumber < 1 SET @l_PageNumber = 1
		IF @l_PageSize   IS NULL OR @l_PageSize   < 1 SET @l_PageSize   = 10

		SET @l_Offset = (@l_PageNumber - 1) * @l_PageSize

		-- Split @l_CustomerID ('A,B,C') into temp table (visible inside sp_executesql)
		IF OBJECT_ID('tempdb..#CustomerList') IS NOT NULL DROP TABLE #CustomerList
		CREATE TABLE #CustomerList (CustomerID NVARCHAR(100))

		IF LTRIM(RTRIM(@l_CustomerID)) <> ''
		BEGIN
			INSERT INTO #CustomerList (CustomerID)
			SELECT LTRIM(RTRIM(REPLACE(value, '''', '')))
			FROM STRING_SPLIT(@l_CustomerID, ',')
			WHERE LTRIM(RTRIM(value)) <> ''
		END

		-- Always-on filters (applied to every call)
		SET @l_Where = N'V.Status <> ''DELETED''
			AND V.OrignalRouteType IN (
				''WalmartUploadInventory'', ''TargetPlusInventoryFeedWHSWise'',
				''AmazonInventoryUpload'', ''LowesWHSWInventoryUpload'',
				''MacysInventoryUpload'', ''MichealInventoryUpload'',
				''KnotInventoryUpload'', ''KnotWHSWInventoryUpload'',
				''AmazonWHSWInventoryUpload''
			)'

		-- Conditional filters: only appended when user actually provided a value
		IF @l_Status <> ''
			SET @l_Where = @l_Where + N' AND V.Status = @p_Status'

		IF EXISTS (SELECT 1 FROM #CustomerList)
			SET @l_Where = @l_Where + N' AND V.CustomerID IN (SELECT CustomerID FROM #CustomerList)'

		IF @l_StartDate <> ''
			SET @l_Where = @l_Where + N' AND CONVERT(DATE, ISNULL(V.StartDate, V.FinishDate)) >= CONVERT(DATE, @p_StartDate)'

		IF @l_FinishDate <> ''
			SET @l_Where = @l_Where + N' AND CONVERT(DATE, ISNULL(V.StartDate, V.FinishDate)) <= CONVERT(DATE, @p_FinishDate)'

		-- Archive guard (always on) + optional ItemID match merged into same EXISTS
		IF @l_ItemID <> ''
			SET @l_Where = @l_Where + N' AND EXISTS (
				SELECT 1 FROM SCSInventoryFeedData D WITH (NOLOCK)
				WHERE D.BatchID = V.BatchID AND D.ItemId LIKE ''%'' + @p_ItemID + ''%''
			)'
		ELSE
			SET @l_Where = @l_Where + N' AND EXISTS (
				SELECT 1 FROM SCSInventoryFeedData D WITH (NOLOCK)
				WHERE D.BatchID = V.BatchID
			)'

		-- Stage matching rows ONCE into a temp table, then read twice (page + count)
		IF OBJECT_ID('tempdb..#Batches') IS NOT NULL DROP TABLE #Batches
		CREATE TABLE #Batches (
			BatchID				NVARCHAR(500),
			ItemCount			INT,
			StartDate			DATETIME,
			FinishDate			DATETIME,
			Status				VARCHAR(100),
			PageCount			INT,
			RouteType			VARCHAR(500),
			OrignalRouteType	VARCHAR(500),
			CustomerID			VARCHAR(500)
		)

		SET @l_Sql = N'
			INSERT INTO #Batches
			(BatchID, ItemCount, StartDate, FinishDate, Status, PageCount, RouteType, OrignalRouteType, CustomerID)
			SELECT V.BatchID, V.ItemCount, V.StartDate, V.FinishDate, V.Status,
				   V.PageCount, V.RouteType, V.OrignalRouteType, V.CustomerID
			FROM VW_Inventory V WITH (NOLOCK)
			WHERE ' + @l_Where

		EXEC sp_executesql @l_Sql,
			N'@p_ItemID NVARCHAR(100), @p_Status NVARCHAR(50),
			  @p_StartDate NVARCHAR(50), @p_FinishDate NVARCHAR(50)',
			@p_ItemID		= @l_ItemID,
			@p_Status		= @l_Status,
			@p_StartDate	= @l_StartDate,
			@p_FinishDate	= @l_FinishDate

		-- Result set 1 : paged rows
		SELECT BatchID, ItemCount, StartDate, FinishDate, Status,
			   PageCount, RouteType, OrignalRouteType, CustomerID
		FROM #Batches
		ORDER BY StartDate DESC
		OFFSET @l_Offset ROWS FETCH NEXT @l_PageSize ROWS ONLY

		-- Result set 2 : total count
		SELECT COUNT(*) AS TotalCount FROM #Batches

		IF OBJECT_ID('tempdb..#Batches')      IS NOT NULL DROP TABLE #Batches
		IF OBJECT_ID('tempdb..#CustomerList') IS NOT NULL DROP TABLE #CustomerList
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
