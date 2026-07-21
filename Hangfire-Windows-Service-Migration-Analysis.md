# Analysis Report: Migrating Hangfire from IIS In-Process to a Dedicated Windows Service

**Project:** eSyncMate_V2 (EDI Processing Platform)
**Component:** eSyncMate.Processor — Background Job Scheduler (Hangfire)
**Environment:** Production — `ems.safavieh.com:8085`, single server (`EMS`)
**Author:** Development Team
**Date:** 2026-06-30
**Status:** For Senior Review

---

## 1. Executive Summary

The eSyncMate background-job engine (Hangfire) currently runs **inside the IIS worker process** (`w3wp.exe`) because `eSyncMate.Processor` is hosted with `hostingModel="inprocess"`. This couples **job execution** to the **IIS request/recycle lifecycle**. As a result, whenever IIS recycles, the application pool restarts, the server reboots, or a startup dependency hiccups, **all scheduled jobs silently stop running** until a user request "wakes up" the application.

This has caused **repeated production incidents** (most recently on 2026-06-30, a ~2.5-hour outage from 01:00 to 03:29 AM during which no EDI jobs ran). It has also introduced **orphaned execution locks** that block routes from re-running.

This report recommends a **low-risk, industry-standard remedy**: move the **Hangfire job-processing server** into a **dedicated always-on Windows Service**, while **keeping the existing Hangfire Dashboard exactly as-is in IIS**. Both halves continue to share the same SQL storage, so there is **no loss of monitoring or UI** and **no change to the dashboard URL**.

---

## 2. Current Architecture

```
                 ┌─────────────────────────────────────────────┐
                 │            IIS  (w3wp.exe)                    │
   HTTP/JWT ───► │   eSyncMate.Processor  (hostingModel=         │
                 │                          inprocess)           │
                 │   ┌─────────────────────────────────────┐    │
                 │   │ Hangfire SERVER  (processes jobs)     │    │
                 │   │ Hangfire DASHBOARD (/dashboard UI)    │    │
                 │   └─────────────────────────────────────┘    │
                 └───────────────────┬─────────────────────────┘
                                     │
                                     ▼
                       SQL Server  ESYNCMATESCHEDULER
                       (192.168.0.44,7100)  — Hangfire storage
                                     ▲
                                     │  spawns child process per route
                                     ▼
                       eSyncMate.RouteWorker.exe  (route execution)
```

**Key facts**
- Hangfire version: 1.8.5
- The Hangfire **BackgroundJobServer lives inside `w3wp.exe`**. When `w3wp` is not running the application, Hangfire is **not processing jobs** — even if the IIS Application Pool status shows "Started" (green).
- Job execution is **fire-and-forget**: the Hangfire job spawns `eSyncMate.RouteWorker.exe` as a child process (long inventory feeds run 3–6 hours).
- A `RouteExecutionLock` table provides cross-process duplicate-prevention and full-vs-differential feed coordination.

---

## 3. Problem Statement

### 3.1 Job execution is coupled to the IIS lifecycle
Because Hangfire runs in-process, **any event that restarts or fails to warm `w3wp` stops all scheduled jobs**:
- Application Pool recycle (scheduled or manual, e.g. during deployment)
- Server reboot (e.g. Windows Update)
- A transient startup-dependency failure (e.g. SQL maintenance window)

When this happens, jobs only resume after the **next HTTP request** loads the application — the textbook IIS in-process signature.

### 3.2 Documented production incident (2026-06-30)
| Observation | Value |
|---|---|
| Hangfire history graph | Steady ~400 jobs/period, then **dropped to 0 at ~01:00 AM** |
| Hangfire "Server Started" | **~03:29 AM** (i.e. the app only re-initialised on a user request) |
| Outage window | **01:00 → 03:29 AM (~2.5 hours, no jobs ran)** |
| Recurring jobs missed | Hangfire fires an overdue recurring job only **once** on recovery — **no back-fill**, so partner feeds (inventory, orders, ASN) in that window were skipped |

