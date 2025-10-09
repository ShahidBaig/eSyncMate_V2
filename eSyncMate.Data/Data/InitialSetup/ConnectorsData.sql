DELETE FROM Connectors 
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (1, 1, 'Create Order', '{
  "BaseUrl": "https://6198403.restlets.api.netsuite.com/app/site/hosting/",
  "Url": "restlet.nl",
  "Method": "POST",
  "Headers": [
    {
      "Name": "Host",
      "Value": "6198403.restlets.api.netsuite.com"
    },
    {
      "Name": "Content-Type",
      "Value": "application/json"
    }
  ],
  "Parmeters": [
    {
      "Name": "script",
      "Value": "1168"
    },
    {
      "Name": "deploy",
      "Value": "1"
    }
  ],
  "BodyFormat": "JSON",
  "ConsumerKey": "75fd391bed60ac6f9544267044544eba3cf2a828f700064bdd75e9ded4b0a266",
  "ConsumerSecret": "79017bcbe52c2ad8bb18aebe1bd9da45998c034151c2d0e5fd1a1af83393ee0f",
  "Token": "655063da3a0282aa64603249da3b2d4d585b3a458871f440883a4bd0dba69411",
  "TokenSecret": "92b2fd5f13d17120c1b9c2ea2edca1f2b02f1e058efc93edd4312748b23d7f68",
  "Realm": "6198403"
}', 1)


INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (2, 2, 'Create ASN', '{
  "BaseUrl": "https://6198403.restlets.api.netsuite.com/app/site/hosting/",
  "Url": "restlet.nl",
  "Method": "GET",
  "Headers": [
    {
      "Name": "Host",
      "Value": "6198403.restlets.api.netsuite.com"
    },
    {
      "Name": "Content-Type",
      "Value": "application/json"
    }
  ],
  "Parmeters": [
    {
      "Name": "script",
      "Value": "1166"
    },
    {
      "Name": "deploy",
      "Value": "1"
    },
    {
      "Name": "id",
      "Value": "1393"
    },
    {
      "Name": "customerPO",
      "Value": "@@CUSTOMERPO@@"
    }
  ],
  "BodyFormat": "JSON",
  "ConsumerKey": "75fd391bed60ac6f9544267044544eba3cf2a828f700064bdd75e9ded4b0a266",
  "ConsumerSecret": "79017bcbe52c2ad8bb18aebe1bd9da45998c034151c2d0e5fd1a1af83393ee0f",
  "Token": "655063da3a0282aa64603249da3b2d4d585b3a458871f440883a4bd0dba69411",
  "TokenSecret": "92b2fd5f13d17120c1b9c2ea2edca1f2b02f1e058efc93edd4312748b23d7f68",
  "Realm": "6198403"
}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (3, 2, 'Mark for ASN', '{
  "BaseUrl": "https://6198403.restlets.api.netsuite.com/app/site/hosting/",
  "Url": "restlet.nl",
  "Method": "PUT",
  "Headers": [
    {
      "Name": "Host",
      "Value": "6198403.restlets.api.netsuite.com"
    },
    {
      "Name": "Content-Type",
      "Value": "application/json"
    }
  ],
  "Parmeters": [
    {
      "Name": "script",
      "Value": "1167"
    },
    {
      "Name": "deploy",
      "Value": "1"
    }
  ],
  "BodyFormat": "JSON",
  "ConsumerKey": "75fd391bed60ac6f9544267044544eba3cf2a828f700064bdd75e9ded4b0a266",
  "ConsumerSecret": "79017bcbe52c2ad8bb18aebe1bd9da45998c034151c2d0e5fd1a1af83393ee0f",
  "Token": "655063da3a0282aa64603249da3b2d4d585b3a458871f440883a4bd0dba69411",
  "TokenSecret": "92b2fd5f13d17120c1b9c2ea2edca1f2b02f1e058efc93edd4312748b23d7f68",
  "Realm": "6198403"
}', 1)

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (4, 3, 'Create Invoice', '{
  "BaseUrl": "https://6198403.restlets.api.netsuite.com/app/site/hosting/",
  "Url": "restlet.nl",
  "Method": "POST",
  "Headers": [
    {
      "Name": "Host",
      "Value": "6198403.restlets.api.netsuite.com"
    },
    {
      "Name": "Content-Type",
      "Value": "application/json"
    }
  ],
  "Parmeters": [
    {
      "Name": "script",
      "Value": "1190"
    },
    {
      "Name": "deploy",
      "Value": "1"
    }
  ],
  "BodyFormat": "JSON",
  "ConsumerKey": "75fd391bed60ac6f9544267044544eba3cf2a828f700064bdd75e9ded4b0a266",
  "ConsumerSecret": "79017bcbe52c2ad8bb18aebe1bd9da45998c034151c2d0e5fd1a1af83393ee0f",
  "Token": "655063da3a0282aa64603249da3b2d4d585b3a458871f440883a4bd0dba69411",
  "TokenSecret": "92b2fd5f13d17120c1b9c2ea2edca1f2b02f1e058efc93edd4312748b23d7f68",
  "Realm": "6198403"
}', 1)
GO

INSERT INTO Connectors (Id, TypeId, [Name], [Data], CreatedBy)
VALUES (5, 4, 'Create 855', '{
  "BaseUrl": "https://6198403.restlets.api.netsuite.com/app/site/hosting/",
  "Url": "restlet.nl",
  "Method": "GET",
  "Headers": [
    {
      "Name": "Host",
      "Value": "6198403.restlets.api.netsuite.com"
    },
    {
      "Name": "Content-Type",
      "Value": "application/json"
    }
  ],
  "Parmeters": [
    {
      "Name": "script",
      "Value": "1166"
    },
    {
      "Name": "deploy",
      "Value": "1"
    },
    {
      "Name": "id",
      "Value": "1491"
    },
    {
      "Name": "customerPO",
      "Value": "@@CUSTOMERPO@@"
    }
  ],
  "BodyFormat": "JSON",
  "ConsumerKey": "75fd391bed60ac6f9544267044544eba3cf2a828f700064bdd75e9ded4b0a266",
  "ConsumerSecret": "79017bcbe52c2ad8bb18aebe1bd9da45998c034151c2d0e5fd1a1af83393ee0f",
  "Token": "655063da3a0282aa64603249da3b2d4d585b3a458871f440883a4bd0dba69411",
  "TokenSecret": "92b2fd5f13d17120c1b9c2ea2edca1f2b02f1e058efc93edd4312748b23d7f68",
  "Realm": "6198403"
}', 1)
GO