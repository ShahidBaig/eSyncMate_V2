DELETE FROM Maps
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(1, '204 Transformation', 1, '{
  "CarrierCode": "#valueof($.Content[?(@.Name==''B2'')].Content[1].E)",
  "ShipmentId": "#valueof($.Content[?(@.Name==''B2'')].Content[3].E)",
  "Purpose": "#valueof($.Content[?(@.Name==''B2A'')].Content[0].E)",
  "ReferenceNo": "#valueof($.Content[?(@.Name==''L11'')].Content[0].E)",
  "DocumentDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.Content[?(@.Name==''G62'')].Content[1].E),MM/dd/yyyy)",
  "BillToParty": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BT,1)",
  "EquipmentNo": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValueWithoutMatch,#valueof($.Content[?(@.Name==''L_N7'')]),N7,N7,1)",
  "StopOffs": {
    "#loop($.Content[?(@.Name==''L_S5'')])": {
      "LineNo": "#currentvalueatpath($.Content[?(@.Name==''S5'')].Content[0].E)",
      "ReasonCode": "#currentvalueatpath($.Content[?(@.Name==''S5'')].Content[1].E)",
      "Weight": "#currentvalueatpath($.Content[?(@.Name==''S5'')].Content[2].E)",
      "WeightUnitCode": "#currentvalueatpath($.Content[?(@.Name==''S5'')].Content[3].E)",
      "TotalUnits": "#currentvalueatpath($.Content[?(@.Name==''S5'')].Content[4].E)",
      "TotalUnitsCode": "#currentvalueatpath($.Content[?(@.Name==''S5'')].Content[5].E)",
      "ShipperNo": {
        "#loop($.Content[?(@.Name==''L11'')])": "#currentvalueatpath($.Content[0].E)"
      },
      "PickupDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findTagValue,#currentvalueatpath($.Content[?(@.Name==''G62'')]),2,I,1),MM/dd/yyyy)",
      "PickupTime": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatTime,#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findTagValue,#currentvalueatpath($.Content[?(@.Name==''G62'')]),2,I,3),HH:mm:ss)",
      "DeliverDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findTagValue,#currentvalueatpath($.Content[?(@.Name==''G62'')]),2,G,1),MM/dd/yyyy)",
      "DeliverTime": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatTime,#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findTagValue,#currentvalueatpath($.Content[?(@.Name==''G62'')]),2,G,3),HH:mm:ss)",
      "ShipFromName": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N1,0,SF,1)",
      "ShipFromCode": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N1,0,SF,3)",
      "ShipFromAddress": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N3,0,SF,0)",
      "ShipFromCity": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,SF,0)",
      "ShipFromState": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,SF,1)",
      "ShipFromZip": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,SF,2)",
      "ShipFromCountry": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,SF,3)",
      "ConsigneeName": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N1,0,CN,1)",
      "ConsigneeCode": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N1,0,CN,3)",
      "ConsigneeAddress": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N3,0,CN,0)",
      "ConsigneeCity": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,CN,0)",
      "ConsigneeState": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,CN,1)",
      "ConsigneeZip": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,CN,2)",
      "ConsigneeCountry": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagValue,#currentvalueatpath($.Content[?(@.Name==''L_N1_1'')]),N1,N4,0,CN,3)",
      "Packages": {
        "#loop($.Content[?(@.Name==''L_L5'')])": {
          "LineNo": "#currentvalueatpath($.Content[?(@.Name==''L5'')].Content[0].E)",
          "LadingNo": "#currentvalueatpath($.Content[?(@.Name==''L5'')].Content[1].E)",
          "Weight": "#currentvalueatpath($.Content[?(@.Name==''AT8'')].Content[2].E)",
          "WeightUnitCode": "#currentvalueatpath($.Content[?(@.Name==''AT8'')].Content[1].E)",
          "TotalUnits": "#currentvalueatpath($.Content[?(@.Name==''AT8'')].Content[4].E)",
          "ReleaseNumber": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findLoopTagByMatchValue,#currentvalueatpath($.Content[?(@.Name==''L_G61'')]),L11,L11,1,RE,1,0)"
        }
      }  
    }
  }  
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(2, '990 Transformation', 2, '{
	"StartNodes": [
		{ "Name": "B1", "Data": [ "#valueof($.CarrierCode)", "#valueof($.ShipmentId)", "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.DocumentDate),yyyyMMdd)", "@STATUS@" ] },
		{ "Name": "N9", "Data": [ "CN", "#valueof($.ReferenceNo)" ] },
		{ "Name": "G62", "Data": [ "07", "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.getCurrentDate)", "W", "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.getCurrentTime)", "LT" ] },
		{ "Name": "V9", "Data": [ "@STATUSCODE@","","","","","","","@STATUSCODE2@" ] },
	]
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(3, '214 Transformation', 3, '{
	"StartNodes": [
		{ "Name": "B10", "Data": [ "#valueof($.ReferenceNo)", "#valueof($.ShipmentId)", "#valueof($.CarrierCode)" ] },
		{ "Name": "L11", "Data": [ "#valueof($.ShipmentId)", "BN" ] },
		{ "Name": "L11", "Data": [ "#valueof($.ShipperNo)", "SI" ] },
		{ "Name": "N1", "Data": [ "SH", "#valueof($.ShipFromName)", "93", "#valueof($.ShipFromCode)" ] },
		{ "Name": "N3", "Data": [ "#valueof($.ShipFromAddress)" ] },
		{ "Name": "N4", "Data": [ "#valueof($.ShipFromCity)","#valueof($.ShipFromState)", "#valueof($.ShipFromZip)", "#valueof($.ShipFromCountry)" ] },
		{ "Name": "N1", "Data": [ "CN", "#valueof($.ConsigneeName)", "93", "#valueof($.ConsigneeCode)" ] },
		{ "Name": "N3", "Data": [ "#valueof($.ConsigneeAddress)" ] },
		{ "Name": "N4", "Data": [ "#valueof($.ConsigneeCity)","#valueof($.ConsigneeState)", "#valueof($.ConsigneeZip)", "#valueof($.ConsigneeCountry)" ] },
	],
	"Packs": {
		"#loop($.Packages)": {
			"Data": [
				{ "Name": "LX", "Data": [ "#currentvalueatpath($.LineNo)" ] },
				{ "Name": "AT7", "Data": [ "@TRACKSTATUS@", "NS", "", "", "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.getCurrentDate)", "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.getCurrentTime)", "LT" ] },
				{ "Name": "MS1", "Data": [ "@CITY@", "@STATE@", "@COUNTRY@" ] },
				{ "Name": "MS2", "Data": [ "@CARRIERCODE@", "@EQUIPMENTNO@ ] },
				{ "Name": "L11", "Data": [ "#currentvalueatpath($.ReleaseNumber)", "RE" ] },
				{ "Name": "AT8", "Data": [ "G", "#currentvalueatpath($.WeightUnitCode)", "#currentvalueatpath($.Weight)", "#currentvalueatpath($.TotalUnits)" ] }
			]
		}
	},
}', 1)
GO

