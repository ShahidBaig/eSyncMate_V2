ALTER PROCEDURE [dbo].[SP_OrdersData]
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
		    SELECT O.Id, OD.Data,O.OrderNumber, OD.Id OrderDataId,O.ExternalId,O.Status
		    FROM Orders O WITH (NOLOCK)
			    INNER JOIN OrderData OD WITH (NOLOCK)  ON O.Id = OD.OrderId
			    INNER JOIN Customers C WITH (NOLOCK)  ON O.CustomerId = C.Id
		    WHERE C.ERPCustomerID = @l_CustomerID AND O.Status IN (
							SELECT LTRIM(RTRIM(value))
							FROM STRING_SPLIT(@l_OrderStatus, ',')
						  ) AND Type = @l_DataType
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
			ELSE IF @l_CustomerID IN ('MAC0149M')
			BEGIN
					SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' OR D.ShippingMethod = 'FEDEX GROUND' THEN 'fedex' WHEN D.ShippingMethod LIKE 'UPS %' THEN 'ups' ELSE 'fedex' END) LevelOfService,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' ELSE 'FedExGround' END ) ShippingMethod,
					O.ExternalId AS  SellerOrderId,MAX(D.ItemID) ItemID
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
			ELSE IF @l_CustomerID IN ('LOW2221MP')
			BEGIN
					SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' OR D.ShippingMethod = 'FEDEX GROUND' THEN 'FDEG' WHEN D.ShippingMethod LIKE 'UPS %' THEN 'UPSG' ELSE 'FDEG' END) LevelOfService,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' ELSE 'FedExGround' END ) ShippingMethod,
					O.ExternalId AS  SellerOrderId,MAX(D.ItemID) ItemID
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
			ELSE IF @l_CustomerID IN ('KNO8068')
			BEGIN
				SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
							MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' OR D.ShippingMethod = 'FEDEX GROUND' THEN 'fedex' WHEN D.ShippingMethod LIKE 'UPS %' THEN 'ups' ELSE 'fedex' END) LevelOfService,
							MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' ELSE 'FedExGround' END) ShippingMethod,
							O.ExternalId AS SellerOrderId, MAX(D.ItemID) ItemID,
							-- Shipped = true when total ordered qty = total ASN qty (no remaining qty to ship)
							CAST(CASE WHEN MAX(OD.RemainingQty) = 0 THEN 1 ELSE 0 END AS BIT) AS Shipped
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
					CROSS APPLY (
						SELECT SUM(D2.LineQty) - SUM(ISNULL(D2.ASNQty, 0)) - SUM(ISNULL(D2.CancelQty, 0)) RemainingQty
						FROM OrderDetail D2 WITH (NOLOCK)
						WHERE D2.OrderId = O.Id
					) OD
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
			ELSE IF @l_CustomerID IN ('AMA1005')
			BEGIN
					SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' OR D.ShippingMethod = 'FEDEX GROUND' THEN 'FedEx' WHEN D.ShippingMethod LIKE 'UPS %' THEN 'UPSG' ELSE 'FedEx' END) LevelOfService,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedEx Ground' ELSE 'FedEx Ground' END ) ShippingMethod,
					O.ExternalId AS  SellerOrderId,MAX(D.ItemID) ItemID,Max(D.order_line_id) AS order_line_id,Max(D.Id) AS OrderDetailID
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD' --AND O.Id = 234033
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
			ELSE IF @l_CustomerID IN ('MIC1300MP')
			BEGIN
					SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' OR D.ShippingMethod = 'FEDEX GROUND' THEN 'FEDEX' WHEN D.ShippingMethod LIKE 'UPS %' THEN 'UPS' ELSE 'FEDEX' END) LevelOfService,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' ELSE 'FedExGround' END ) ShippingMethod,
					O.ExternalId AS  SellerOrderId,MAX(D.ItemID) ItemID,Max(D.order_line_id) AS order_line_id,Max(D.Id) AS OrderDetailID
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
			END
			ELSE
			BEGIN
				--SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate + 'T' + CONVERT(VARCHAR,GETDATE(), 114) + 'Z' ShippedDate, SUM(D.LineQty) LineQty,
				SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, 
					CASE WHEN CONVERT(VARCHAR,D.ShippedDate,101)=REPLACE(CONVERT(VARCHAR,O.OrderDate, 102), '.', '-') THEN LEFT(D.ShippedDate,10) + 'T' + SUBSTRING(CONVERT(VARCHAR,DATEADD(HH,1, O.OrderDate), 114),0,9) + '.000Z' ELSE LEFT(D.ShippedDate,10) + 'T00:00:00.000Z' END ShippedDate, SUM(D.LineQty) LineQty,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FH' 
							 WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'G2' 
							 WHEN D.ShippingMethod = 'UPS SurePost USPS Delivery' THEN 'SP' 
							 WHEN D.ShippingMethod = 'Fedex Smartpost' THEN 'FP' 
							 WHEN D.ShippingMethod = 'UPS Ground Service' THEN 'G2' 
							 ELSE 'FH' END) LevelOfService,
					MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' 
							 WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' 
							 WHEN D.ShippingMethod = 'UPS SurePost USPS Delivery' THEN 'UPSSurePost' 
							 WHEN D.ShippingMethod = 'Fedex Smartpost' THEN 'FedExSmartPost'
							 WHEN D.ShippingMethod = 'UPS Ground Service' THEN 'UPSGround'
							 ELSE 'FedExGround' END ) ShippingMethod,
					O.ExternalId AS  SellerOrderId
				FROM Orders O WITH (NOLOCK)
					INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
					INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
				WHERE C.ERPCustomerID = @l_CustomerID AND O.Status = @l_OrderStatus AND ISNULL(D.TrackingNo, '') <> ''
					AND ISNULL(D.[Status], '') = 'ASNRVD'
				GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, O.OrderDate
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
