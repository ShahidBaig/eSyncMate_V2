DROP TABLE [Temp_SCSInventoryFeed]

GO


CREATE TABLE [dbo].[Temp_SCSInventoryFeed](
	[CustomerID] [varchar](50) NULL,
	[ItemId] [varchar](50) NULL,
	[CustomerItemCode] [varchar](50) NULL,
	[ETA_Date] [nvarchar](30) NULL,
	[ETA_Qty] [int] NULL,
	[Total_ATS] [int] NULL,
	[ATS_L10] [int] NULL,
	[ATS_L21] [int] NULL,
	[ATS_L28] [int] NULL,
	[ATS_L30] [int] NULL,
	[ATS_L34] [int] NULL,
	[ATS_L35] [int] NULL,
	[ATS_L36] [int] NULL,
	[ATS_L37] [int] NULL,
	[ATS_L40] [int] NULL,
	[ATS_L41] [int] NULL,
	[ATS_L55] [int] NULL,
	[ATS_L60] [int] NULL,
	[ATS_L70] [int] NULL,
	[ATS_L91] [int] NULL,
	[ATS_L29] [int] NULL,
	[ATS_L65] [int] NULL,
	[CurrentPage] [int] NULL,
	[TotalPages] [int] NULL,
	[CreatedDate] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL
) ON [PRIMARY]
GO




SELECT * INTO zdt_SCSInventoryFeed_3062025 FROM SCSInventoryFeed

GO

DROP TABLE SCSInventoryFeed 

GO


CREATE TABLE [dbo].[SCSInventoryFeed](
	[CustomerID] [varchar](50) NOT NULL,
	[ItemId] [varchar](50) NOT NULL,
	[CustomerItemCode] [varchar](50) NULL,
	[ETA_Date] [nvarchar](30) NULL,
	[ETA_Qty] [int] NULL,
	[Total_ATS] [int] NULL,
	[ATS_L10] [int] NULL,
	[ATS_L21] [int] NULL,
	[ATS_L28] [int] NULL,
	[ATS_L30] [int] NULL,
	[ATS_L34] [int] NULL,
	[ATS_L35] [int] NULL,
	[ATS_L36] [int] NULL,
	[ATS_L37] [int] NULL,
	[ATS_L40] [int] NULL,
	[ATS_L41] [int] NULL,
	[ATS_L55] [int] NULL,
	[ATS_L60] [int] NULL,
	[ATS_L70] [int] NULL,
	[ATS_L91] [int] NULL,
	[ATS_L29] [int] NULL,
	[ATS_L65] [int] NULL,
	[Status] [varchar](25) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL
	
 CONSTRAINT [PK_SCSInventoryFeed] PRIMARY KEY CLUSTERED 
(
	[CustomerID] ASC,
	[ItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[SCSInventoryFeed] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO

ALTER TABLE [dbo].[SCSInventoryFeed] ADD  DEFAULT (getdate()) FOR [ModifiedDate]
GO



INSERT INTO SCSInventoryFeed(CustomerID,ItemId,CustomerItemCode,ETA_Date,ETA_Qty,Total_ATS,ATS_L10,ATS_L21,ATS_L28,ATS_L30,ATS_L34,ATS_L35,
ATS_L36,ATS_L37,ATS_L40,ATS_L41,ATS_L55,ATS_L60,ATS_L70,ATS_L91,ATS_L29,ATS_L65,Status,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy)

SELECT CustomerID,ItemId,CustomerItemCode,ETA_Date,ETA_Qty,Total_ATS,ATS_L10,ATS_L21,ATS_L28,ATS_L30,ATS_L34,ATS_L35,
ATS_L36,ATS_L37,ATS_L40,ATS_L41,ATS_L55,ATS_L60,ATS_L70,ATS_L91,0,0,Status,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy 
FROM zdt_SCSInventoryFeed_3062025

GO

