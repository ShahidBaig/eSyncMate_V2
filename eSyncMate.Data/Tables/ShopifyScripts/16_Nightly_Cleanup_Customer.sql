/*==============================================================================
  16 - Shopify (Task 00006): add the customer to the nightly cleanup

  The inventory routes write to two tables of the customer's own (script 02 creates them):
      SCSInventoryFeedData_<id>     request / response per item per batch
      SCSInventoryFeed_<id>_Log     snapshot per item per batch

  The nightly cleanup deletes the old rows of these tables, but only for the customers in a list that is
  written inside each of these two procedures:
      Sp_NightlyCleanup_DeleteCustomerFeedData
      Sp_NightlyCleanup_DeleteCustomerLogTables

  A new customer is not in the list, so its two tables would grow for ever. This script adds
  ('<@ErpCustomerID>') at the end of the list in both procedures.

  Patches the LIVE definition (OBJECT_DEFINITION), not a repo copy: every other line stays as it is.
  Run it as often as needed: a procedure that already names the customer is skipped.
  A server without these procedures (no nightly cleanup there) is reported and nothing is changed.
  Needs script 02 (the two tables). No secret here.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';   -- same as script 02
/*==============================================================================*/

DECLARE @p TABLE (Seq INT IDENTITY(1,1), SpName SYSNAME);
INSERT INTO @p (SpName) VALUES ('Sp_NightlyCleanup_DeleteCustomerFeedData'), ('Sp_NightlyCleanup_DeleteCustomerLogTables');

DECLARE @i INT = 1, @Sp SYSNAME, @Def NVARCHAR(MAX), @Marker INT, @Values INT, @End INT, @List NVARCHAR(MAX), @Msg NVARCHAR(400);
DECLARE @Create INT, @Alter INT;

BEGIN TRY
    IF @ErpCustomerID LIKE '%[^A-Za-z0-9_]%' OR LTRIM(RTRIM(@ErpCustomerID)) = ''
        THROW 51600, '@ErpCustomerID may only hold letters, digits and _ (it is part of the table names).', 1;

    WHILE @i <= (SELECT MAX(Seq) FROM @p)
    BEGIN
        SELECT @Sp = SpName FROM @p WHERE Seq = @i;
        SET @Def = OBJECT_DEFINITION(OBJECT_ID('dbo.' + @Sp));

        IF OBJECT_ID('dbo.' + @Sp, 'P') IS NULL
            PRINT @Sp + ' is not on this server (no nightly cleanup here) - nothing changed.';
        ELSE IF @Def IS NULL
        BEGIN
            SET @Msg = @Sp + ': cannot read the definition (the login lacks VIEW DEFINITION). Run as a privileged login.';
            THROW 51601, @Msg, 1;
        END
        ELSE IF CHARINDEX('''' + @ErpCustomerID + '''', @Def) > 0
            PRINT @Sp + ' already has ' + @ErpCustomerID + ' - nothing changed.';
        ELSE
        BEGIN
            -- the list:  INSERT INTO @Customers (CustomerID) VALUES ('A'), ('B'), ... ;
            SET @Marker = CHARINDEX('INSERT INTO @Customers', @Def);
            SET @Values = CASE WHEN @Marker > 0 THEN CHARINDEX('VALUES', @Def, @Marker) ELSE 0 END;
            SET @End    = CASE WHEN @Values > 0 THEN CHARINDEX(';', @Def, @Values) ELSE 0 END;
            SET @List   = CASE WHEN @End > 0 THEN SUBSTRING(@Def, @Values + LEN('VALUES'), @End - @Values - LEN('VALUES')) ELSE NULL END;

            -- only one list, and nothing but ('...') entries between VALUES and the ;
            IF @Marker = 0 OR @Values = 0 OR @End = 0
               OR CHARINDEX('INSERT INTO @Customers', @Def, @Marker + 1) > 0
               OR @List NOT LIKE '%(''%'')%'
               OR REPLACE(REPLACE(REPLACE(REPLACE(@List, CHAR(13), ''), CHAR(10), ''), CHAR(9), ''), ' ', '') LIKE '%[^A-Za-z0-9_,()'']%'
            BEGIN
                SET @Msg = @Sp + ': the customer list does not have the expected shape. Add (''' + @ErpCustomerID + ''') to its list by hand.';
                THROW 51602, @Msg, 1;
            END

            SET @Def = STUFF(@Def, @End, 0, ', (''' + @ErpCustomerID + ''')');

            -- CREATE or ALTER header -> ALTER (only the first keyword)
            SET @Create = PATINDEX('%CREATE%PROC%', @Def);
            SET @Alter  = PATINDEX('%ALTER%PROC%', @Def);

            -- "CREATE OR ALTER PROCEDURE" is already fine as it is
            IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter) AND SUBSTRING(@Def, @Create, 15) <> 'CREATE OR ALTER'
                SET @Def = STUFF(@Def, @Create, LEN('CREATE'), 'ALTER');

            EXEC sp_executesql @Def;

            PRINT 'Added ' + @ErpCustomerID + ' to ' + @Sp + '.';
        END

        SET @i += 1;
    END

    /*--- Verification ---------------------------------------------------------*/
    SELECT p.SpName,
           CASE WHEN OBJECT_ID('dbo.' + p.SpName, 'P') IS NULL THEN 'Procedure not on this server'
                WHEN CHARINDEX('''' + @ErpCustomerID + '''', OBJECT_DEFINITION(OBJECT_ID('dbo.' + p.SpName))) > 0 THEN 'Customer in the list'
                ELSE 'Customer MISSING' END AS Result
      FROM @p p ORDER BY p.Seq;

    -- the two tables the cleanup will purge (script 02 creates them)
    SELECT t.TableName, CASE WHEN OBJECT_ID('dbo.' + t.TableName, 'U') IS NULL THEN 'MISSING - run script 02' ELSE 'exists' END AS TableState
      FROM (VALUES ('SCSInventoryFeedData_' + @ErpCustomerID), ('SCSInventoryFeed_' + @ErpCustomerID + '_Log')) t (TableName);
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  MANUAL FALLBACK (the script stopped with error 51602): open the procedure and add the customer at the end
  of the list that follows  INSERT INTO @Customers (CustomerID) VALUES  -  e.g.  ..., ('MOR4778'), ('WKE1000');
==============================================================================*/
