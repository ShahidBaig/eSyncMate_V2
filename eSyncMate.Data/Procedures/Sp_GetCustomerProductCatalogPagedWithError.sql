CREATE OR ALTER PROCEDURE [dbo].[Sp_GetCustomerProductCatalogPagedWithError]
    @p_Criteria   NVARCHAR(MAX) = NULL,
    @p_OrderBy    NVARCHAR(200) = NULL,
    @p_PageNumber INT           = 1,
    @p_PageSize   INT           = 10
AS
BEGIN
    /*  Product Catalog grid: one page of items, plus the error shown on hover.

        Kept in a procedure so the rules that decide what counts as a reportable error can be
        corrected without redeploying the Processor.

        Result Set 1: the page, with ErrorData / ErrorType / ErrorDate
        Result Set 2: TotalCount
    */
    SET NOCOUNT ON

    DECLARE @l_Criteria NVARCHAR(MAX) = ISNULL(LTRIM(RTRIM(@p_Criteria)), ''),
            @l_OrderBy  NVARCHAR(200) = ISNULL(NULLIF(LTRIM(RTRIM(@p_OrderBy)), ''), 'ProductId DESC'),
            @l_PageSize INT           = 10,
            @l_Offset   INT,
            @l_Where    NVARCHAR(MAX),
            @l_SQL      NVARCHAR(MAX)

    BEGIN TRY
        IF ISNULL(@p_PageSize, 0)   > 0 SET @l_PageSize = @p_PageSize
        IF ISNULL(@p_PageNumber, 0) < 1 SET @p_PageNumber = 1

        SET @l_Offset = (@p_PageNumber - 1) * @l_PageSize
        SET @l_Where  = CASE WHEN @l_Criteria = '' THEN '' ELSE ' WHERE ' + @l_Criteria END

        SET @l_SQL = N'
        WITH PagedCatalog AS (
            SELECT * FROM [VW_SCS_CustomerProductCatalog]' + @l_Where + N'
            ORDER BY ' + @l_OrderBy + N'
            OFFSET @p_Offset ROWS FETCH NEXT @p_PageSize ROWS ONLY
        )
        SELECT P.*, ERR.ErrorData, ERR.ErrorType, ERR.ErrorDate
        FROM PagedCatalog P
        OUTER APPLY (
            SELECT TOP 1
                   LEFT(D.Data, 8000) AS ErrorData,
                   D.Type             AS ErrorType,
                   D.CreatedDate      AS ErrorDate
            FROM [SCS_CustomerProductCatalogData] D
            -- Data is NVARCHAR(MAX); a fixed 300-char head keeps the text tests on the first LOB page
            CROSS APPLY (SELECT DataHead = LOWER(SUBSTRING(D.Data, 1, 300))) H
            WHERE D.ProductId = P.ProductId
              AND DATALENGTH(D.Data) > 0
              -- An item in a final good state has nothing to report. The view folds SYNCED/DELETED
              -- into Published and APPROVED/APPROVED_PR into Approved; raw values listed too.
              AND UPPER(ISNULL(P.SyncStatus, '''')) NOT IN (''APPROVED'', ''PUBLISHED'', ''APPROVED_PR'')
              -- A transport failure says nothing about the product, so it never becomes its error
              AND NOT (D.Type IN (''REQ-ERR'', ''RSP-ERR'') AND (
                       H.DataHead LIKE ''%upstream%''
                    OR H.DataHead LIKE ''%timeout%''
                    OR H.DataHead LIKE ''%timed out%''
                    OR H.DataHead LIKE ''%gateway%''
                    OR H.DataHead LIKE ''%service unavailable%''
                    OR H.DataHead LIKE ''%temporarily unavailable%''
                    OR H.DataHead LIKE ''%internal server error%''
                    OR H.DataHead LIKE ''%connection%refused%''
                    OR H.DataHead LIKE ''%too many requests%''
                    OR H.DataHead LIKE ''%rate limit%''
              ))
              AND (
                    -- PRD/UNL/STA/LOG are the per-operation types; REQ/RSP are their predecessors,
                    -- kept so rows written before the split still show
                    (   D.Type IN (''PRD-ERR'', ''UNL-ERR'', ''STA-ERR'', ''LOG-ERR'', ''RSP-ERR'', ''REQ-ERR'', ''Internal'')
                        -- Error rows are never deleted on success, so a corrected and re-uploaded item
                        -- still carries its old one. Anything older than the last upload is history.
                        -- Day granularity matches VW_SCS_CustomerProductCatalogData.
                    AND CONVERT(DATE, D.CreatedDate) >= ISNULL((
                            SELECT CONVERT(DATE, ISNULL(C.ModifiedDate, C.CreatedDate))
                            FROM [SCS_CustomerProductCatalog] C
                            WHERE C.ProductId = P.ProductId
                        ), CONVERT(DATE, D.CreatedDate))
                    )
                    -- A rejection carries no *-ERR row: the reasons sit inside the status response
                 OR (   D.Type IN (''STA-RSP'', ''RSP-JSON'')
                    AND (   P.SyncStatus IN (''REJECTED'', ''Error'')
                            -- A successful response still carries the key as an empty array,
                            -- so only a populated array is a real finding
                         OR (   CHARINDEX(''"errors":[{'', REPLACE(REPLACE(REPLACE(REPLACE(LEFT(D.Data, 8000), '' '', ''''), CHAR(13), ''''), CHAR(10), ''''), CHAR(9), '''')) > 0
                            AND P.SyncStatus NOT IN (''Published'', ''Approved'', ''Pending'')
                            )
                        )
                    )
              )
            -- A real error outranks a status payload, then newest first
            ORDER BY CASE WHEN D.Type IN (''STA-RSP'', ''RSP-JSON'') THEN 1 ELSE 0 END,
                     D.CreatedDate DESC,
                     D.Id DESC
        ) ERR
        ORDER BY ' + @l_OrderBy

        -- Result Set 1: the page
        EXEC sp_executesql @l_SQL,
             N'@p_Offset INT, @p_PageSize INT',
             @p_Offset   = @l_Offset,
             @p_PageSize = @l_PageSize

        -- Result Set 2: total count
        SET @l_SQL = N'SELECT COUNT(*) AS TotalCount FROM [VW_SCS_CustomerProductCatalog]' + @l_Where

        EXEC sp_executesql @l_SQL
    END TRY
    BEGIN CATCH
        THROW
    END CATCH
END
