CREATE PROCEDURE [dbo].[SP_CLTUpdateAddress]
    @p_UserNo    INT  =  0
AS
BEGIN
    DECLARE @l_UserNo   VARCHAR(50) = @p_UserNo

    BEGIN TRY
		
		UPDATE CT
		SET CT.TrackStatus     = temp.TrackStatus,
			CT.ShipFromAddress = temp.ShipFromAddress,
			CT.ShipFromCity    = temp.ShipFromCity,
			CT.ShipFromState   = temp.ShipFromState,
			CT.ShipFromZip     = temp.ShipFromZip,
			CT.ShipFromCountry = temp.ShipFromCountry,
			CT.Status = CASE WHEN temp.TrackStatus = 'D1' THEN 'ReadyToComplete' ELSE temp.TrackStatus END
		FROM CarrierLoadTender CT
		INNER JOIN [Temp_CLTUpdateAddress] temp ON CT.ShipperNo = temp.ShipperNo AND CT.ShipmentId = temp.ShipmentId

		INSERT INTO [CLTUpdateAddress_Log](ShipmentId,ShipperNo,TrackStatus,ShipFromAddress,ShipFromCity,ShipFromState,ShipFromZip,ShipFromCountry,LogDate)
		SELECT ShipmentId,ShipperNo,TrackStatus,ShipFromAddress,ShipFromCity,ShipFromState,ShipFromZip,ShipFromCountry,GETDATE() 
		FROM [Temp_CLTUpdateAddress]

		DELETE FROM [Temp_CLTUpdateAddress]

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

