DELETE FROM Connectors 
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (1, 1, 'CLT Table', '{"ConnectivityType":"SqlServer","CommandType":"Query","Command":"","KeyFieldName":"","DataFieldName":"","CustomerID":"","JsonDataCollectionName":"","ConnectionString":"Server=rxo.geckotech.com.mx;Database=EDIProcessor;UID=sa;PWD=Gecko8079;"}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (2, 3, 'FREJI 204 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Outbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"/Inbox","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"FREJIESPECIALIZADOS","ConsumerSecret":"5GN!0c1@","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (3, 3, 'Logística 204 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Outbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"/Inbox","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"SERVICIOCORPLOGISTICA","ConsumerSecret":"dP9876w(","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (4, 3, 'Saucedo 204 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Outbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"/Inbox","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"SERVICIOCORPORATIVO","ConsumerSecret":"2m8C\\B4o","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (5, 3, 'FREJI 990 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Inbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"FREJIESPECIALIZADOS","ConsumerSecret":"5GN!0c1@","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (6, 3, 'Logística 990 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Inbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"SERVICIOCORPLOGISTICA","ConsumerSecret":"dP9876w(","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (7, 3, 'Saucedo 990 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Inbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"SERVICIOCORPORATIVO","ConsumerSecret":"2m8C\\B4o","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (8, 3, 'FREJI 214 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Inbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"FREJIESPECIALIZADOS","ConsumerSecret":"5GN!0c1@","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (9, 3, 'Logística 214 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Inbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"SERVICIOCORPLOGISTICA","ConsumerSecret":"dP9876w(","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (10, 3, 'Saucedo 214 Files', '{"ConnectivityType":"Rest","BaseUrl":"/Inbox","AuthType":"SFTP","Host":"sfgdev.rxo.com","Url":"","Method":"","Headers":[],"Parmeters":[],"BodyFormat":"","ConsumerKey":"SERVICIOCORPORATIVO","ConsumerSecret":"2m8C\\B4o","Token":"","TokenSecret":"","Realm":"","CustomerID":""}', 1)
GO