/*==============================================================================
  80_DownloadTargetItems_Create_Rest_Connectors.sql
  ------------------------------------------------------------------------------
  Phase 1 : Make the "Download Items Data" (RouteType 31) routes connector-driven
            instead of hardcoding the Target products_catalog API + credentials
            inside DownloadTargetItemsRoute.cs.

  What this script does:
    1. Creates 2 REST destination connectors (TypeId = 6), modelled exactly on
       "TargetPlus Check Products Status" (connector 26 / 89):
         - TargetPlus Download Items Data        -> TAR6266P
         - SEI- TargetPlus Download Items Data    -> TAR6266PAH
       Each holds BaseUrl (seller-specific products_catalog), the 3 auth headers
       (x-api-key / x-seller-id / x-seller-token) and query params
       per_page=1000 & expand=fields. (after_id is added per-page by the code.)
       Data is AES-256 encrypted (ENC:) with the same EncryptionKey as all
       other connectors.

    2. Repoints DestinationConnectorId of both "Download Items Data" routes to
       the new REST connectors. Source connectors (SqlServer) are LEFT UNCHANGED.

  Safe to re-run: connectors are created only if the name does not already exist;
  the route repoint always re-applies. Wrapped in a transaction.

  NOTE: Deploy together with the updated DownloadTargetItemsRoute.cs.
==============================================================================*/

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @tarId INT, @seiId INT;

    -- Connectors.Id is NOT an IDENTITY column -> assign ids manually.
    DECLARE @nextId INT = (SELECT ISNULL(MAX(Id), 0) FROM Connectors);

    /*--- 1. TAR6266P REST connector -----------------------------------------*/
    SELECT @tarId = Id FROM Connectors WHERE Name = 'TargetPlus Download Items Data';

    IF @tarId IS NULL
    BEGIN
        SET @nextId = @nextId + 1;
        SET @tarId  = @nextId;
        INSERT INTO Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
        VALUES (@tarId, 6, 'TargetPlus Download Items Data',
'ENC:pKLcYgjWwGAu10UEBt9zUYWkm1G5qCHmKY6LVjUVj/TFBjIIdfmyGtevmIzSZZ1yDtxbGyXH67q75tELcCA+WCGcq9dVuDL8gPOXdkvY2leEcpHSoBsKsicMm6IqjOjDjucwQEddveq3gQE8J1KI9CqyEJ9M2Yz7I2Bo+2uQTNXRcRqc/p8Q38kPt2VnejszRDbrg7CTk6MwtEP81WIoiZa0fyNWFcikuLjf1+nrlvfznDl/E4PQm8eBj50rVBeZW4Rdfz5/FdPrgHXGL2t0MUsQj2FKz3yoh7pgMzfZ4kJEJ25v2nZhKQiHO7HPPujCWQVFleqARwqu+V1gKBQQ/9+X87CdONfYR1cFJg1jXjgvarFXE+cv2qi8kQy2cc5hU+sgU7jK6CvW2aIwdxwW6gDOWQ92zoqp8FlOv9jFhgsUNvDAXE22YwmcLEm5k6R6Hb1A3sv6kyycTmekWgsPr7oTFPzbDK21nndJbggsv4qWi7qZTHnXTcPeb6g+rGQ483n3OHQME8HqYkNNZ3AZtGP+g2whmoXnKA0OJpGkMGBqrACutN+D8M7E4VoyqrmP5eoaTEoG/nnQmvSGhm+kMBf+K5FESRuhAAsiuHSYINyCkgc0nCqb0T/9p/tvLzCLF/1iEVXx2oJnqGgSNp3yp7kAbMNtxrFB4TN0etTX+OU4Th0NPncnbH/HD2nlw1W13VvK5PRHl/jIopmQ2Lk2LsTkk0AJjewCWGHoOSGu5Pe9xEyGZICH9pQVE7D67aBdj9bYUtk5BvH1vPkjOZMC7eN9JhVOyhN7GrOWkcN71gCjsu7uQo5c4IygCf2A3nYcMsOTn6cx6IixUHGFsDg6fA==',
                GETDATE(), 1);
        PRINT 'Created connector [TargetPlus Download Items Data] Id = ' + CAST(@tarId AS VARCHAR(10));
    END
    ELSE
        PRINT 'Connector [TargetPlus Download Items Data] already exists Id = ' + CAST(@tarId AS VARCHAR(10));

    /*--- 2. TAR6266PAH (SEI) REST connector ---------------------------------*/
    SELECT @seiId = Id FROM Connectors WHERE Name = 'SEI- TargetPlus Download Items Data';

    IF @seiId IS NULL
    BEGIN
        SET @nextId = @nextId + 1;
        SET @seiId  = @nextId;
        INSERT INTO Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
        VALUES (@seiId, 6, 'SEI- TargetPlus Download Items Data',
'ENC:MVlhk5cBjDLZnisZs6Ozi6QvOWstxHxJgt3DzuPJonbWrfZWi+sLXlJ6xPt5+idrHh4qFhbmdYtAr17yjazh6syoVxOb/iLMBknVmEehtSG778zxE03UG2Bbjcqw4IXSg+MZY277bI1IN5+Turqlys3lUbxfQKlJmze02h7zfxTu0Fi/xl/twvMUlP9no6aF65NX5/H1oxmMV2z/8+nlmxfsepjjD5CUtNf8BnSEbP7fbH06bgczEdhjnylhJ8u0sW1rkRPFxEWTmMKLe6I+2jcPFNiWU/bERCn9GOo8bBrh+A6G66XVdFoiWCAD3eInnH6xrKf9m2m5WOVhs2w66hnSrz8ZUIqmKAKDCeAYJijGbpEHQwjnSY0wcS0rRFoAtd8P4R8rRmnGqUiAlACfHoVcGaRh75ZTkQTrDODNLr4u+J4eQx08M2LaPy1H3JRu+gtjtanEyyKIKMnCLNc2XF5QtSGQjj2Z4uTnbVm+sVNV+FwoTQErBThAgYvgDmq4indVWop/OyDfy7SfFOKbE/SMtJziNQEK3+NDyUhGhz1m8R6qdtlJJBzCcO5YGQJjoPaZcKIsaP8KYa5HsURiMo47FiZ5ZPHshbvxgwYdRJT5sZqpVdsgmJ6KWgf0iSB5nrg4YY864qpvJRxSUPwwt2T6ucewXf3Iehh7ewgrk9KbtuZNGNAjc/TENo7fp3XQeRPQuTZZ7n3e5MS1ETCtveMc1xZJVBzNQoJ5g389vbGc8WkBJih7zRfzDkmOj/c9nfbD/BDmqxZnJ61OuZ1sbkv7Ai3QSOfIwXgdVEaKJtCO8DRP7ICAffIFGltdTeF7f7S3HkCdv7t+Rk2Nn3R4UA==',
                GETDATE(), 1);
        PRINT 'Created connector [SEI- TargetPlus Download Items Data] Id = ' + CAST(@seiId AS VARCHAR(10));
    END
    ELSE
        PRINT 'Connector [SEI- TargetPlus Download Items Data] already exists Id = ' + CAST(@seiId AS VARCHAR(10));

    /*--- 3. Repoint route DestinationConnectorId (match by type + customer) --*/
    UPDATE Routes SET DestinationConnectorId = @tarId
    WHERE TypeId = 31 AND CustomerName = 'TAR6266P';
    PRINT 'TAR6266P  Download Items Data routes repointed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    UPDATE Routes SET DestinationConnectorId = @seiId
    WHERE TypeId = 31 AND CustomerName = 'TAR6266PAH';
    PRINT 'TAR6266PAH Download Items Data routes repointed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    /*--- Verification --------------------------------------------------------*/
    SELECT r.Id, r.Name AS RouteName, r.CustomerName,
           r.SourceConnectorId, sc.Name AS SourceConnName,
           r.DestinationConnectorId, dc.Name AS DestConnName
    FROM Routes r
    LEFT JOIN Connectors sc ON r.SourceConnectorId = sc.Id
    LEFT JOIN Connectors dc ON r.DestinationConnectorId = dc.Id
    WHERE r.TypeId = 31;

    COMMIT TRAN;
    PRINT 'Done.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
