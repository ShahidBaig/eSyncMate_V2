DELETE FROM Maps
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(1, 'Order Transformation', 1, '{
  "order" : {
    "header" : {
        "CustomerID": "@CUSTOMERID@",
        "CustomerPO": "#valueof($.order_number)",
        "AddressName": "#valueof($.addresses.shipping_address.first_name)",
        "CompanyName": "#valueof($.addresses.shipping_address.first_name)",
        "ShipToCode": "",
        "Address1": "#valueof($.addresses.shipping_address.address1)",
        "Address2": "#valueof($.addresses.shipping_address.address2)",
        "City": "#valueof($.addresses.shipping_address.city)",
        "State": "#valueof($.addresses.shipping_address.state)",
        "Zip": "#valueof($.addresses.shipping_address.postal_code)",
        "Country": "#valueof($.addresses.shipping_address.country_code)",
        "Phone": "#ifcondition(#length(#valueof($.addresses.shipping_address.phone_numbers)),0,,#valueof($.addresses.shipping_address.phone_numbers[0].number))",
        "OrderTakenBy": "API",
        "Instructions": "#ifcondition(#length(#valueof($.other_info)),0,,#substring(#valueof($.other_info[0].value),0,100))",
        "ShipViaCode": "FEDX",
        "OrderDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.order_date),MM/dd/yyyy)",
        "ShipDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.requested_shipment_date),MM/dd/yyyy)",
        "CancelDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.requested_shipment_date),MM/dd/yyyy)",
        "ExternalID": "#valueof($.id)"
    },
    "detail": {
        "#loop($.order_lines)": {
            "ItemID": "#currentvalueatpath($.external_id)",
            "OrderQty": "#currentvalueatpath($.quantity)",
            "UnitPrice": "#currentvalueatpath($.unit_price)",
            "Discount": "#currentvalueatpath($.total_item_discount)",
            "WHSID": "",
            "Remarks": "#ifcondition(#length(#currentvalueatpath($.other_info)),0,,#substring(#currentvalueatpath($.other_info[0].value),0,100))",
            "ETA_Date": "",
            "TaxAmount": ""
        }
    }
  }  
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(2, 'Shipment Transformation', 2, '{
    "items": {
        "#loop($.Output.TrackingInfo)": {
            "level_of_service": "#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX HOME DELIVERY,FH,#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX GROUND,G2,FH))",
            "order_line_number": "#currentvalueatpath($.OrderLineNo)",
            "quantity": "#currentvalueatpath($.ShippedQty)",
            "shipped_date": "#currentvalueatpath($.ShippedDate)",
            "shipping_method": "",
            "shipping_method": "#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX HOME DELIVERY,FedExGroundHomeDelivery,#ifcondition(#currentvalueatpath($.ShippingMethod),FEDEX GROUND,FedExGround,FedExGround))",
            "tracking_number": "#currentvalueatpath($.TrackingNo)"
        }
    }
}', 1)
GO
