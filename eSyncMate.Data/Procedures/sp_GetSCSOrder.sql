DROP PROCEDURE IF EXISTS [dbo].[SP_GETSCSOrder]
GO

CREATE PROCEDURE [dbo].[SP_GETSCSOrder]
    @p_CustomerID    VARCHAR(50)  =  NULL
AS
BEGIN
    DECLARE @l_CustomerID    VARCHAR(50) = @p_CustomerID

    BEGIN TRY
		SELECT O.ExternalId,O.Id,O.OrderNumber
		FROM Orders O WITH (NOLOCK)
			INNER JOIN Customers C WITH (NOLOCK) ON  O.CustomerId= C.Id
            INNER JOIN (SELECT OrderId, SUM(LineQty - ISNULL(ASNQty,0) - ISNULL(CancelQty,0)) TotalQty FROM OrderDetail GROUP BY OrderId) D ON O.Id = D.OrderId
		WHERE C.ERPCustomerID = @l_CustomerID AND O.Status IN ('SYNCED', 'SHIPPED') AND D.TotalQty > 0
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
GO