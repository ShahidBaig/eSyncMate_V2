DELETE FROM RouteTypes
GO

INSERT INTO RouteTypes (Id, Name, Description, CreatedBy, CreatedDate)
VALUES (100, 'Carrier Load Tender', 'Carrier Load Tender Route', 1, GETDATE())
GO

INSERT INTO RouteTypes (Id, Name, Description, CreatedBy, CreatedDate)
VALUES (101, 'Carrier Load Tender 990', 'Carrier Load Tender 990', 1, GETDATE())
GO

INSERT INTO RouteTypes (Id, Name, Description, CreatedBy, CreatedDate)
VALUES (102, 'Carrier Load Tender 214', 'Carrier Load Tender 214', 1, GETDATE())
GO