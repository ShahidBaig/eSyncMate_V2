IF NOT EXISTS (
    SELECT 1 
    FROM ApplicationSettings 
    WHERE TagName = 'BulkUpdatePricesFromCustomerPortal_TAR6266PAH'
)
BEGIN
    INSERT INTO ApplicationSettings (TagName, TagValue, CreatedUser, CreatedDate)
    VALUES ('BulkUpdatePricesFromCustomerPortal_TAR6266PAH', '73', 1, GETDATE())

    PRINT 'Data added successfully'
END
ELSE
BEGIN
    PRINT 'Already exists'
END
GO