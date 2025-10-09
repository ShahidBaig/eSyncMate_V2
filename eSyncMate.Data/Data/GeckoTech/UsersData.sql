DELETE FROM Users
GO

INSERT INTO Users (Id, FirstName, LastName, Email, Password, Status, CreatedBy, CreatedDate, UserType)
VALUES (1, 'Admin', 'Admin', 'admin@geckotech.com', 'GDEBC', 'Active', 1, GETDATE(), 'Admin')
GO