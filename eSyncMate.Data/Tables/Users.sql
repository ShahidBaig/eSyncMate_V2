CREATE TABLE [dbo].[Users]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [FirstName] NVARCHAR(50) NOT NULL, 
    [LastName] NVARCHAR(50) NOT NULL,
    [Email] NVARCHAR(50) NOT NULL,
    [Mobile] [nvarchar](50) NULL,
    [Password] NVARCHAR(100) NOT NULL, 
    [Status] NVARCHAR(50) NOT NULL,
    [CreatedDate] DATETIME NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NULL DEFAULT 1,
    [UserType] [nvarchar](20) NOT NULL,
    [Company] [nvarchar](50) NULL, 
    [CustomerName] NVARCHAR(500) NULL,
    [IsSetupAllowed] BIT NULL DEFAULT 0,
    [USERID] NVARCHAR(100) NOT NULL
)
