/*
===============================================================================
  Script:      82_Add_Index_OrderData_Type_OrderId_CreatedDate.sql
  Description: Narrow covering index for Sp_GetDashboardStats.

               The dashboard aggregate seeks OrderData by Type over a date range
               and takes MAX(CreatedDate) per OrderId. Neither existing index
               serves that shape well:

                 IX_OrderData_OrderId_Type  key (OrderId, Type)  include (Data)
                 IX_OrderData_Type          key (Type)           include (OrderId,
                                            OrderNumber, Data, Status, CreatedDate,
                                            CreatedBy)

               Both INCLUDE Data (NVARCHAR(MAX)), which bloats the leaf level.
               Per-order seeks are served; a range-wide GROUP BY is not — and the
               dashboard auto-refreshes every 15 minutes.

               This index is deliberately narrow: three key columns, no INCLUDE.

  Note:        Run this BEFORE 81 goes live if the table is large. Capture the
               plan and duration for Sp_GetDashboardStats before and after.
  Target:      All environments
  Date:        2026-07-30
===============================================================================
*/

/*
  Key order matters and an earlier revision of this script got it wrong.

  The query is:   WHERE Type IN (...) AND CreatedDate >= @from  GROUP BY OrderId

  With (Type, OrderId, CreatedDate) the date predicate is NOT a seek — the engine
  seeks Type and then has to scan every row of that Type across all history. That
  is fatal for ERPASN-ERR, which alone is ~225,000 rows per WEEK on production.

  With (Type, CreatedDate) it seeks Type and range-scans only the requested dates.
  OrderId is INCLUDEd so the aggregate is still covered.
*/

-- Drop the earlier, wrongly-ordered index if a previous run created it
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_OrderData_Type_OrderId_CreatedDate'
             AND object_id = OBJECT_ID('dbo.OrderData'))
BEGIN
    PRINT 'Dropping IX_OrderData_Type_OrderId_CreatedDate (wrong key order) ...';
    DROP INDEX IX_OrderData_Type_OrderId_CreatedDate ON dbo.OrderData;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_OrderData_Type_CreatedDate_OrderId'
                 AND object_id = OBJECT_ID('dbo.OrderData'))
BEGIN
    PRINT 'Creating IX_OrderData_Type_CreatedDate_OrderId on dbo.OrderData ...';

    CREATE NONCLUSTERED INDEX IX_OrderData_Type_CreatedDate_OrderId
        ON dbo.OrderData ([Type], CreatedDate)
        INCLUDE (OrderId);

    PRINT 'Created.';
END
ELSE
BEGIN
    PRINT 'IX_OrderData_Type_CreatedDate_OrderId already exists — nothing to do.';
END
GO
