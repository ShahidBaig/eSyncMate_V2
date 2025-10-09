DROP PROCEDURE IF EXISTS [dbo].[SP_OrdersData]
GO

CREATE PROCEDURE [dbo].[SP_OrdersData]
    @p_CustomerID       VARCHAR(50)  =  '',
    @p_DataType         VARCHAR(25) =  'API-JSON',
    @p_OrderStatus      VARCHAR(25) =  'New',
    @p_OrderDataStatus  VARCHAR(25) =  '@ORDERDATASTATUS@',
    @p_OrderId			INT =  0
AS
BEGIN
    DECLARE @l_CustomerID   VARCHAR(50),
            @l_DataType     VARCHAR(25),
            @l_OrderStatus  VARCHAR(25),
            @l_OrderDataStatus  VARCHAR(25) =  @p_OrderDataStatus,
			@l_OrderId		INT = @p_OrderId
    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
        SET @l_DataType = @p_DataType
        SET @l_OrderStatus = @p_OrderStatus

        IF @l_OrderDataStatus = '@ORDERDATASTATUS@'
        BEGIN
		    SELECT O.Id, OD.Data,O.OrderNumber, OD.Id OrderDataId,O.ExternalId
		    FROM Orders O WITH (NOLOCK)
			    INNER JOIN OrderData OD WITH (NOLOCK)  ON O.Id = OD.OrderId
			    INNER JOIN Customers C WITH (NOLOCK)  ON O.CustomerId = C.Id
		    WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND Type = @l_DataType
				AND O.Id = CASE WHEN @l_OrderId = 0 THEN O.Id ELSE @l_OrderId END
        END 
        ELSE
        BEGIN
			IF @l_CustomerID = 'WAL4001MP'
			BEGIN
					SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FH' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'G2' ELSE 'FH' END) LevelOfService,
					MAX(O.ShipToAddress2) ShippingMethod,
					O.ExternalId AS  SellerOrderId
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
			ELSE IF @l_CustomerID = 'MAC0149M'
			BEGIN
					SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'fedex' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'fedex' ELSE 'fedex' END) LevelOfService,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' ELSE 'FedExGround' END ) ShippingMethod,
					O.ExternalId AS  SellerOrderId,MAX(D.ItemID) ItemID
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
			ELSE
			BEGIN
					SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FH' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'G2' ELSE 'FH' END) LevelOfService,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' ELSE 'FedExGround' END ) ShippingMethod,
					O.ExternalId AS  SellerOrderId
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
        END 
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