The IIS warm-up configuration (App Pool `AlwaysRunning`, site `Preload`, Application Initialization module, `web.config` init block) was verified to be **correctly in place** — yet the outage still occurred, demonstrating that **warm-up configuration alone cannot guarantee availability**. The leading trigger is an unrecoverable 1:00 AM event (suspected SQL maintenance/restart during which the warm-up preload request failed; Application Initialization does not retry).

### 3.3 Orphaned execution locks
Route execution locks are released by an **in-process `Process.Exited` event handler** inside `w3wp`. If the application dies before the child process exits (recycle, crash, reboot, or the 1 AM event), **the handler never runs and the lock remains `IsActive = 1`**, blocking that route from re-running until an hourly cleanup job reclaims it (up to ~60 minutes later). Seven such orphaned locks were observed during the 2026-06-30 incident.

---

## 4. Root Cause

> **The single root cause is architectural: a long-running background job scheduler is hosted inside a request-driven web server (IIS) whose lifecycle is optimised for HTTP requests, not for 24/7 background processing.**

Every symptom above (silent job stoppage, dependency on a "wake-up" request, orphaned locks on recycle) is a direct consequence of this coupling. Tactical mitigations (warm-up settings, lock cleanup jobs) reduce the frequency but **cannot eliminate** the failure mode while Hangfire remains in-process.

---

## 5. Proposed Solution

Split Hangfire into its two independent responsibilities:

| Responsibility | API | New Home |
|---|---|---|
| **Job Server** (processes jobs) | `AddHangfireServer()` | **New dedicated Windows Service** (always-on) |
| **Dashboard** (monitoring UI) | `UseHangfireDashboard()` | **Stays in the IIS app, unchanged** |

Both continue to point at the **same SQL storage (`ESYNCMATESCHEDULER`)**, so the dashboard keeps showing all jobs, recurring jobs, retries and servers — including the new Windows Service, which registers itself in the **Servers** tab.

### 5.1 Target Architecture

```
   ┌────────────────────────────────┐
   │        IIS  (w3wp.exe)          │       ┌──────────────────────────────┐
   │  eSyncMate.Processor            │       │   Windows Service (always-on) │
   │  • REST APIs                    │       │   eSyncMate.Scheduler         │
   │  • Hangfire DASHBOARD (/dashboard)│     │   • Hangfire SERVER           │
   │  • NO AddHangfireServer()       │       │   • spawns RouteWorker.exe    │
   └───────────────┬────────────────┘       └───────────────┬──────────────┘
                   │                                         │
                   └──────────────┬──────────────────────────┘
                                  ▼
                  SQL Server  ESYNCMATESCHEDULER  (shared Hangfire storage)
```

### 5.2 What changes vs. what stays the same

| Aspect | Before | After |
|---|---|---|
| Dashboard URL | `ems.safavieh.com:8085/dashboard` | **Unchanged** |
| Dashboard content | Jobs / Recurring / Servers / graphs | **Unchanged** (reads same DB) |
| Job processing | Inside `w3wp` (dies on recycle) | **In Windows Service (recycle-proof)** |
| Recurring job definitions | DB-driven (Flow system) | **Unchanged** (service picks them up) |
| Route execution | Spawns `RouteWorker.exe` | **Unchanged** |
| Deployment of jobs | Stop App Pool → copy → start | Stop Service → copy → start |

---

## 6. Benefits

1. **Eliminates the primary outage class** — IIS recycles, deployments, reboots and warm-up failures no longer stop job processing.
2. **Eliminates recycle-induced orphaned locks** — the job host is no longer torn down by App-Pool-OFF during deployments.
3. **Cleaner, safer deployments** — restarting the API no longer disrupts running jobs; the scheduler is restarted independently and intentionally.
4. **No loss of visibility** — the Hangfire Dashboard remains fully functional at the same URL.
5. **Consistent with the existing design** — `RouteWorker.exe`, `AlertWorker.exe`, and `eSyncmateApiMonitor` already run as independent processes/services; this aligns the scheduler with that proven pattern.
6. **Better diagnostics** — the scheduler gets its own service logs and its own Windows Service auto-restart-on-failure policy.

---

