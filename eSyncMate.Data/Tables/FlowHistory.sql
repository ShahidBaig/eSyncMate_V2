CREATE TABLE FlowHistory (
    Id				BIGINT IDENTITY(1,1) NOT NULL,
    FlowId			BIGINT               NOT NULL,
	FlowDetailId    BIGINT               NOT NULL,
    RouteId			INT                  NOT NULL,
    UserId			INT                  NOT NULL,
    FlowStatus		NVARCHAR(50)         NULL,
    JobId			NVARCHAR(50)         NULL,
    CreatedDate		DATETIME             NOT NULL	CONSTRAINT DF_FlowHistory_CreatedDate DEFAULT GETDATE(),
    CreatedBy		INT                  NOT NULL,
    CONSTRAINT PK_FlowHistory PRIMARY KEY CLUSTERED (Id ASC)
);
