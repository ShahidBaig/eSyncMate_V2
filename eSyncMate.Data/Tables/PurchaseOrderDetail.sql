

CREATE TABLE [dbo].[PurchaseOrderDetail](
	[Id] [int] NOT NULL,
	[OrderId] [int] NOT NULL,
	ItemID VARCHAR(100),
	UPC VARCHAR(100),
	Description VARCHAR(1000),
	[LineNo] [int] NOT NULL,
	[UnitPrice] [real] NULL,
	[OrderQty] [int] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL,
	[Status] [varchar](50) NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PurchaseOrderDetail] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO

ALTER TABLE [dbo].[PurchaseOrderDetail] ADD  CONSTRAINT [POD_CreatedBy]  DEFAULT ((1)) FOR [CreatedBy]
GO


