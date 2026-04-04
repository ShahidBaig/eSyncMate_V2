DROP VIEW [dbo].[VW_OrderData]  
GO



CREATE VIEW [dbo].[VW_OrderData] AS
WITH RankedOrderData AS (
    SELECT
        OD.Id,
        OD.OrderId,
        OD.[Type],
        OD.[Data],
        OD.CreatedDate,
        OD.[Type] + '-' + OD.OrderNumber + '-' + REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, GETDATE(), 127), '-', ''), ':', ''), ' ', ''), '.', '') +
        CASE
            WHEN OD.[Type] LIKE '%EDI%' THEN '.edi'
            WHEN OD.[Type] LIKE '%JSON%' OR OD.[Type] LIKE '%RESPONSE%' OR OD.[Type] LIKE '%-NS%' OR OD.[Type] LIKE '%Fields%' OR
                 OD.[Type] LIKE '%-ACK%' OR OD.[Type] LIKE '%-RES%' OR OD.[Type] LIKE '%-SNT%' OR OD.[Type] LIKE '%-ERROR%' OR
                 OD.[Type] LIKE '%-ERR%' THEN '.json'
            ELSE '.txt'
        END AS [FileName],
        CASE OD.[Type]
            WHEN 'API-JSON'          THEN 1
            WHEN 'API-JSON-REQ'      THEN 2
            WHEN 'API-JSON-RES'      THEN 3
            WHEN 'API-ACK-SNT'       THEN 4
            WHEN 'API-ACK'           THEN 5
            WHEN 'ACK-ERR'           THEN 6
            WHEN 'ERP-SNT'           THEN 7
            WHEN 'ERP-JSON'          THEN 8
            WHEN 'ERP-ERROR'         THEN 9
            WHEN 'ERP-ERR'           THEN 9
            WHEN 'ERP.ERROR'         THEN 9
            WHEN 'ERP.ERR'           THEN 9
            WHEN 'ERPASN-JSON'       THEN 10
            WHEN 'ERPASN-ERR'        THEN 11
            WHEN 'ASN-SNT'           THEN 12
            WHEN 'ASN-RES'           THEN 13
            WHEN 'ASN-ERR'           THEN 14
            WHEN 'ERPCancelOrder-JSON' THEN 15
            WHEN 'ERPCANLN-JSON'     THEN 16
            WHEN 'ERPCANLN-ERR'      THEN 17
            ELSE 50
        END AS TypeSortOrder,
        ROW_NUMBER() OVER (PARTITION BY OD.OrderId, OD.[Type] ORDER BY OD.CreatedDate DESC) AS rn
    FROM
        OrderData OD WITH (NOLOCK)
    INNER JOIN
        Orders O WITH (NOLOCK) ON OD.OrderId = O.Id

)
SELECT
    Id,
    OrderId,
    [Type],
    [Data],
    CreatedDate,
    [FileName],
    TypeSortOrder
FROM
    RankedOrderData
WHERE
    rn = 1 OR [Type] != 'ERPASN-ERR'
GO


