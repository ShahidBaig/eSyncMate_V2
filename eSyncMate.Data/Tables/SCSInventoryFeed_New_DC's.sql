ALTER TABLE SCSInventoryFeed
ADD ATS_L56 INT NULL,
    ATS_L57 INT NULL

GO


DROP TABLE [dbo].[Temp_SCSInventoryFeed]

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
	[ATS_L56] [int] NULL,
	[ATS_L57] [int] NULL,
	[CurrentPage] [int] NULL,
	[TotalPages] [int] NULL,
	[CreatedDate] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[ModifiedDate] [datetime2](0) NULL,
	[ModifiedBy] [int] NULL
) ON [PRIMARY]
GO



INSERT INTO TargetPlusShipNodes(WHSID,ShipNode,CustomerID)
VALUES ('L56','9wz55h','TAR6266P'),
	   ('L57','zdq4lf','TAR6266P'),
	   ('L56','rzw5wv','TAR6266PAH'),
	   ('L57','om0vdh','TAR6266PAH')

GO



