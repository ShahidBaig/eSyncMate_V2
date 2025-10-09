CREATE TRIGGER trg_OrderDataLogs
ON OrderData
AFTER UPDATE
AS
BEGIN
    INSERT INTO OrderDataLogs (OrderId, Type, OrderNumber, Data, Status, CreatedDate)
    SELECT OrderId, Type, OrderNumber, Data, Status, CreatedDate
    FROM deleted;
END;
GO
