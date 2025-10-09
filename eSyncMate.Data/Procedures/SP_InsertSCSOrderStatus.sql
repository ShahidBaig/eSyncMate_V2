CREATE PROCEDURE [dbo].[SP_InsertSCSOrderStatus]
    @p_CustomerID    VARCHAR(50)  =  NULL
AS
BEGIN
    DECLARE @l_CustomerID    VARCHAR(50) = @p_CustomerID

    BEGIN TRY
		SELECT ExternalId,C.Id AS Cust_Id,O.Id AS Order_Id--,OD.Data,O.Id 
		FROM Orders O WITH (NOLOCK)
			INNER JOIN OrderData OD WITH (NOLOCK)  ON O.Id = OD.OrderId
			INNER JOIN Customers C WITH (NOLOCK) ON  O.CustomerId= C.Id
		WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = 'NEW' AND Type = 'API-JSON'
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

