
INSERT INTO [dbo].[Warehouses] 
(
	[WHSId],
    [Name], 
	[Address1],
	[Address2],
	[City],
	[State],
	[Zip],
	[Country],
	[Contact],
    [CreatedBy], 
    [CreatedDate], 
    [ModifiedBy], 
    [ModifiedDate]
) 
VALUES 
(
	'123ABC',
    'SurgiMac NY',                 
    '10 Kees Pl',   
	'Address2 XYZ',  
    'Merrick',                     
    'NY',  
    '11566-3658', 
	'US',
    '+16464214136',                
    1,                             
    GETDATE(),                     
    NULL,                          
    NULL                                     
);

INSERT INTO [dbo].[Warehouses] 
(
	[WHSId],
    [Name], 
	[Address1],
	[Address2],
	[City],
	[State],
	[Zip],
	[Country],
	[Contact],
    [CreatedBy], 
    [CreatedDate], 
    [ModifiedBy], 
    [ModifiedDate]
) 
VALUES 
(
	'1234',
    'SurgiMac Utah',                 
    '870 W 410 N', 
	'Address2 ABC',  
    'London',                     
    'UT',                          
    '84042', 
	'US',
    '6464214136',                
    1,                             
    GETDATE(),                     
    NULL,                          
    NULL                 
);