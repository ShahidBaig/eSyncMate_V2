/*
    102 — Repair stored order payloads that will not parse
    -------------------------------------------------------------------------------------------
    An Amazon product Title carries an unescaped inch mark, so the WHOLE stored API-JSON is
    unreadable and the order can never be placed:

        "Title":"SAFAVIEH Lighting Orianna Table Lamp, 25"CeramicBlue/WhiteTBL4104A","IsGift":...

    This does to the stored rows what OrderPayloadRepair.TryRepair does at run time: it
    neutralises the quotes INSIDE the Title value and leaves every other byte alone. Title is
    decorative — no code and no ERP map reads it — so nothing downstream changes.

    The end of the value is located from the "IsGift" key that always follows it, never from the
    broken title text, and a payload that does not become valid JSON is left untouched.

    HOW TO RUN
      1. Leave @WhatIf = 1 and run it. Nothing is written; the result grid lists what WOULD change.
      2. Set @WhatIf = 0 and run it again to apply.
      3. Re-process the affected orders.

    SCOPING — @FromDate and the status list keep this off the whole table. OrderData.Data is
    NVARCHAR(MAX) and ISJSON() reads the entire blob, so widening either is expensive.
*/

SET NOCOUNT ON;

DECLARE @WhatIf    BIT      = 1;                             -- 1 = report only, 0 = apply
DECLARE @FromDate  DATETIME = DATEADD(DAY, -90, GETDATE());  -- how far back to look, by OrderDate
DECLARE @MaxTitles INT      = 100;                           -- guard: most Titles in one payload

IF OBJECT_ID('tempdb..#Candidates') IS NOT NULL
    DROP TABLE #Candidates;

CREATE TABLE #Candidates
(
    DataId      INT            NOT NULL PRIMARY KEY,
    OrderId     INT            NOT NULL,
    OrderNumber NVARCHAR(100)  NULL,
    OrderStatus NVARCHAR(50)   NULL,
    Repaired    BIT            NOT NULL DEFAULT (0),
    Note        NVARCHAR(200)  NULL
);

INSERT INTO #Candidates (DataId, OrderId, OrderNumber, OrderStatus)
SELECT d.Id, o.Id, o.OrderNumber, o.Status
FROM   Orders    o
JOIN   OrderData d ON d.OrderId = o.Id AND d.Type = 'API-JSON'
WHERE  o.Status IN ('ERROR', 'ACKERROR', 'ASNERROR', 'New', 'InProgress')
AND    o.OrderDate >= @FromDate
AND    ISJSON(d.Data) = 0;

PRINT 'Unreadable API-JSON payloads found: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

DECLARE @DataId  INT,
        @Json    NVARCHAR(MAX),
        @Work    NVARCHAR(MAX),
        @Seg     NVARCHAR(MAX),
        @Value   NVARCHAR(MAX),
        @Clean   NVARCHAR(MAX),
        @Pos     INT,
        @Loop    INT,
        @KeyAt   INT,
        @ColonAt INT,
        @OpenAt  INT,
        @ValueAt INT,
        @GiftAt  INT,
        @SegLen  INT,
        @RevAt   INT,
        @CloseAt INT;

DECLARE l_Broken CURSOR LOCAL FAST_FORWARD FOR
    SELECT DataId FROM #Candidates;

OPEN l_Broken;
FETCH NEXT FROM l_Broken INTO @DataId;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @Json = Data FROM OrderData WHERE Id = @DataId;

    SET @Work = @Json;
    SET @Pos  = 1;
    SET @Loop = 0;

    WHILE @Loop < @MaxTitles
    BEGIN
        SET @Loop = @Loop + 1;

        -- "Title" ... : ... "  — the whitespace is stepped over, because a payload that has been
        -- through a JObject round trip is indented ("Title": "...") rather than compact.
        SET @KeyAt = CHARINDEX('"Title"', @Work, @Pos);
        IF @KeyAt = 0 BREAK;

        SET @ColonAt = CHARINDEX(':', @Work, @KeyAt + 7);
        IF @ColonAt = 0 BREAK;

        SET @OpenAt = CHARINDEX('"', @Work, @ColonAt + 1);
        IF @OpenAt = 0 BREAK;

        SET @ValueAt = @OpenAt + 1;

        SET @GiftAt = CHARINDEX('"IsGift"', @Work, @ValueAt);
        IF @GiftAt = 0 BREAK;

        -- Everything between the value and "IsGift" ends with  ",  and then any whitespace, so the
        -- LAST  ",  in that stretch is the quote that closes the Title.
        SET @Seg    = SUBSTRING(@Work, @ValueAt, @GiftAt - @ValueAt);
        SET @SegLen = LEN(@Seg + N'|') - 1;
        SET @RevAt  = CHARINDEX(',"', REVERSE(@Seg));

        IF @RevAt = 0 BREAK;

        SET @CloseAt = @ValueAt + @SegLen - @RevAt - 1;
        IF @CloseAt <= @ValueAt BREAK;

        SET @Value = SUBSTRING(@Work, @ValueAt, @CloseAt - @ValueAt);
        SET @Clean = LTRIM(RTRIM(REPLACE(REPLACE(@Value, '\', ' '), '"', ' in ')));

        SET @Work = STUFF(@Work, @ValueAt, @CloseAt - @ValueAt, @Clean);
        SET @Pos  = @ValueAt + (LEN(@Clean + N'|') - 1);
    END

    IF @Work <> @Json AND ISJSON(@Work) = 1
    BEGIN
        IF @WhatIf = 0
            UPDATE OrderData SET Data = @Work WHERE Id = @DataId;

        UPDATE #Candidates
        SET    Repaired = 1,
               Note     = CASE WHEN @WhatIf = 1 THEN 'Would repair - Title quotes neutralised'
                               ELSE 'Repaired - Title quotes neutralised' END
        WHERE  DataId = @DataId;
    END
    ELSE
    BEGIN
        UPDATE #Candidates
        SET    Note = CASE WHEN @Work = @Json THEN 'No Title/IsGift pair found - not this fault'
                           ELSE 'Still not valid JSON after the edit - left untouched' END
        WHERE  DataId = @DataId;
    END

    FETCH NEXT FROM l_Broken INTO @DataId;
END

CLOSE l_Broken;
DEALLOCATE l_Broken;

SELECT OrderNumber, OrderStatus, DataId, Repaired, Note
FROM   #Candidates
ORDER  BY Repaired DESC, OrderNumber;

DECLARE @Total INT, @Fixed INT;

SELECT @Total = COUNT(*),
       @Fixed = SUM(CASE WHEN Repaired = 1 THEN 1 ELSE 0 END)
FROM   #Candidates;

PRINT 'Repairable: ' + CAST(ISNULL(@Fixed, 0) AS VARCHAR(10))
    + ' of ' + CAST(ISNULL(@Total, 0) AS VARCHAR(10))
    + CASE WHEN @WhatIf = 1 THEN '  (report only - nothing written)' ELSE '  (applied)' END;

DROP TABLE #Candidates;
