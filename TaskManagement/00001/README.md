# Task 00001 — Re-Map Item IDs (Amazon error orders)

**Branch:** `MB-00010`  ·  **Commit prefix:** `#00001:`  ·  **Status:** built — Processor + UI compile clean; 1 script, not yet run

## What the action does

An Amazon order arrives with a Seller SKU that is not yet in `SCSInventoryFeed`,
so ingestion falls back to writing the raw SKU into `OrderDetail.ItemID`. The ERP
rejects it and the order sits in **ERROR**.

**Re-Map Item IDs** re-runs the same lookup that ingestion does — Seller SKU →
`SCSInventoryFeed.CustomerItemCode` → `ItemId` — and writes the result into the `ItemID`
field of every line in the order's stored API-JSON, which is what the ERP is built from.
The order can then be re-processed.

**Amazon customers only.**

## Naming (agreed 2026-09-03)

| Thing | Name |
|---|---|
| Grid button / tooltip | `Re-Map Item IDs` |
| Endpoint | `POST api/orders/remapItemIds` |
| Entity method | `OrderData.UpdateData(Id, Data)` |
| Role permission | none — admin-only by design |
| Visibility flag | `ShowRemapItemIdsActionInRoles` |

Matches the existing `Re-Process Order` / `Resubmit to ERP` / `Re-Transmit The ASN` family.

## Where the action appears

| | |
|---|---|
| **Screens** | Orders grid **and** the Dashboard drilldown dialog |
| **Row status** | `ERROR`, `ACKERROR`, `ASNERROR` only |
| **Customers** | `AMA1005`, `AMA1000`, `AMA1006`, `MOR4778` — matched on `ERPCustomerID` |
| **Users** | admins (no per-role permission, by design) |
| **Toggle** | `ShowRemapItemIdsActionInRoles = '1'` |

## How "Amazon only" is enforced

The customer list is an **explicit `ERPCustomerID` list** in
`ApplicationSettings.AmazonCustomerIds`, served to the UI on the same
`getActionColumnVisibility` call the grids already make. One list, read by both sides:

| Layer | Gate | Authority? |
|---|---|---|
| Server (`OrdersController.RemapItemIds`) | `CustomerName` ∈ `AmazonCustomerIds` | **Yes** |
| UI (button visibility) | `row.erpCustomerID` ∈ the same list | No — cosmetic only |

Read live from the table on each call, so onboarding another Amazon customer is a one-row
`UPDATE` — no redeploy, no Processor restart.

**Why not derive it?** Two ways were tried and both are wrong:

- `Customers.Marketplace` is free text — `AMAZON SELLER CENTRAL`, `Amazon Pacific Rugs`,
  `AMAZON MASTER WEAVERS OF AMERICA`. A substring match works today and breaks on the first
  customer named differently.
- The Amazon GetOrders route (`TypeId = 49`) looks rigorous but **excludes `AMA1000`**, which
  has no routes configured at all in dev yet is on the list.

No `VW_Orders` change is needed either way, so no `sp_refreshview` risk is taken.

An unreadable or missing `AmazonCustomerIds` row leaves the set empty, which **closes** the
action rather than opening it to every customer.

## Scripts — run order

| # | Script | What it does | Run on dev | Run on prod |
|---|---|---|---|---|
| 101 | `101_Seed_RemapItemIds_Settings.sql` | Seeds `ShowRemapItemIdsActionInRoles = '0'` and `AmazonCustomerIds = 'AMA1005,AMA1000,AMA1006,MOR4778'` | ☐ | ☐ |
| — | `Verify_AffectedOrders.sql` | Read-only sizing / test queries. Not a deployment script. | ☐ | ☐ |

