/*==============================================================================
  08 - Shopify inventory (Task 00006): show Shopify batches in Inventory Feed Summary

  Both SPs below keep a hardcoded list of upload route types; a batch whose
  InventoryBatchWise.RouteType is not in the list never reaches the screen.
  Adds 'ShopifyInventoryUpload' next to 'AmazonWHSWInventoryUpload' in:

    Sp_GetInventoryFeedSummary          Inventory Feed Summary grid
    Sp_GetConsolidatedDownloadBatches   consolidated download of an upload batch

  Patches the LIVE definition (OBJECT_DEFINITION), not the repo copy.
  Re-runnable: an SP that already has ShopifyInventoryUpload is skipped.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @p TABLE (Seq INT IDENTITY(1,1), SpName SYSNAME, OldText NVARCHAR(200), NewText NVARCHAR(200));

-- Sp_GetInventoryFeedSummary builds the list inside dynamic SQL -> doubled quotes
INSERT INTO @p (SpName, OldText, NewText) VALUES
  ('Sp_GetInventoryFeedSummary',
   N'''''AmazonWHSWInventoryUpload''''',
   N'''''AmazonWHSWInventoryUpload'''', ''''ShopifyInventoryUpload'''''),
  ('Sp_GetConsolidatedDownloadBatches',
   N'''AmazonWHSWInventoryUpload''',
   N'''AmazonWHSWInventoryUpload'', ''ShopifyInventoryUpload''');

DECLARE @i INT = 1, @Sp SYSNAME, @Old NVARCHAR(200), @New NVARCHAR(200), @Def NVARCHAR(MAX), @Hits INT;

BEGIN TRY
    WHILE @i <= (SELECT MAX(Seq) FROM @p)
    BEGIN
        SELECT @Sp = SpName, @Old = OldText, @New = NewText FROM @p WHERE Seq = @i;
        SET @Def = OBJECT_DEFINITION(OBJECT_ID('dbo.' + @Sp));

        IF @Def IS NULL
            THROW 51400, 'An SP was not found on this server (or no VIEW DEFINITION permission). Nothing after it was changed.', 1;

        IF CHARINDEX('ShopifyInventoryUpload', @Def) > 0
            PRINT @Sp + ' already has ShopifyInventoryUpload - nothing changed.';
        ELSE
        BEGIN
            SET @Hits = (DATALENGTH(@Def) - DATALENGTH(REPLACE(@Def, @Old, ''))) / DATALENGTH(@Old);

            IF @Hits <> 1
            BEGIN
                DECLARE @Msg NVARCHAR(400) = @Sp + ': expected the AmazonWHSWInventoryUpload list entry once, found '
                                           + CAST(@Hits AS NVARCHAR(10)) + '. Add ShopifyInventoryUpload by hand.';
                THROW 51401, @Msg, 1;
            END

            SET @Def = REPLACE(@Def, @Old, @New);

            -- CREATE or ALTER header -> ALTER (only the first keyword)
            DECLARE @Create INT = PATINDEX('%CREATE%PROC%', @Def);
            DECLARE @Alter  INT = PATINDEX('%ALTER%PROC%', @Def);

            -- "CREATE OR ALTER PROCEDURE" is already fine as it is
            IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter) AND SUBSTRING(@Def, @Create, 15) <> 'CREATE OR ALTER'
                SET @Def = STUFF(@Def, @Create, LEN('CREATE'), 'ALTER');

            EXEC sp_executesql @Def;

            PRINT 'Added ShopifyInventoryUpload to ' + @Sp + '.';
        END

        SET @i += 1;
    END

    SELECT p.SpName,
           CASE WHEN CHARINDEX('ShopifyInventoryUpload', OBJECT_DEFINITION(OBJECT_ID('dbo.' + p.SpName))) > 0
                THEN 'Shopify present' ELSE 'Shopify MISSING' END AS Result
      FROM @p p ORDER BY p.Seq;
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

-- Quick check (the WKE1000 batch should come back):
-- EXEC Sp_GetInventoryFeedSummary @p_CustomerID = 'WKE1000';
