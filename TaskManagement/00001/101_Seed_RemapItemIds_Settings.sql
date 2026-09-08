/*==============================================================================
  Task   : 00001 - Re-Map Item IDs action for Amazon error orders
  Script : 101_Seed_RemapItemIds_Settings.sql
  Date   : 2026-09-03
  Purpose: The ONLY script this task needs. Two ApplicationSettings rows.

  1. ShowRemapItemIdsActionInRoles - feature toggle for the "Re-Map Item IDs"
     button on the Orders grid AND the Dashboard drilldown. Both screens read it
     live (GET api/Role/getActionColumnVisibility), so the action can be switched
     on and off WITHOUT a redeployment. Seeded '0' = hidden, matching the
     Resubmit / Re-Transmit rollout. A missing row also reads as hidden.

  2. AmazonCustomerIds - the ERPCustomerIDs the action applies to. Used by BOTH
     the UI (to show the button) and OrdersController.RemapItemIds (to accept the
     call), so there is one list, not two. Onboarding another Amazon customer is
     a one-row UPDATE - no redeploy, no restart.

     Deliberately an explicit list rather than derived data:
       - Customers.Marketplace is free text ('AMAZON SELLER CENTRAL',
         'Amazon Pacific Rugs', 'AMAZON MASTER WEAVERS OF AMERICA').
       - The Amazon GetOrders route (TypeId 49) would exclude AMA1000, which has
         no routes configured yet but is still an Amazon customer.

  To turn the action ON:
      UPDATE ApplicationSettings SET TagValue = '1'
      WHERE TagName = 'ShowRemapItemIdsActionInRoles';

  Who sees it: admins. The action has NO per-role permission column by design -
  the UI falls back to admin and the API's permission middleware has no keyword
  branch for this path, so it resolves to CanView.

  Idempotent: safe to run multiple times (won't overwrite an existing value).
==============================================================================*/

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = 'ShowRemapItemIdsActionInRoles')
    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES ('ShowRemapItemIdsActionInRoles', '1', GETDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = 'AmazonCustomerIds')
    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES ('AmazonCustomerIds', 'AMA1005,AMA1000,AMA1006,MOR4778', GETDATE(), 1);

-- Verify
SELECT TagName, TagValue FROM dbo.ApplicationSettings
WHERE TagName IN ('ShowResubmitActionInRoles', 'ShowReTransmitActionInRoles',
                  'ShowRemapItemIdsActionInRoles', 'AmazonCustomerIds');

-- Sanity: do these ERPCustomerIDs exist?
SELECT ERPCustomerID, Name, Marketplace
FROM dbo.Customers
WHERE ERPCustomerID IN ('AMA1005', 'AMA1000', 'AMA1006', 'MOR4778');
GO
