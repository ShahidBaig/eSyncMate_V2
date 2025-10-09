CREATE TABLE Suppliers(
	ID				INT IDENTITY(1,1) PRIMARY KEY,
	SupplierID		VARCHAR(50) NOT NULL,
	Name			VARCHAR(500),
	Status			VARCHAR(50),
	CreatedDate		DATETIME NOT NULL,			
	CreatedBy		INT NOT NULL,			
	ModifiedDate	DATETIME NULL,			
	ModifiedBy		INT  NULL			
)

GO





INSERT INTO Suppliers(SupplierID,Name,Status,CreatedDate,CreatedBy)
VALUES ('Majid321','Majid321','Active',GETDATE(),1),
('Bilal123','Bilal123','Active',GETDATE(),1),
('Tanveer9966','Tanveer9966','Active',GETDATE(),1)
GO