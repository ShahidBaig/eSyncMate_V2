CREATE VIEW [dbo].[VW_SCS_CustomerProductCatalogData]
AS
    SELECT CPD.[Id], CPD.[ProductId], CPD.[Data], CPD.[CreatedDate], CPD.[CreatedBy], CPD.[ModifiedDate], CPD.[ModifiedBy],  CPC.[ItemTypeName] ,
    CPD.Type + '-' + REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, CPC.[CreatedDate], 127), '-', ''), ':', ''), ' ', ''), '.', '') + 
    CASE WHEN CPD.Type LIKE '%EDI%' THEN '.edi'
         -- The payload decides the extension: REQ-SNT holds a JSON body on a create/update but a
         -- plain URL on a status/unlist call, so the Type alone cannot tell them apart.
         WHEN LEFT(LTRIM(H.DataHead), 1) IN ('{', '[') THEN '.json'
         WHEN CPD.Type LIKE '%JSON%' OR CPD.Type LIKE '%RESPONSE%' OR CPD.Type LIKE '%-NS%' OR CPD.Type LIKE '%Fields%' THEN '.json' ELSE '.txt' END [FileName],
     -- Per-operation types written by ProductCatalogRoute / ProductCatalogStatusRoute.
     CASE WHEN  CPD.Type = 'PRD-REQ'  THEN 'Product create/update sent to marketplace'
                WHEN CPD.Type = 'PRD-RSP'  THEN 'Product create/update response from marketplace'
                WHEN CPD.Type = 'PRD-ERR'  THEN 'Error while sending product data'
                WHEN CPD.Type = 'UNL-REQ'  THEN 'Unlist request sent to marketplace'
                WHEN CPD.Type = 'UNL-RSP'  THEN 'Unlist response from marketplace'
                WHEN CPD.Type = 'UNL-ERR'  THEN 'Error while unlisting product'
                WHEN CPD.Type = 'STA-RSP'  THEN 'Listing status received from marketplace'
                WHEN CPD.Type = 'STA-ERR'  THEN 'Error while checking listing status'
                WHEN CPD.Type = 'LOG-REQ'  THEN 'Logistics update sent to marketplace'
                WHEN CPD.Type = 'LOG-RSP'  THEN 'Logistics update response from marketplace'
                WHEN CPD.Type = 'LOG-ERR'  THEN 'Error while updating logistics'
                -- Predecessors of the types above. Rows written before the split keep these, so they
                -- are captioned too. REQ-JSON is the awkward one: ProductCatalogStatusRoute used it
                -- for the logistics REQUEST while ProductCatalogRoute used it for the create/update
                -- RESPONSE, and the payload shape is the only way to tell those two apart.
                WHEN CPD.Type = 'REQ-SNT'  THEN 'Product data sent to marketplace'
                WHEN CPD.Type = 'REQ-JSON' AND H.DataHead LIKE '%"url"%' AND H.DataHead LIKE '%"method"%'
                                           THEN 'Logistics update sent to marketplace'
                WHEN CPD.Type = 'REQ-JSON' THEN 'Response received from marketplace'
                WHEN CPD.Type = 'REQ-ERR'  THEN 'Error while sending product data'
                WHEN CPD.Type = 'RSP-JSON' THEN 'Listing status received from marketplace'
                WHEN CPD.Type = 'RSP-ERR'  THEN 'Error while checking listing status'
        ELSE
        CPD.Type
        END Type,
    CPC.ItemID
    FROM SCS_CustomerProductCatalogData CPD WITH (NOLOCK) 
    INNER JOIN SCS_CustomerProductCatalog CPC WITH (NOLOCK) ON CPD.ProductId = CPC.ProductId
    -- Data is NVARCHAR(MAX); reading a fixed 300-char head keeps this to the first LOB page and
    -- gives both the extension test and the transient-error test one value to work from.
    CROSS APPLY (SELECT DataHead = LOWER(SUBSTRING(CPD.[Data], 1, 300))) H
    -- Day granularity on purpose: a log row written earlier on the same day as the item's
    -- ModifiedDate must still show. A raw datetime compare silently hides it.
    WHERE CONVERT(DATE,CPD.CreatedDate) >= ISNULL(CONVERT(DATE,CPC.ModifiedDate), CONVERT(DATE,CPC.CreatedDate))
    -- 'Internal' is eSyncMate's own validation note, not marketplace traffic. It stays out of the
    -- Product Data popup; the grid tooltip still reads it straight from the base table.
    AND CPD.Type <> 'Internal'
    -- Transport failures (timeout, gateway, rate limit, empty body) tell the user nothing about
    -- their product, so they are hidden while the marketplace's real rejection reasons stay.
    -- The route classifies these the same way in code -- CommonUtils.IsTransientResponse -- but it
    -- writes both kinds as REQ-ERR with only the response body, so matching the text is all the
    -- view can do. A transient body worded differently will still slip through.
    AND NOT (CPD.Type IN ('PRD-ERR', 'UNL-ERR', 'STA-ERR', 'LOG-ERR', 'REQ-ERR', 'RSP-ERR')
             AND (CPD.[Data] IS NULL
                  OR DATALENGTH(CPD.[Data]) = 0
                  OR H.DataHead LIKE '%upstream%'
                  OR H.DataHead LIKE '%timeout%'
                  OR H.DataHead LIKE '%timed out%'
                  OR H.DataHead LIKE '%gateway%'
                  OR H.DataHead LIKE '%service unavailable%'
                  OR H.DataHead LIKE '%temporarily unavailable%'
                  OR H.DataHead LIKE '%internal server error%'
                  OR H.DataHead LIKE '%connection%refused%'
                  OR H.DataHead LIKE '%too many requests%'
                  OR H.DataHead LIKE '%rate limit%'))
GO
