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
    [FileName]
FROM 
    RankedOrderData
WHERE 
    rn = 1 OR [Type] != 'ERPASN-ERR' 
GO


