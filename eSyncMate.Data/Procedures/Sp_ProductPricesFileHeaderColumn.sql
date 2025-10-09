CREATE PROCEDURE [dbo].[Sp_ProductPricesFileHeaderColumn]
    @p_CustomerID        NVARCHAR(500) = '',
    @p_UserNo        INT = 0
AS
BEGIN
    DECLARE @l_CustomerID		 NVARCHAR(500)
    DECLARE @l_UserNo		 INT

	CREATE TABLE #l_HardCodeProperty
	(
		propertyName    VARCHAR(1000)
	)
    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
        SET @l_UserNo = @p_UserNo;


		INSERT INTO #l_HardCodeProperty(propertyName)
		VALUES 
				   
				('ItemID'),
				('ListPrice'),
				('MapPrice'),
				('OffPrice'),
				('ID')

		SELECT  STRING_AGG(propertyName, ',')  [Name]
		FROM #l_HardCodeProperty
	
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

	DROP TABLE #l_HardCodeProperty
END
