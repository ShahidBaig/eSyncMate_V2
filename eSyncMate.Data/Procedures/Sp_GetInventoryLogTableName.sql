ALTER PROCEDURE [dbo].[Sp_GetInventoryLogTableName]
    @p_CustomerID VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @l_TableName NVARCHAR(200) = 'SCSInventoryFeed_' + @p_CustomerID + '_Log'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @l_TableName
    )
        SET @l_TableName = NULL

    SELECT @l_TableName AS LogTableName
END
