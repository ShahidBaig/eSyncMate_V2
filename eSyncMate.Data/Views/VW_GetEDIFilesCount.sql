CREATE VIEW VW_GetEDIFilesCount
AS
	SELECT CustomerID, SUM(COUNT_204) COUNT_204,DocDate, SUM(COUNT_214) COUNT_214, MAX(Name) AS CustomerName
	FROM (
			SELECT CustomerId,COUNT(DISTINCT CT.ShipmentID) AS COUNT_204, CONVERT(DATE,CreatedDate) AS DocDate, 0 AS COUNT_214
			FROM CarrierLoadTender CT WITH(NOLOCK)
			GROUP BY CustomerId,CONVERT(DATE,CreatedDate)
			UNION ALL
			SELECT CL.CustomerId AS CustomerId, 0 as COUNT_204, CONVERT(DATE,CT.CreatedDate) AS DocDate, COUNT(*) AS COUNT_214
			FROM CarrierLoadTenderData CT WITH (NOLOCK)
				INNER JOIN CarrierLoadTender CL WITH (NOLOCK) ON CT.CarrierLoadTenderId = CL.Id
			WHERE CT.Type = '214-EDI'
			GROUP BY CustomerId,CONVERT(DATE,CT.CreatedDate)
		) S
			INNER JOIN Customers C WITH(NOLOCK) ON S.CustomerId = C.Id
	GROUP BY CustomerId,DocDate
GO