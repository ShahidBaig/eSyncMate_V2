/* ------------------------------------------------------------------
   64 — Customer-wise field mapping for the SO shipping fields

   Where each of the four values sits inside a marketplace's order payload is configuration,
   not code. This table holds it, so a new partner — or a partner changing their JSON — is a
   row change, never a build.

       WarehouseCode | ShippingCode | ShippingAgentCode | ShipDate

   ReadPath   SQL JSON_VALUE path (single value, so array positions are [0])
   WritePath  Newtonsoft path used when saving; [*] applies the value to every array element.
              Falls back to ReadPath when empty.
   ValueType  how the raw payload value is turned into what the screen shows, and back:
                TEXT             as-is
                FIRST_TOKEN      "L55 Whitestown IN" / "L65, Patterson CA" -> L55 / L65
                ISO_DATE         2026-07-22T06:59:59Z  <-> MM/dd/yyyy
                EPOCH_MS_NUM     1784714400000 (number) <-> MM/dd/yyyy
                EPOCH_MS_STR     "1784748206052" (string) <-> MM/dd/yyyy
                TARGET_SHIPNODE  distribution_center_id <-> TargetPlusShipNodes.WHSID
   Priority   lowest wins when a customer has more than one candidate path.

   A field with no row for a customer is simply not available for that partner: it shows
   empty and saving does not invent a place for it.
   ------------------------------------------------------------------ */

IF OBJECT_ID('dbo.OrderFieldMappings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderFieldMappings
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderFieldMappings PRIMARY KEY,
        CustomerID  VARCHAR(50)  NOT NULL,
        FieldName   VARCHAR(50)  NOT NULL,
        ReadPath    VARCHAR(400) NOT NULL,
        WritePath   VARCHAR(400) NULL,
        ValueType   VARCHAR(30)  NOT NULL CONSTRAINT DF_OrderFieldMappings_ValueType DEFAULT ('TEXT'),
        Priority    INT          NOT NULL CONSTRAINT DF_OrderFieldMappings_Priority DEFAULT (1),
        IsActive    BIT          NOT NULL CONSTRAINT DF_OrderFieldMappings_IsActive DEFAULT (1),
        CreatedDate DATETIME     NOT NULL CONSTRAINT DF_OrderFieldMappings_CreatedDate DEFAULT (GETDATE()),
        CreatedBy   INT          NOT NULL CONSTRAINT DF_OrderFieldMappings_CreatedBy DEFAULT (0),
        ModifiedDate DATETIME    NULL,
        ModifiedBy  INT          NULL
    );

    CREATE INDEX IX_OrderFieldMappings_Customer_Field
        ON dbo.OrderFieldMappings (CustomerID, FieldName, Priority) INCLUDE (ReadPath, WritePath, ValueType, IsActive);
END
GO

/* ---------------- Seed: the agreed mapping ---------------- */

DELETE FROM dbo.OrderFieldMappings;
GO

