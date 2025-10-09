

CREATE TABLE ApplicationSettings
(
    ID				INT IDENTITY(1,1) PRIMARY KEY,       
    TagName			VARCHAR(250) NULL ,
	TagValue		VARCHAR(250) NULL ,
	CreatedDate		DATETIME NOT NULL,
	CreatedUser		INT NOT NULL
);

GO

INSERT INTO ApplicationSettings (TagName,TagValue,CreatedDate,CreatedUser)
VALUES ('RouteExecutePriceData',23,GETDATE(),1)

GO

INSERT INTO ApplicationSettings (TagName,TagValue,CreatedDate,CreatedUser)
VALUES ('ProductMarkErrorResolve','',GETDATE(),1)

GO

