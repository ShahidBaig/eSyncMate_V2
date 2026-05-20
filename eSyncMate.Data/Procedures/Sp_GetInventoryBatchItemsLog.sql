ALTER PROCEDURE [dbo].[Sp_GetInventoryBatchItemsLog]
    @p_BatchIDs   NVARCHAR(MAX),
    @p_ItemID     NVARCHAR(100) = '',
    @p_PageNumber INT           = 1,
    @p_PageSize   INT           = 10
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Resolve CustomerID from the first BatchID
    DECLARE @l_FirstBatch  NVARCHAR(500)
    DECLARE @l_CustomerID  VARCHAR(50)
    DECLARE @l_TableName   NVARCHAR(200)

    SET @l_FirstBatch = LTRIM(RTRIM(
        LEFT(@p_BatchIDs, CHARINDEX(',', @p_BatchIDs + ',') - 1)
    ))

    SELECT TOP 1 @l_CustomerID = CustomerID
    FROM   InventoryBatchWise
    WHERE  BatchID = @l_FirstBatch

    -- 2. Resolve log table name via SP (pattern-based, DB-validated)
    DECLARE @TempTableName TABLE (LogTableName NVARCHAR(200))
    INSERT INTO @TempTableName
        EXEC [dbo].[Sp_GetInventoryLogTableName] @l_CustomerID
    SELECT TOP 1 @l_TableName = LogTableName FROM @TempTableName

    -- 3. No log table found -> return empty (controller falls back to current-state)
    IF @l_TableName IS NULL
    BEGIN
        SELECT TOP 0
            CAST(NULL AS VARCHAR(50))   AS CustomerID,
            CAST(NULL AS VARCHAR(50))   AS ItemId,
            CAST(NULL AS VARCHAR(50))   AS CustomerItemCode,
            CAST(NULL AS NVARCHAR(30))  AS ETA_Date,
            CAST(NULL AS INT)           AS ETA_Qty,
            CAST(NULL AS INT)           AS Total_ATS,
            CAST(NULL AS INT)           AS ATS_L10,
            CAST(NULL AS INT)           AS ATS_L21,
            CAST(NULL AS INT)           AS ATS_L28,
            CAST(NULL AS INT)           AS ATS_L29,
            CAST(NULL AS INT)           AS ATS_L30,
            CAST(NULL AS INT)           AS ATS_L34,
            CAST(NULL AS INT)           AS ATS_L35,
            CAST(NULL AS INT)           AS ATS_L36,
            CAST(NULL AS INT)           AS ATS_L37,
            CAST(NULL AS INT)           AS ATS_L40,
            CAST(NULL AS INT)           AS ATS_L41,
            CAST(NULL AS INT)           AS ATS_L55,
            CAST(NULL AS INT)           AS ATS_L56,
            CAST(NULL AS INT)           AS ATS_L57,
            CAST(NULL AS INT)           AS ATS_L60,
            CAST(NULL AS INT)           AS ATS_L65,
            CAST(NULL AS INT)           AS ATS_L70,
            CAST(NULL AS INT)           AS ATS_L91,
            CAST(NULL AS VARCHAR(25))   AS [Status],
            CAST(NULL AS DATETIME)      AS ModifiedDate,
            CAST(NULL AS NVARCHAR(500)) AS BatchID,
            CAST(NULL AS VARCHAR(20))   AS LogType

        SELECT 0 AS TotalCount
        RETURN
    END

    -- 4. Split BatchIDs CSV into temp table
    IF OBJECT_ID('tempdb..#LogBatchIDs') IS NOT NULL
        DROP TABLE #LogBatchIDs

    CREATE TABLE #LogBatchIDs (BatchID NVARCHAR(500))

    INSERT INTO #LogBatchIDs (BatchID)
    SELECT LTRIM(RTRIM(value))
    FROM   STRING_SPLIT(@p_BatchIDs, ',')
    WHERE  LTRIM(RTRIM(value)) <> ''

    -- 5. Item filter clause
    DECLARE @l_ItemFilter NVARCHAR(300) = ''
    IF @p_ItemID IS NOT NULL AND LTRIM(RTRIM(@p_ItemID)) <> ''
        SET @l_ItemFilter = N' AND ItemId LIKE ''%'
                          + REPLACE(LTRIM(RTRIM(@p_ItemID)), '''', '''''')
                          + N'%'''

    -- 6. Dynamic SQL
    DECLARE @l_Offset INT = (@p_PageNumber - 1) * @p_PageSize
    DECLARE @l_SQL    NVARCHAR(MAX)

    SET @l_SQL = N'
    ;WITH CTE AS (
        SELECT
            CustomerID, ItemId, CustomerItemCode,
            ETA_Date, ETA_Qty, Total_ATS,
            ATS_L10, ATS_L21, ATS_L28, ATS_L29, ATS_L30,
            ATS_L34, ATS_L35, ATS_L36, ATS_L37, ATS_L40,
            ATS_L41, ATS_L55, ATS_L56, ATS_L57, ATS_L60,
            ATS_L65, ATS_L70, ATS_L91,
            [Status],
            LogDate AS ModifiedDate,
            BatchID,
            LogType,
            ROW_NUMBER() OVER (
                PARTITION BY ItemId
                ORDER BY LogDate DESC
            ) AS rn
        FROM ' + QUOTENAME(@l_TableName) + N'
        WHERE BatchID IN (SELECT BatchID FROM #LogBatchIDs)'
        + @l_ItemFilter + N'
    )
    SELECT
        CustomerID, ItemId, CustomerItemCode,
        ETA_Date, ETA_Qty, Total_ATS,
        ATS_L10, ATS_L21, ATS_L28, ATS_L29, ATS_L30,
        ATS_L34, ATS_L35, ATS_L36, ATS_L37, ATS_L40,
        ATS_L41, ATS_L55, ATS_L56, ATS_L57, ATS_L60,
        ATS_L65, ATS_L70, ATS_L91,
        [Status], ModifiedDate, BatchID, LogType
    FROM CTE
    WHERE rn = 1
    ORDER BY ItemId
    OFFSET ' + CAST(@l_Offset  AS NVARCHAR(10)) + N' ROWS
    FETCH NEXT '  + CAST(@p_PageSize AS NVARCHAR(10)) + N' ROWS ONLY;

    SELECT COUNT(DISTINCT ItemId) AS TotalCount
    FROM ' + QUOTENAME(@l_TableName) + N'
    WHERE BatchID IN (SELECT BatchID FROM #LogBatchIDs)'
    + @l_ItemFilter + N';'

    EXEC sp_executesql @l_SQL

END
