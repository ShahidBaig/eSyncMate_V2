ALTER PROCEDURE [dbo].[Sp_ProductPromotionFileHeaderColumn]
AS
BEGIN
   
   CREATE TABLE #l_HardCodeProperty
    (
        propertyName    VARCHAR(8000),
		[Required]		varchar(50)
    )
    
    BEGIN TRY

       INSERT INTO #l_HardCodeProperty(propertyName, Required)
		VALUES ('ItemId', 'True'), 
			   ('ListPrice', 'True'),
			   ('OffPrice', 'True'),
			   ('MAPPrice', 'True'),
			   ('PromoStartDate', 'True'),
			   ('PromoEndDate',  'True'),
			   ('OldListPrice', 'True'),
			   ('OldOffPrice', 'True'),
			   ('OldMAPPrice', 'True');


		UPDATE #l_HardCodeProperty 
		SET propertyName = CASE WHEN [Required] = 'True' THEN '*' + [propertyName] ELSE [propertyName] END;

		SELECT STRING_AGG(propertyName, ',') Name 
		FROM #l_HardCodeProperty;
        
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
