ALTER PROCEDURE [dbo].[Sp_GetInventoryBatchItems]
    @p_BatchIDs   NVARCHAR(MAX) = NULL,
    @p_ItemID     NVARCHAR(100) = NULL,
    @p_PageNumber INT           = 1,
    @p_PageSize   INT           = 10
AS
BEGIN
    DECLARE @l_BatchIDs   NVARCHAR(MAX) = ISNULL(@p_BatchIDs, ''),
            @l_ItemID     NVARCHAR(100) = ISNULL(@p_ItemID, ''),
            @l_PageNumber INT           = @p_PageNumber,
            @l_PageSize   INT           = @p_PageSize,
            @l_Offset     INT,
            @l_CustomerID VARCHAR(50),
            @l_FeedTable  NVARCHAR(200),
            @l_SQL        NVARCHAR(MAX)

    BEGIN TRY
        IF @l_PageNumber IS NULL OR @l_PageNumber < 1 SET @l_PageNumber = 1
        IF @l_PageSize   IS NULL OR @l_PageSize   < 1 SET @l_PageSize   = 10

        SET @l_Offset = (@l_PageNumber - 1) * @l_PageSize

        -- Split batch IDs CSV into temp table
        IF OBJECT_ID('tempdb..#BatchIDs') IS NOT NULL DROP TABLE #BatchIDs
        CREATE TABLE #BatchIDs (BatchID NVARCHAR(500))

        IF LTRIM(RTRIM(@l_BatchIDs)) <> ''
        BEGIN
            INSERT INTO #BatchIDs (BatchID)
            SELECT LTRIM(RTRIM(REPLACE(value, '''', '')))
            FROM   STRING_SPLIT(@l_BatchIDs, ',')
            WHERE  LTRIM(RTRIM(value)) <> ''
        END

        IF NOT EXISTS (SELECT 1 FROM #BatchIDs)
        BEGIN
            SELECT TOP 0
                CAST(NULL AS VARCHAR(50))  AS CustomerID,
                CAST(NULL AS VARCHAR(50))  AS ItemId,
                CAST(NULL AS VARCHAR(50))  AS CustomerItemCode,
                CAST(NULL AS NVARCHAR(30)) AS ETA_Date,
                CAST(NULL AS INT)          AS ETA_Qty,
                CAST(NULL AS INT)          AS Total_ATS,
                CAST(NULL AS INT)          AS ATS_L10,
                CAST(NULL AS INT)          AS ATS_L21,
                CAST(NULL AS INT)          AS ATS_L28,
                CAST(NULL AS INT)          AS ATS_L29,
                CAST(NULL AS INT)          AS ATS_L30,
                CAST(NULL AS INT)          AS ATS_L34,
                CAST(NULL AS INT)          AS ATS_L35,
                CAST(NULL AS INT)          AS ATS_L36,
                CAST(NULL AS INT)          AS ATS_L37,
                CAST(NULL AS INT)          AS ATS_L40,
                CAST(NULL AS INT)          AS ATS_L41,
                CAST(NULL AS INT)          AS ATS_L55,
                CAST(NULL AS INT)          AS ATS_L56,
                CAST(NULL AS INT)          AS ATS_L57,
                CAST(NULL AS INT)          AS ATS_L60,
                CAST(NULL AS INT)          AS ATS_L65,
                CAST(NULL AS INT)          AS ATS_L70,
                CAST(NULL AS INT)          AS ATS_L91,
                CAST(NULL AS VARCHAR(25))  AS [Status],
                CAST(NULL AS DATETIME)     AS CreatedDate,
                CAST(NULL AS INT)          AS CreatedBy,
                CAST(NULL AS DATETIME)     AS ModifiedDate,
                CAST(NULL AS INT)          AS ModifiedBy,
                CAST(NULL AS INT)          AS Id,
                CAST(NULL AS NVARCHAR(500)) AS BatchID
            SELECT 0 AS TotalCount
            RETURN
        END

        -- Resolve CustomerID from first BatchID → customer-specific FeedData table
        SELECT TOP 1 @l_CustomerID = CustomerID
        FROM   InventoryBatchWise WITH (NOLOCK)
        WHERE  BatchID IN (SELECT TOP 1 BatchID FROM #BatchIDs)

        SET @l_FeedTable = N'SCSInventoryFeedData_' + @l_CustomerID

        IF OBJECT_ID('dbo.' + @l_FeedTable, 'U') IS NULL
            SET @l_FeedTable = N'VW_AllSCSInventoryFeedData'  -- fallback

        -- Item filter clause
        DECLARE @l_ItemFilter NVARCHAR(300) = N''
        IF @l_ItemID <> ''
            SET @l_ItemFilter = N' AND INV.ItemId LIKE ''%'' + @p_ItemID + ''%'''

        -- Dynamic query — joins SCSInventoryFeed directly with customer FeedData table
        IF OBJECT_ID('tempdb..#Items') IS NOT NULL DROP TABLE #Items
        CREATE TABLE #Items (
            CustomerID      VARCHAR(50),
            ItemId          VARCHAR(50),
            CustomerItemCode VARCHAR(50),
            ETA_Date        NVARCHAR(30),
            ETA_Qty         INT,
            Total_ATS       INT,
            ATS_L10         INT, ATS_L21 INT, ATS_L28 INT, ATS_L29 INT, ATS_L30 INT,
            ATS_L34         INT, ATS_L35 INT, ATS_L36 INT, ATS_L37 INT, ATS_L40 INT,
            ATS_L41         INT, ATS_L55 INT, ATS_L56 INT, ATS_L57 INT, ATS_L60 INT,
            ATS_L65         INT, ATS_L70 INT, ATS_L91 INT,
            [Status]        VARCHAR(25),
            CreatedDate     DATETIME,
            CreatedBy       INT,
            ModifiedDate    DATETIME,
            ModifiedBy      INT,
            Id              INT,
            BatchID         NVARCHAR(500)
        )

        SET @l_SQL = N'
        ;WITH Ranked AS (
            SELECT
                INV.CustomerID, INV.ItemId, INV.CustomerItemCode,
                INV.ETA_Date, INV.ETA_Qty, INV.Total_ATS,
                INV.ATS_L10, INV.ATS_L21, INV.ATS_L28, INV.ATS_L29, INV.ATS_L30,
                INV.ATS_L34, INV.ATS_L35, INV.ATS_L36, INV.ATS_L37, INV.ATS_L40,
                INV.ATS_L41, INV.ATS_L55, INV.ATS_L56, INV.ATS_L57, INV.ATS_L60,
                INV.ATS_L65, INV.ATS_L70, INV.ATS_L91,
                INV.[Status],
                BD.ActionDate AS CreatedDate,
                INV.CreatedBy,
                INV.ModifiedDate,
                INV.ModifiedBy,
                BD.Id,
                BD.BatchID,
                ROW_NUMBER() OVER (
                    PARTITION BY INV.ItemId
                    ORDER BY BD.ActionDate DESC, BD.Id DESC
                ) AS rn
            FROM SCSInventoryFeed INV WITH (NOLOCK)
            INNER JOIN (
                SELECT
                    MAX(Id) AS Id,
                    CustomerId,
                    ItemId,
                    BatchID,
                    MIN(CASE WHEN [Type] IN (''ERP-RVD'', ''JSON-SNT'') THEN CreatedDate END) AS ActionDate
                FROM ' + QUOTENAME(@l_FeedTable) + N' WITH (NOLOCK)
                WHERE BatchID IN (SELECT BatchID FROM #BatchIDs)
                GROUP BY CustomerId, ItemId, BatchID
            ) BD ON INV.CustomerID = BD.CustomerId AND INV.ItemId = BD.ItemId
            WHERE BD.BatchID IN (SELECT BatchID FROM #BatchIDs)'
            + @l_ItemFilter + N'
        )
        INSERT INTO #Items
        SELECT
            CustomerID, ItemId, CustomerItemCode, ETA_Date, ETA_Qty, Total_ATS,
            ATS_L10, ATS_L21, ATS_L28, ATS_L29, ATS_L30,
            ATS_L34, ATS_L35, ATS_L36, ATS_L37, ATS_L40,
            ATS_L41, ATS_L55, ATS_L56, ATS_L57, ATS_L60,
            ATS_L65, ATS_L70, ATS_L91,
            [Status], CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, Id, BatchID
        FROM Ranked
        WHERE rn = 1'

        EXEC sp_executesql @l_SQL,
            N'@p_ItemID NVARCHAR(100)',
            @p_ItemID = @l_ItemID

        -- Result Set 1: paged items
        SELECT *
        FROM   #Items
        ORDER BY ItemId
        OFFSET @l_Offset ROWS FETCH NEXT @l_PageSize ROWS ONLY

        -- Result Set 2: total count
        SELECT COUNT(*) AS TotalCount FROM #Items

        IF OBJECT_ID('tempdb..#BatchIDs') IS NOT NULL DROP TABLE #BatchIDs
        IF OBJECT_ID('tempdb..#Items')    IS NOT NULL DROP TABLE #Items

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(MAX);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState    INT;

        SELECT
            @ErrorMessage  = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState    = ERROR_STATE();

        PRINT 'Error Message: ' + @ErrorMessage;
        THROW;
    END CATCH;
END
GO