## 7. Risks and Mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Both IIS and the Service accidentally run a Hangfire **Server** → jobs run twice | Medium | Remove `AddHangfireServer()` from the IIS app; verify only the Service appears in the dashboard **Servers** tab. Hangfire's distributed locks also prevent most double-execution. |
| Service cannot resolve job types (e.g. `RouteEngine`) | Low | Service references the same `eSyncMate.Processor` assemblies that define the job methods (same as RouteWorker today). |
| Configuration / connection-string drift between IIS app and Service | Low | Both load configuration from the shared `ApplicationSettings` table (already the project standard); only the connection string is in `appsettings.json`. |
| Service account lacks permissions (DB, file paths, spawning child processes) | Medium | Run the Service under the same identity currently used by the App Pool; validate DB access and `RouteWorker.exe` path on first start. |
| Operational unfamiliarity (start/stop/monitor a service) | Low | Use NSSM (already used in HA tooling) for install/auto-start; document start/stop and add the Service to `eSyncmateApiMonitor` health checks. |

**Rollback:** trivial — re-enable `AddHangfireServer()` in the IIS app and stop the Windows Service. The system returns to the current behaviour with no schema changes.

---

## 8. Implementation Plan (high level)

| Phase | Task | Est. Effort |
|---|---|---|
| 1 | Create `eSyncMate.Scheduler` Windows Service project (.NET 8 Worker), referencing `eSyncMate.Processor`; configure Hangfire **Server only** against `ESYNCMATESCHEDULER` | 0.5 day |
| 2 | Move recurring-job registration/bootstrap (if any) so the Service owns it; confirm Flow-driven jobs are picked up | 0.5 day |
| 3 | Remove `AddHangfireServer()` from `eSyncMate.Processor`; keep `UseHangfireDashboard()` | 0.25 day |
| 4 | Package + install via NSSM (auto-start, restart-on-failure); run under the App-Pool identity | 0.5 day |
| 5 | Add the Service to `eSyncmateApiMonitor` health checks; logging/alerting | 0.5 day |
| 6 | Staging validation: recycle IIS and confirm jobs keep running; deploy test; failover/restart test | 1 day |
| 7 | Production cutover + monitoring | 0.5 day |
| | **Total** | **~3.5–4 days** |

---

## 9. Interim Mitigations Already Applied (Tactical)

These reduce impact **today**, but do **not** replace the strategic migration:

1. **IIS warm-up settings verified** — App Pool `Start Mode = AlwaysRunning`, `Idle Time-out = 0`, site `Preload = True`, Application Initialization module enabled, `web.config` init block present (preloads `/api/health`).
2. **Deterministic lock release (code fix, 2026-06-30)** — `eSyncMate.RouteWorker` now releases its **own** execution lock in a `finally` block (via a new `--lockToken` argument) instead of relying solely on the parent process's `Process.Exited` handler. The parent handler and the hourly cleanup remain as idempotent backstops.
3. **Lock timeout alignment (config)** — `ApplicationSettings.RouteLockTimeoutMinutes` to be set greater than the longest route run (e.g. 480 min) so a still-running multi-hour feed's lock is not prematurely reclaimed (which could cause a duplicate run).
4. **Deploy practice** — verify no `RouteWorker.exe` is running before stopping the App Pool, to avoid orphaning in-flight runs.

---

## 10. Recommendation

Approve the migration of the **Hangfire job server to a dedicated Windows Service**, retaining the dashboard in IIS. It is a **low-risk, reversible, ~4-day** change that **permanently eliminates** the recurring class of background-job outages and orphaned-lock issues, with **no loss of monitoring capability** and **no database schema changes**. The tactical fixes in Section 9 should remain in place as defence-in-depth.

---

## Appendix A — Glossary
- **In-process hosting** — the .NET app runs inside the IIS worker process (`w3wp.exe`) rather than a separate `dotnet.exe`.
- **Application Pool recycle** — IIS periodically (or on demand) restarts the worker process; in-process apps lose all in-memory state, including a running Hangfire server.
- **Application Initialization / Preload** — IIS feature that sends a warm-up request to the app after a restart so it initialises without waiting for a real user request.
- **Hangfire Server vs Dashboard** — the Server processes jobs; the Dashboard is a read-only web UI over the same storage. They are independent and need not share a process.
