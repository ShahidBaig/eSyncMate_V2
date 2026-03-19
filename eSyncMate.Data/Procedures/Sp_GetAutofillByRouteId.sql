CREATE PROCEDURE [dbo].[Sp_GetAutofillByRouteId]
    @CustomerID NVARCHAR(50),
    @RouteId    INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @CustomerID IS NULL OR LTRIM(RTRIM(@CustomerID)) = ''
        BEGIN
            RAISERROR('CustomerID is required and cannot be empty.', 16, 1);
            RETURN;
        END

        IF @RouteId IS NULL OR @RouteId <= 0
        BEGIN
            RAISERROR('RouteId must be a valid positive integer.', 16, 1);
            RETURN;
        END

        SELECT TOP 1
            fd.FrequencyType,
            fd.StartDate,
            fd.EndDate,
            fd.RepeatCount,
            fd.WeekDays,
            fd.OnDay,
            fd.ExecutionTime
        FROM FlowDetails fd
        INNER JOIN Flows  f ON f.Id  = fd.FlowId
        INNER JOIN Routes r ON r.Id  = fd.RouteId
        WHERE f.CustomerID = @CustomerID
          AND fd.RouteId   = @RouteId
        ORDER BY fd.Id DESC;
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('No autofill data found for the given CustomerID and RouteId.', 1, 1);
        END

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        DECLARE @ErrorLine     INT            = ERROR_LINE();
        DECLARE @ErrorProc     NVARCHAR(200)  = ISNULL(ERROR_PROCEDURE(), 'Sp_GetAutofillByRouteId');
        RAISERROR('Procedure: %s | Line: %d | Error: %s',@ErrorSeverity,@ErrorState,@ErrorProc,@ErrorLine,@ErrorMessage
        );
    END CATCH
END