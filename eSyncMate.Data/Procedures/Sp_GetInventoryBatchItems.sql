ALTER PROCEDURE [dbo].[Sp_GetInventoryBatchItems]
	@p_BatchIDs		NVARCHAR(MAX)	= NULL,	-- single GUID or CSV of GUIDs
	@p_ItemID		NVARCHAR(100)	= NULL,	-- optional partial match
	@p_PageNumber	INT				= 1,
	@p_PageSize		INT				= 10
AS
BEGIN
	DECLARE	@l_BatchIDs		NVARCHAR(MAX)	= ISNULL(@p_BatchIDs, ''),
			@l_ItemID		NVARCHAR(100)	= ISNULL(@p_ItemID, ''),
			@l_PageNumber	INT				= @p_PageNumber,
			@l_PageSize		INT				= @p_PageSize,
			@l_Offset		INT

	BEGIN TRY
		IF @l_PageNumber IS NULL OR @l_PageNumber < 1 SET @l_PageNumber = 1
		IF @l_PageSize   IS NULL OR @l_PageSize   < 1 SET @l_PageSize   = 10

		SET @l_Offset = (@l_PageNumber - 1) * @l_PageSize

		-- Split batch IDs CSV into temp table (strip any stray quotes)
		IF OBJECT_ID('tempdb..#BatchIDs') IS NOT NULL DROP TABLE #BatchIDs
		CREATE TABLE #BatchIDs (BatchID NVARCHAR(500))

		IF LTRIM(RTRIM(@l_BatchIDs)) <> ''
		BEGIN
			INSERT INTO #BatchIDs (BatchID)
			SELECT LTRIM(RTRIM(REPLACE(value, '''', '')))
			FROM STRING_SPLIT(@l_BatchIDs, ',')
			WHERE LTRIM(RTRIM(value)) <> ''
		END

		-- No batch IDs -> nothing to return
		IF NOT EXISTS (SELECT 1 FROM #BatchIDs)
		BEGIN
			SELECT TOP 0 * FROM VW_BatchWiseInventory WITH (NOLOCK)
			SELECT 0 AS TotalCount
			RETURN
		END

		-- Stage the DEDUPED item rows in #Items.
		-- ROW_NUMBER keeps the latest row per ItemId across all passed batches.
		-- For a single batch, each ItemId already appears once -> rn=1 for all.
		IF OBJECT_ID('tempdb..#Items') IS NOT NULL DROP TABLE #Items

		;WITH Ranked AS (
			SELECT	V.CustomerID, V.ItemId, V.CustomerItemCode, V.ETA_Date, V.ETA_Qty, V.Total_ATS,
					V.ATS_L10, V.ATS_L21, V.ATS_L28, V.ATS_L29, V.ATS_L30, V.ATS_L34, V.ATS_L35,
					V.ATS_L36, V.ATS_L37, V.ATS_L40, V.ATS_L41, V.ATS_L55, V.ATS_L56, V.ATS_L57,
					V.ATS_L60, V.ATS_L65, V.ATS_L70, V.ATS_L91,
					V.Status, V.CreatedDate, V.CreatedBy, V.ModifiedDate, V.ModifiedBy,
					V.Id, V.BatchID,
					ROW_NUMBER() OVER (
						PARTITION BY V.ItemId
						ORDER BY ISNULL(V.ModifiedDate, V.CreatedDate) DESC, V.Id DESC
					) AS rn
			FROM	VW_BatchWiseInventory V WITH (NOLOCK)
			WHERE	V.BatchID IN (SELECT BatchID FROM #BatchIDs)
				AND (@l_ItemID = '' OR V.ItemId LIKE '%' + @l_ItemID + '%')
		)
		SELECT	CustomerID, ItemId, CustomerItemCode, ETA_Date, ETA_Qty, Total_ATS,
				ATS_L10, ATS_L21, ATS_L28, ATS_L29, ATS_L30, ATS_L34, ATS_L35,
				ATS_L36, ATS_L37, ATS_L40, ATS_L41, ATS_L55, ATS_L56, ATS_L57,
				ATS_L60, ATS_L65, ATS_L70, ATS_L91,
				Status, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy,
				Id, BatchID
		INTO	#Items
		FROM	Ranked
		WHERE	rn = 1

		-- Result Set 1 : paged items
		SELECT	*
		FROM	#Items
		ORDER BY ItemId
		OFFSET	@l_Offset ROWS FETCH NEXT @l_PageSize ROWS ONLY

		-- Result Set 2 : total count (for pager)
		SELECT COUNT(*) AS TotalCount FROM #Items

		IF OBJECT_ID('tempdb..#BatchIDs') IS NOT NULL DROP TABLE #BatchIDs
		IF OBJECT_ID('tempdb..#Items')    IS NOT NULL DROP TABLE #Items
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
