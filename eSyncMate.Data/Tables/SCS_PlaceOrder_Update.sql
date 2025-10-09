
UPDATE OrderData SET Data = '{
    "order": {
        "header": {
            "CustomerID": "TAR6266P",
            "CustomerPO": "ABC0001",
            "AddressName": "John Wayne",
            "CompanyName": "ABC Company",
            "ShipToCode": "000000",
            "Address1": "House # 2, Street 3",
            "Address2": "Manhattan",
            "City": "New York",
            "State": "NY",
            "Zip": "12802",
            "Country": "USA",
            "Phone": "212-111-2222",
            "OrderTakenBy": "sams",
            "EventID": "",
            "Instructions": "Please inspect all packages before shipping",
            "ShipViaCode": "UPSG",
            "OrderDate": "2024-03-04",
            "ShipDate": "2024-03-04",
            "CancelDate": "2024-05-04",
            "ShippingCost": "25.00",
            "DeliveryTime": "2024-03-04 4:00:00 PM",
            "AddressType": "BUSINESS",
            "SignatureRequired": "Y",
            "IncludeDeclareValue": "Y",
            "ShipComplete": "Y",
            "OrderSource": "IPad",
            "ExternalID": "01234567899"
        },
        "detail": [
            {
                "ItemID": "ABT141A-212",
                "OrderQty": "3",
                "UnitPrice": "200.00",
                "Discount": "15",
                "WHSID": "L21",
                "Remarks": "Ship as soon as possible",
                "ETA_Date": "",
                "TaxAmount": "230.00"
            },
            {
                "ItemID": "ABT141A-214",
                "OrderQty": "1",
                "UnitPrice": "135.00",
                "Discount": "15",
                "WHSID": "L21",
                "Remarks": "Ship as soon as possible",
                "ETA_Date": ""
            }
        ]
    }
}' WHERE OrderId =2866 AND Type = '850-JSON'
--INSERT INTO RouteTypes(Id,Name,Description,CreatedDate,CreatedBy)
--VALUES (9,'SCSPlaceOrder','SCSPlaceOrder',GETDATE(),1)

--GO