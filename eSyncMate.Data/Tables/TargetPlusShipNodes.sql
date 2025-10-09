
CREATE TABLE [dbo].[TargetPlusShipNodes](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[WHSID] [varchar](10) NULL,
	[ShipNode] [nvarchar](500) NULL
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO




INSERT INTO [TargetPlusShipNodes](WHSID,ShipNode)
VALUES ('L10','zfxfmh'),
('L21','wmbfak'),
('L28','6wgvxt'),
('L35','lgpwfk'),
('L36','0eg9sv'),
('L37','oxwbut'),
('L40','d248jw'),
('L41','a012gx'),
('L55','52uosu'),
('L60','l2wacg'),
('L70','boot48')

GO



	
	
	
	
	
	
	
	
	
	
	