INSERT INTO dbo.OrderFieldMappings (CustomerID, FieldName, ReadPath, WritePath, ValueType, Priority)
VALUES
-- ============ Amazon (not covered by the client's list — current derivation) ============
('AMA1000', 'WarehouseCode',     '$.DefaultShipFromLocationAddress.Name', '$.DefaultShipFromLocationAddress.Name', 'FIRST_TOKEN', 1),
('AMA1000', 'ShippingAgentCode', '$.ShipmentServiceLevelCategory',        NULL,                                    'TEXT',        1),
('AMA1000', 'ShipDate',          '$.LatestShipDate',                      NULL,                                    'ISO_DATE',    1),

('AMA1005', 'WarehouseCode',     '$.DefaultShipFromLocationAddress.Name', '$.DefaultShipFromLocationAddress.Name', 'FIRST_TOKEN', 1),
('AMA1005', 'ShippingAgentCode', '$.ShipmentServiceLevelCategory',        NULL,                                    'TEXT',        1),
('AMA1005', 'ShipDate',          '$.LatestShipDate',                      NULL,                                    'ISO_DATE',    1),

('MOR4778', 'WarehouseCode',     '$.DefaultShipFromLocationAddress.Name', '$.DefaultShipFromLocationAddress.Name', 'FIRST_TOKEN', 1),
('MOR4778', 'ShippingAgentCode', '$.ShipmentServiceLevelCategory',        NULL,                                    'TEXT',        1),
('MOR4778', 'ShipDate',          '$.LatestShipDate',                      NULL,                                    'ISO_DATE',    1),

-- ============ Target — warehouse resolved through TargetPlusShipNodes ============
('TAR6266P',   'WarehouseCode', '$.distribution_center_id',   NULL, 'TARGET_SHIPNODE', 1),
('TAR6266P',   'ShipDate',      '$.requested_shipment_date',  NULL, 'ISO_DATE',        1),

('TAR6266PAH', 'WarehouseCode', '$.distribution_center_id',   NULL, 'TARGET_SHIPNODE', 1),
('TAR6266PAH', 'ShipDate',      '$.requested_shipment_date',  NULL, 'ISO_DATE',        1),

-- ============ Mirakl — Knot / Lowe's / Macy's ============
('KNO8068',   'WarehouseCode',     '$.order_lines[0].shipping_from.warehouse.code', '$.order_lines[*].shipping_from.warehouse.code', 'TEXT',     1),
('KNO8068',   'ShippingCode',      '$.shipping_type_standard_code',                 NULL,                                            'TEXT',     1),
('KNO8068',   'ShippingAgentCode', '$.shipping_type_label',                         NULL,                                            'TEXT',     1),
('KNO8068',   'ShipDate',          '$.order_lines[0].shipped_date',                 '$.order_lines[*].shipped_date',                 'ISO_DATE', 1),

('LOW2221MP', 'WarehouseCode',     '$.order_lines[0].shipping_from.warehouse.code', '$.order_lines[*].shipping_from.warehouse.code', 'TEXT',     1),
('LOW2221MP', 'ShippingCode',      '$.shipping_type_standard_code',                 NULL,                                            'TEXT',     1),
('LOW2221MP', 'ShippingAgentCode', '$.shipping_type_label',                         NULL,                                            'TEXT',     1),
('LOW2221MP', 'ShipDate',          '$.order_lines[0].shipped_date',                 '$.order_lines[*].shipped_date',                 'ISO_DATE', 1),

('MAC0149M',  'WarehouseCode',     '$.order_lines[0].shipping_from.warehouse.code', '$.order_lines[*].shipping_from.warehouse.code', 'TEXT',     1),
('MAC0149M',  'ShippingCode',      '$.shipping_type_standard_code',                 NULL,                                            'TEXT',     1),
('MAC0149M',  'ShippingAgentCode', '$.shipping_type_label',                         NULL,                                            'TEXT',     1),
('MAC0149M',  'ShipDate',          '$.order_lines[0].shipped_date',                 '$.order_lines[*].shipped_date',                 'ISO_DATE', 1),

-- ============ Walmart — carrier and method live in the line's tracking info ============
('WAL4001MP', 'ShippingCode',      '$.orderLines.orderLine[0].orderLineStatuses.orderLineStatus[0].trackingInfo.carrierName.carrier',
                                   '$.orderLines.orderLine[*].orderLineStatuses.orderLineStatus[*].trackingInfo.carrierName.carrier', 'TEXT', 1),
('WAL4001MP', 'ShippingAgentCode', '$.orderLines.orderLine[0].orderLineStatuses.orderLineStatus[0].trackingInfo.methodCode',
                                   '$.orderLines.orderLine[*].orderLineStatuses.orderLineStatus[*].trackingInfo.methodCode',          'TEXT', 1),
('WAL4001MP', 'ShipDate',          '$.shippingInfo.estimatedShipDate', NULL, 'EPOCH_MS_NUM', 1),

-- ============ Michaels — service level only ============
('MIC1300MP', 'ShippingAgentCode', '$.orderLines[0].serviceLevel', '$.orderLines[*].serviceLevel', 'TEXT', 1);
GO

/* ---------------- Reader used by the application ---------------- */

IF OBJECT_ID('dbo.Sp_GetOrderFieldMappings', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sp_GetOrderFieldMappings;
GO

CREATE PROCEDURE dbo.Sp_GetOrderFieldMappings
    @p_CustomerID VARCHAR(50) = NULL   -- NULL returns every customer, for caching
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CustomerID,
           FieldName,
           ReadPath,
           ISNULL(NULLIF(WritePath, ''), ReadPath) AS WritePath,
           ValueType,
           Priority
    FROM dbo.OrderFieldMappings
    WHERE IsActive = 1
      AND (@p_CustomerID IS NULL OR CustomerID = @p_CustomerID)
    ORDER BY CustomerID, FieldName, Priority;
END
GO

-- Verify
EXEC dbo.Sp_GetOrderFieldMappings;