**One script, and it is idempotent.** Numbering continues the global sequence in
`scripts\eSyncmateScripts\` (100 was the last one used, in `2026-08-31\`).

After running it, switch the action on:

```sql
UPDATE ApplicationSettings SET TagValue = '1'
WHERE TagName = 'ShowRemapItemIdsActionInRoles';
```

No redeploy or restart — the Orders screen reads the flag live.

### Why there is no permission script

An earlier draft added a grantable `CanRemapItemIds` role permission (RoleMenus +
UserMenus columns, `VW_UserMenus`, `Sp_GetUserPermissions`). **Removed — it is not
required**, and it carried real risk: it depended on script 76 never being deployed to
client prod, and it rebuilt a view and an SP whose live text could not be read back
(the MCP login has no `VIEW DEFINITION` grant).

Nothing blocks the feature without it:

- **UI** — `permissions.canRemapItemIds` is undefined, `?? isAdmin` applies, so admins
  see the button and other roles do not.
- **API** — `PermissionMiddleware.ResolvePermission` matches on URL keywords
  (`retransmit`, `resubmit`, `delete`, `update`/`edit`/`save`, `add`/`create`/`insert`).
  `orders/remapitemids` hits none of them and falls through to `return perm.CanView`,
  so the endpoint is not rejected.

**Net effect: the action is admin-only.** If it ever needs to be grantable per role,
that script comes back and needs three more things with it — the middleware keyword
branch (`if (lower.Contains("remap")) return perm.CanRemapItemIds;`, without which the
column is never enforced), the Role Management grid column, and
`AuthenticationController.GetUserMenuTree`.

## Rollback

```sql
DELETE FROM dbo.ApplicationSettings WHERE TagName = 'ShowRemapItemIdsActionInRoles';
```

Or simply set it back to `'0'` — the button disappears and nothing else changes. No
schema was touched, so there is nothing else to undo.

## What the endpoint does

`POST api/orders/remapItemIds?OrderId=&CustomerName=`

1. Rejects the customer unless its `ERPCustomerID` is in `ApplicationSettings.AmazonCustomerIds`.
2. Resolves the order's `OrderNumber`, then loads its `OrderData` row with
   `Type = 'API-JSON'` **by OrderNumber** — some OrderData rows are written before the
   OrderId is known (see `UpdateOrderDataOrderID`).
3. Walks **every** line in `OrderDetail.payload.OrderItems` — single-line and multi-line
   orders take the same path, no special casing.
4. One SQL lookup for the whole order: `SCSInventoryFeed` on `CustomerID` +
   `CustomerItemCode IN (…the order's Seller SKUs)`.
5. Writes the resolved `ItemId` into each line's `ItemID` field and saves the payload back.
6. Returns a per-line verdict: `UPDATED`, `ALREADY CORRECT`, `NOT IN INVENTORY FEED`,
   `AMBIGUOUS - maps to X, Y`, `NO SELLER SKU ON LINE`.

Clicking the button runs it straight away — **there is no confirmation prompt**. The action only
replaces an Item ID it resolved from the feed, so there is nothing to warn about up front; the
result dialog reports what happened afterwards. The order is **not** re-processed — that stays a
separate click.

## What the user sees when a SKU does not match

An unmatched line is **never blanked** — the endpoint only assigns `ItemID` when it resolves a
single feed row, so the previous value stays exactly as it was. The result dialog then says so:

- a per-line table showing `Seller SKU`, the Item ID (struck-through → new when it changed, or
  the kept value tagged `unchanged` when it did not), and a plain-English verdict;
- summary chips: *n updated · n already correct · n not found · n ambiguous*;
- an amber banner whenever anything failed to match: **"n line(s) could not be matched and kept
  their existing Item ID. Nothing was cleared."**

Verdicts are spelled out for the grid user: `NOT IN INVENTORY FEED` → "SKU not in inventory
feed", `AMBIGUOUS …` → "Several Item IDs match this SKU", `NO SELLER SKU ON LINE` → "No Seller
SKU on this line".

The grid refreshes only when at least one line actually changed.

## Code changes — done

| Layer | File | Change |
|---|---|---|
| Entity | `eSyncMate.DB/Entities/OrderData.cs` | `UpdateData(Id, Data)` — parameterised, the payload can contain apostrophes |
| Controller | `eSyncMate.Processor/Controllers/OrdersController.cs` | `RemapItemIds` endpoint after `ReTransmitASN` |
| Visibility | `eSyncMate.Processor/Controllers/RoleController.cs` | `showRemapItemIds` on `getActionColumnVisibility` |
| Service | `UI/src/app/services/api.service.ts` | `RemapItemIds()`; `canRemapItemIds` on `getMenuPermissions` |
| Model | `UI/src/app/models/models.ts` | `canRemapItemIds` on `UserMenuItem` |
| Dialog | `UI/src/app/orders/remap-result-dialog/` | the only popup — per-line result table, app header pattern, brand orange |

| Grid | `UI/src/app/orders/orders.component.ts` / `.html` | button, `isAmazonOrder()` gate, flags, `RemapItemIds()` |
| Drilldown | `UI/src/app/dashboard/orders-drilldown-dialog/*.ts` / `.html` | same button, reusing the existing `isRowErrorStatus()` |

Both screens share the two dialogs and the one customer list — nothing is duplicated except
the `isAmazonOrder()` one-liner.

Processor builds clean (0 errors). UI builds clean (0 errors).

## Code changes — still to do

| Layer | File | Change |
|---|---|---|
| Ingestion fix | `AmazonGetOrdersRoute.cs:133-137` | empty `catch {}` on the feed load — add a log line |

Optional, only if the action must ever be grantable per role: the `CanRemapItemIds`
permission script plus the middleware keyword branch, the Role Management column, and
`AuthenticationController.GetUserMenuTree`. See *Why there is no permission script* above.

See `Analysis.md` for the full trace and the open decisions.
