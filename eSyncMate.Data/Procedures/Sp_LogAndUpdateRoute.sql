CREATE PROCEDURE Sp_LogAndUpdateRoute
    @FlowId BIGINT,
    @FlowDetailId BIGINT,
    @RouteId INT,
    @UserId INT,
    @FlowStatus NVARCHAR(50),
    @OldJobId NVARCHAR(50),
    @NewJobId NVARCHAR(50),
    @FrequencyType VARCHAR(100),
    @StartDate DATETIME,
    @EndDate DATETIME,
    @RepeatCount INT,
    @WeekDays VARCHAR(250),
    @OnDay VARCHAR(200),
    @ExecutionTime VARCHAR(200)
AS
BEGIN
    -- SET NOCOUNT ON;

    -- 1. Insert into FlowHistory
    INSERT INTO FlowHistory (
        FlowId, 
        FlowDetailId, 
        RouteId, 
        UserId, 
        FlowStatus, 
        JobId, 
        CreatedDate, 
        CreatedBy
    )
    VALUES (
        @FlowId, 
        @FlowDetailId, 
        @RouteId, 
        @UserId, 
        @FlowStatus, 
        @OldJobId, 
        GETDATE(), 
        @UserId
    );

    -- 2. Update Routes table if status is Active
    IF (LOWER(@FlowStatus) = 'active')
    BEGIN
        UPDATE [Routes]
        SET 
            Status = @FlowStatus,
            FrequencyType = @FrequencyType,
            StartDate = @StartDate,
            EndDate = @EndDate,
            RepeatCount = @RepeatCount,
            WeekDays = @WeekDays,
            OnDay = @OnDay,
            ExecutionTime = @ExecutionTime,
            JobID = @NewJobId,
            ModifiedDate = GETDATE(),
            ModifiedBy = @UserId
        WHERE Id = @RouteId;
    END
END
GO
