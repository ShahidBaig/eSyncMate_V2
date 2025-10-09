CREATE VIEW [dbo].[VW_CarrierLoadTenderData] AS 
SELECT CLTD.Id, CLTD.CarrierLoadTenderId, CLTD.[Type], CLTD.[Data], CLTD.CreatedDate, 
	CLTD.[Type] + '-' + REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, GETDATE(), 127), '-', ''), ':', ''), ' ', ''), '.', '') + 
	CASE WHEN CLTD.[Type] LIKE '%EDI%' THEN '.edi' WHEN CLTD.[Type] LIKE '%JSON%' OR CLTD.[Type] LIKE '%RESPONSE%' OR CLTD.[Type] LIKE '%-NS%' OR CLTD.[Type] LIKE '%Fields%' THEN '.json' ELSE '.txt' END [FileName]
FROM CarrierLoadTenderData CLTD WITH (NOLOCK)
	INNER JOIN CarrierLoadTender CLT WITH (NOLOCK) ON CLTD.CarrierLoadTenderId = CLT.Id
GO