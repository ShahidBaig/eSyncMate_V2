/*==============================================================================
  06 - Shopify inventory (Task 00006): source data in Sp_SCS_GetProductCatalog

  One branch, @p_RouteTypeID = 'ShopifyInventoryUpload' (the route passes
  RouteTypesEnum.ShopifyInventoryUpload as @ROUTETYPEID@):

      the customer's UPDATED / NEW SCSInventoryFeed rows (ERP Full / Differential feed),
      ATS_<WHSID> per warehouse, SKU = CustomerItemCode when filled else ItemId,
      ProductId from CustomerProductCatalogPrices when the item has one.

  The production copy of this SP differs from the repo copy, so it is NOT replaced:
  the script reads the LIVE definition on this server and runs it as ALTER with only this branch changed.
  Every other branch stays exactly as it is.
  Run it as often as needed:
    branch missing -> inserted just before the last END TRY
    branch there   -> its text (from its ELSE IF up to the next ELSE IF / the last END TRY) replaced by @Branch
==============================================================================*/

SET NOCOUNT ON;

DECLARE @Def NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'));

DECLARE @Branch NVARCHAR(MAX) = N'ELSE IF  @l_RouteTypeID = ''ShopifyInventoryUpload''
		BEGIN
			-- Task 00006: Shopify warehouse-wise inventory
			SELECT INV.CustomerID, INV.ItemId, INV.CustomerItemCode,
				   ISNULL(NULLIF(LTRIM(RTRIM(INV.CustomerItemCode)), ''''), INV.ItemId) AS SKU,
				   PR.id, PR.ProductId, INV.Total_ATS,
				   INV.ATS_L10, INV.ATS_L21, INV.ATS_L28, INV.ATS_L29, INV.ATS_L30, INV.ATS_L34, INV.ATS_L35, INV.ATS_L36,
				   INV.ATS_L37, INV.ATS_L40, INV.ATS_L41, INV.ATS_L55, INV.ATS_L56, INV.ATS_L57, INV.ATS_L60, INV.ATS_L65,
				   INV.ATS_L70, INV.ATS_L91
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				LEFT OUTER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
			WHERE INV.CustomerID = @l_CustomerID AND INV.[Status] IN (''UPDATED'',''NEW'')
		END
		';

BEGIN TRY
    IF @Def IS NULL
        THROW 51350, 'Sp_SCS_GetProductCatalog not found on this server (or no VIEW DEFINITION permission).', 1;

    IF CHARINDEX('END TRY', @Def) = 0 OR CHARINDEX('@l_RouteTypeID', @Def) = 0
        THROW 51351, 'Sp_SCS_GetProductCatalog does not have the expected END TRY / @l_RouteTypeID shape - change the branch by hand.', 1;

    -- the last END TRY (DATALENGTH, not LEN: LEN drops trailing spaces, REVERSE does not)
    DECLARE @EndTry INT = DATALENGTH(@Def) / 2 - CHARINDEX(REVERSE('END TRY'), REVERSE(@Def)) - LEN('END TRY') + 2;
    DECLARE @Marker INT = CHARINDEX('''ShopifyInventoryUpload''', @Def);

    IF @Marker = 0
    BEGIN
        SET @Def = STUFF(@Def, @EndTry, 0, @Branch);
        PRINT 'Adding the ShopifyInventoryUpload branch.';
    END
    ELSE
    BEGIN
        IF CHARINDEX('''ShopifyInventoryUpload''', @Def, @Marker + 1) > 0
            THROW 51352, 'Sp_SCS_GetProductCatalog names ShopifyInventoryUpload more than once - change the branch by hand.', 1;

        -- from the ELSE IF of the branch up to the next ELSE IF (or the last END TRY)
        DECLARE @Before NVARCHAR(MAX) = LEFT(@Def, @Marker - 1);
        DECLARE @Start  INT = DATALENGTH(@Before) / 2 - CHARINDEX(REVERSE('ELSE IF'), REVERSE(@Before)) - LEN('ELSE IF') + 2;
        DECLARE @Next   INT = CHARINDEX('ELSE IF', @Def, @Marker);
        DECLARE @End    INT = CASE WHEN @Next > 0 AND @Next < @EndTry THEN @Next ELSE @EndTry END;

        IF CHARINDEX(REVERSE('ELSE IF'), REVERSE(@Before)) = 0 OR @Start <= 0 OR @End <= @Marker
            THROW 51353, 'Could not find where the ShopifyInventoryUpload branch starts / ends - change it by hand.', 1;

        SET @Def = STUFF(@Def, @Start, @End - @Start, @Branch);
        PRINT 'Replacing the ShopifyInventoryUpload branch.';
    END

    -- CREATE or ALTER header -> ALTER (only the first keyword)
    DECLARE @Create INT = PATINDEX('%CREATE%PROC%', @Def);
    DECLARE @Alter  INT = PATINDEX('%ALTER%PROC%', @Def);

    -- "CREATE OR ALTER PROCEDURE" is already fine as it is
    IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter) AND SUBSTRING(@Def, @Create, 15) <> 'CREATE OR ALTER'
        SET @Def = STUFF(@Def, @Create, LEN('CREATE'), 'ALTER');

    EXEC sp_executesql @Def;

    PRINT 'Sp_SCS_GetProductCatalog altered.';

    SELECT CASE WHEN CHARINDEX('''ShopifyInventoryUpload''', OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'))) > 0
                THEN 'Branch present' ELSE 'Branch MISSING' END AS ShopifyBranch;
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

-- Quick check (no rows until the customer has UPDATED / NEW inventory):
-- EXEC Sp_SCS_GetProductCatalog @p_CustomerID = 'WKE1000', @p_RouteTypeID = 'ShopifyInventoryUpload', @p_UserNo = 1;
