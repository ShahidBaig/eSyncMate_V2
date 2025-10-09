
INSERT INTO ConnectorTypes(Id,Name,Party,CreatedDate,CreatedBy)
VALUES ((SELECT MAX(ID) + 1 FROM ConnectorTypes),'Walmart','Walmart',GETDATE(),1)
GO