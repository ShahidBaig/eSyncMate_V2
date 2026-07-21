# Analysis Report: Route Execution Lock & Flow Coordination — Current State, Issues, and Recommended Changes

**Project:** eSyncMate_V2 (EDI Processing Platform)
**Area:** Inventory route dispatch — `RouteExecutionLock` + Flow Coordination
**Components:** `RouteEngine.ExecuteExternal`, `RouteExecutionLock`, `RouteAbortFlag`, `Sp_IsOtherFlowInventoryRouteRunning`
**Environment:** Production — `EMS`, SQL `ESYNCMATESCHEDULER`
**Date:** 2026-06-30
**Status:** For Senior Review

---

## 1. Executive Summary

Inventory routes (Full Feed, Differential, Upload) within a Flow must not run at the same time, otherwise the trading partner (Walmart, Macy's, Lowe's, etc.) receives overlapping/inconsistent inventory. Today this coordination is enforced using a single database table — `RouteExecutionLock` — that serves **two different purposes at once**:

1. **Self-lock** — "is *this* route already running?" (duplicate prevention)
2. **Flow coordination** — "is *another* inventory route in the same Flow running?" (via `Sp_IsOtherFlowInventoryRouteRunning`, which reads the same lock table)

Because both mechanisms depend on the same `IsActive = 1` rows, and because a lock is **acquired before the skip decision** and is **not always released reliably**, the design exhibits several defects ranging from *missed inventory feeds* to *broken coordination on long-running feeds*. This report documents the current behaviour, the concrete issues, the desired behaviour, and the recommended changes.

---

## 2. How It Works Today (Current State)

When an inventory route fires (via Hangfire → `RouteEngine.Execute` → `ExecuteExternal`):

```
[1] Load route. If IN-ACTIVE → remove job, return.

[2] AcquireLock(Customer, TypeId, RouteId, TimeoutMinutes)        ← acquires its OWN lock
       • If a lock already exists  → log "already running (DB lock held)" → RETURN
       • Else  → row inserted (IsActive = 1), continue

[3] If inventory route → GetFlowIdForRoute → IsOtherFlowInventoryRouteRunning?
       (SP counts OTHER inventory routes in the Flow with IsActive = 1 AND age <= 60 min)
       • Full Feed   → WAIT up to 30 min, then proceed or skip
       • Upload      → Full running → SKIP; Differential running → WAIT; else SKIP
       • Differential→ SKIP immediately
       • On skip/timeout → RETURN (no process launched)

[4] Spawn RouteWorker.exe (--routeId, --lockToken). processLaunched = true.

[5] Release lock:
       • Launched  → child RouteWorker finally + parent Process.Exited (backstop)
       • Skipped   → parent finally: if(!processLaunched) ReleaseRouteLock()
```

Key SQL (the coordination check):

```sql
SELECT COUNT(1) AS RunningCount
FROM RouteExecutionLock rel
  INNER JOIN FlowDetails fd ON fd.RouteId = rel.RouteId
  INNER JOIN Routes r ON r.Id = fd.RouteId
WHERE fd.FlowId = @FlowId
  AND fd.RouteId != @ExcludeRouteId
  AND r.TypeId IN (@InventoryTypeIds)
  AND rel.IsActive = 1
  AND DATEDIFF(MINUTE, rel.AcquiredAt, GETDATE()) <= 60;   -- hardcoded 60
```

---

## 3. Issues in the Current Design

> Legend: 🔴 Critical · 🟠 High · 🟡 Medium

### 🔴 A. Coordination breaks after 60 minutes on long feeds
- **Now:** The SP only counts locks whose age is `<= 60` minutes. Full Feeds run **3–6 hours**.
- **Failure:** At T+61 min, a sibling inventory route no longer "sees" the still-running Full Feed → it proceeds and runs **concurrently** with the Full Feed → partner receives overlapping inventory.
- **Impact:** The coordination fails for exactly the long feeds it is meant to protect.

### 🔴 B. Orphaned lock → whole-Flow skip cascade
- **Now:** On the skip/wait path the lock is **not released reliably**, leaving `IsActive = 1` stuck.
- **Failure:** A single stuck lock makes `Sp_IsOtherFlowInventoryRouteRunning` report "running" for **every** other inventory route in that Flow, for up to 60 minutes → the whole Flow's inventory routes skip.
- **Impact:** Inventory for the affected Flow stops syncing until the lock ages out (≤ 60 min) or is cleared manually. Observed 2026-06-30 (routes 44, 45, 76 in Flows 19 & 24 repeatedly skipping).

### 🟠 C. Mutual-skip race (two siblings fire together → both skip)
- **Now:** A route acquires its **own** lock *before* checking coordination.
- **Failure:** If two inventory routes in the same Flow fire at nearly the same time, each acquires its lock, then each sees the *other's* lock and **both skip** — no route runs.
- **Impact:** Missed inventory feeds even with no orphaned lock; chronic if the routes share a schedule.

### 🟠 D. Lock is acquired before the skip decision
- **Now:** Even a route that is about to skip first creates an `IsActive = 1` row.
- **Failure:** That premature row (a) pollutes sibling coordination (Issue C) and (b) becomes an orphan if its release fails (Issue B).
- **Impact:** Amplifies B and C; the lock is held when no work is being done.

### 🟡 E. Timeout inconsistency (SP hardcodes 60; AcquireLock uses a column)
- **Now:** `AcquireLock` and `CleanStaleLocks` use each lock's `TimeoutMinutes` column; the SP hardcodes `<= 60`.
- **Failure:** Setting `RouteLockTimeoutMinutes` to anything other than 60 makes duplicate-prevention and coordination disagree.
- **Impact:** Configuration changes silently break coordination.

