CREATE TABLE [dbo].[PurchaseOrders](
	[Id] [int] NOT NULL,
	[Status] [varchar](50) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[InboundEDIId] [int] NULL,
	[OrderDate] [datetime] NOT NULL,
	[PONumber] [varchar](50) NULL,
	[SupplierID] [varchar](50) NULL,
	[LocationID] [varchar](50) NULL,
	[POStatus] [varchar](50) NULL,
	[VCreatedDate]	DATETIME NULL,
	[VExpectedDate] DATETIME NULL,
	[ShipToName] [nvarchar](250) NULL,
	[ShipToAddress1] [nvarchar](250) NULL,
	[ShipToAddress2] [nvarchar](250) NULL,
	[ShipToCity] [nvarchar](250) NULL,
	[ShipToState] [nvarchar](50) NULL,
	[ShipToZip] [nvarchar](50) NULL,
	[ShipToCountry] [nvarchar](250) NULL,
	[ShipToEmail] [nvarchar](50) NULL,
	[ShipToPhone] [nvarchar](50) NULL,
	[BillToName] [nvarchar](250) NULL,
	[BillToAddress1] [nvarchar](250) NULL,
	[BillToAddress2] [nvarchar](250) NULL,
	[BillToCity] [nvarchar](250) NULL,
	[BillToState] [nvarchar](50) NULL,
	[BillToZip] [nvarchar](50) NULL,
	[BillToCountry] [nvarchar](250) NULL,
	[BillToEmail] [nvarchar](50) NULL,
	[BillToPhone] [nvarchar](50) NULL,
	[BuyerId] [nvarchar](250) NULL,
	[BuyerName] [nvarchar](250) NULL,
	[BuyerAddress1] [nvarchar](250) NULL,
	[BuyerAddress2] [nvarchar](250) NULL,
	[BuyerCity] [nvarchar](250) NULL,
	[BuyerState] [nvarchar](50) NULL,
	[BuyerZip] [nvarchar](50) NULL,
	[BuyerCountry] [nvarchar](250) NULL,
	[BuyerEmail] [nvarchar](50) NULL,
	[BuyerPhone] [nvarchar](50) NULL,
	[IsStoreOrder] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PurchaseOrders] ADD  DEFAULT (getdate()) FOR [OrderDate]
GO

ALTER TABLE [dbo].[PurchaseOrders] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO


