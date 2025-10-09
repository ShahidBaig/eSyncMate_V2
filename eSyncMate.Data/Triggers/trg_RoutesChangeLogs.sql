IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_RoutesChangeLogs')
BEGIN
    EXEC('CREATE TRIGGER dbo.trg_RoutesChangeLogs
       ON dbo.Routes
       AFTER INSERT, DELETE, UPDATE
    AS 
    BEGIN
        SET NOCOUNT ON;

        DECLARE @ActionType NVARCHAR(10);
        DECLARE @ModUserId INT;

        IF EXISTS (SELECT * FROM INSERTED) AND EXISTS (SELECT * FROM DELETED)
        BEGIN
            SET @ActionType = ''UPDATE'';
            SELECT @ModUserId = ModifiedBy FROM INSERTED
        END
        ELSE IF EXISTS (SELECT * FROM INSERTED)
        BEGIN
            SET @ActionType = ''INSERT'';
            SELECT @ModUserId = CreatedBy FROM INSERTED
        END
        ELSE IF EXISTS (SELECT * FROM DELETED)
        BEGIN
            SET @ActionType = ''DELETE'';
            SELECT @ModUserId = ModifiedBy FROM DELETED
        END

        INSERT INTO dbo.RoutesLogs (ActionType, [Id], [TypeId], [Status], [SourcePartyId], [DestinationPartyId],[SourceConnectorId],[DestinationConnectorId],[MapId],[PartyGroupId] ,[CreatedDate], [CreatedBy], [ModifiedDate], [ModifiedBy],[FrequencyType],[StartDate],[EndDate],[RepeatCount],[WeekDays],[OnDay],[ExecutionTime],[JobID],[Name],[RouteGroup])
        SELECT 
            @ActionType,
            COALESCE(i.Id, d.Id),
            COALESCE(i.TypeId, d.TypeId),
            COALESCE(i.Status, d.Status),
            COALESCE(i.SourcePartyId, d.SourcePartyId),
            COALESCE(i.DestinationPartyId, d.DestinationPartyId),
            COALESCE(i.SourceConnectorId, d.SourceConnectorId),
            COALESCE(i.DestinationConnectorId, d.DestinationConnectorId),
            COALESCE(i.MapId, d.MapId),
            COALESCE(i.PartyGroupId, d.PartyGroupId),
            COALESCE(i.CreatedDate, d.CreatedDate),
            COALESCE(i.CreatedBy, d.CreatedBy),
            GETDATE(), 
            @ModUserId, 
            COALESCE(i.FrequencyType, d.FrequencyType),
            COALESCE(i.StartDate, d.StartDate),
            COALESCE(i.EndDate, d.EndDate),
            COALESCE(i.RepeatCount, d.RepeatCount),
            COALESCE(i.WeekDays, d.WeekDays),
            COALESCE(i.OnDay, d.OnDay),
            COALESCE(i.ExecutionTime, d.ExecutionTime),
            COALESCE(i.JobID, d.JobID),
            COALESCE(i.Name, d.Name),
			COALESCE(i.RouteGroup, d.RouteGroup),
            COALESCE(i.CustomerName, d.CustomerName)
        FROM INSERTED i
        FULL OUTER JOIN DELETED d ON i.Id = d.Id;
    END')
END
GO