### 🟡 F. The 30-minute wait holds both a worker and a lock
- **Now:** Full Feed / Upload "wait" loops `Thread.Sleep(30s)` for up to 30 minutes while holding a Hangfire worker **and** the lock.
- **Failure:** Workers are consumed by waiting (not working) routes; a waiting route also appears "running" to siblings even though it has done no work.
- **Impact:** Worker pressure + siblings blocked by a route that is only waiting.

---

## 4. What It Should Be (Target Behaviour)

| # | Today | Should Be |
|---|---|---|
| A | Coordination valid only for first 60 min of a run | Coordination valid for the **entire** real run, regardless of duration |
| B | Stuck lock blocks the whole Flow for ≤ 60 min | A finished/dead route **never** blocks others; release is reliable + self-healing in minutes |
| C | Two simultaneous siblings both skip | Exactly **one** wins and runs; the other skips deterministically |
| D | Lock held even when skipping | Lock/"running" state exists **only while actually executing** |
| E | Two different timeout values | **One** source of truth for the timeout |
| F | Waiting consumes a worker + lock | Waiting is worker-free (re-schedule) and does not mark the route as "running work" |

**Underlying principle:** separate "**is this route's dispatch in flight**" (a short-lived dispatch guard) from "**is this route actually executing work right now**" (the signal coordination should use). Coordination should be driven by *real execution state with liveness*, not by a dispatch lock that is acquired early and released unreliably.

---

## 5. Recommended Changes

Ordered by priority. Changes 1–3 stop the active production pain; 4–6 remove the structural defects.

| # | Change | Fixes | Effort |
|---|---|---|---|
| 1 | **Reliable release on skip/wait paths** — release the lock explicitly at each skip/timeout return **with a log line** (success/failure visible), not only via `finally`. | B | 0.5 day |
| 2 | **Self-healing janitor + startup cleanup** — store the child `ProcessId` on launch; a job every ~3 min (and on startup) releases locks on this machine whose process is dead, or whose `ProcessId` is NULL and age exceeds the max wait window. Keep `CleanStaleLocks` as a final backstop. | B | 1 day |
| 3 | **SP uses per-row timeout** — replace `DATEDIFF(...) <= 60` with `<= rel.TimeoutMinutes`; set `TimeoutMinutes` per route type so it exceeds the longest run (e.g. full feed = 480). | A, E | 0.5 day |
| 4 | **Acquire the lock only when proceeding to launch** — move `AcquireLock` to *after* the coordination decision, so skip/wait routes never hold a lock. | C, D | 0.5 day |
| 5 | **Atomic single-winner claim** — make "check others + claim" a single atomic DB operation (or a flow-level claim) so two simultaneous siblings cannot both skip; exactly one wins. | C | 1 day |
| 6 | **Worker-free wait** — replace the 30-min `Thread.Sleep` wait loops with re-scheduling (e.g. requeue the job after a delay) so no worker/lock is held while waiting. | F | 1 day |

**Stretch / proper redesign (recommended for a later sprint):** introduce an explicit `RouteRunState` (Started/Finished + heartbeat) written by the RouteWorker at actual start and finish, and base flow-coordination on *that* (with liveness) instead of the dispatch lock. This cleanly separates the two responsibilities that `RouteExecutionLock` currently overloads and removes Issues A–F at the root.

---

## 6. Suggested Sequencing

1. **Hotfix (today/this week):** Changes 1 + 2 + 3 — stop the orphan cascade, make coordination valid for long feeds, self-heal in minutes. Low risk, no behavioural change to actual route work.
2. **Hardening (next):** Changes 4 + 5 — remove the mutual-skip race and early-acquire coupling.
3. **Cleanup (later sprint):** Change 6 + the `RouteRunState` redesign.

---

## 7. Risks & Notes

- All lock changes are **reversible** and involve **no change to the actual route execution logic** (only when/where the lock is acquired/released and how coordination reads state).
- The janitor and per-row timeout require a small schema touch (`ProcessId` column) and an SP edit — both additive and backward-compatible.
- Setting `TimeoutMinutes` too low risks clearing a live long lock (duplicate run); too high delays orphan cleanup. The janitor (liveness-based) removes this trade-off because it does not wait for the timeout.
- Single-server today (`EMS`); the janitor's process-liveness check must be scoped to its own `MachineName` if the deployment ever becomes multi-node.

---

## 8. Recommendation

Approve Changes **1–3 as a hotfix** to immediately stop missed/halted inventory feeds, followed by **4–6** to remove the structural race and coupling. Plan the `RouteRunState` redesign for a later sprint as the permanent separation of dispatch-locking from execution-state coordination.

---

### Appendix — Affected Files
- `eSyncMate.Processor/Managers/RouteEngine.cs` — `ExecuteExternal` (acquire/skip/wait/launch/release), `Execute` (in-process path)
- `eSyncMate.DB/Entities/RouteExecutionLock.cs` — `AcquireLock`, `ReleaseLock`, `CleanStaleLocks`, table schema
- `eSyncMate.DB/Entities/RouteAbortFlag.cs` — `IsOtherFlowInventoryRouteRunning`
- `Sp_IsOtherFlowInventoryRouteRunning` (SQL stored procedure) — the `<= 60` hardcode
- `eSyncMate.RouteWorker/Program.cs` — child lock release (`--lockToken`)
