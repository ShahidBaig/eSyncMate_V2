# Work Session: Task 00008 - 2FA session on refresh, forced password change, Change Password for regular users
**Date**: 2026-11-26
**Status**: In Progress (report delivered, waiting for review before implementation)

## Objective
Client e-mail ("eSyncMage Bug"): (1) user Jason Chen with 2FA is logged out when the browser is refreshed;
(2) checkbox on Add User to force a password change at first login; (3) regular users have no Change Password option.
The user asked for an HTML report with images first; implementation only after review.

## Tasks
- [x] Investigate the three points in code
- [x] Screenshots from the local site (http://localhost:5015, signed in as Majid): user menu, Add User form
- [x] Report (HTML, private artifact + local copy)
- [ ] Review by the user / lead, client answers
- [ ] Implementation, local test against the dev DB, Release10

## Progress Log

### Step 1: Investigation and report
- **Point 1 cause**: `UI/src/app/services/api.service.ts:249-250` `verifyMFADirect` posts `VerifyMFA` without `withCredentials`; `AuthInterceptor.ts:93-96` skips public URLs (Login, VerifyMFA, RefreshToken) so it does not add them. UI and API are different origins (ems.safavieh.com vs :8085), so the browser drops the `jwt` refresh cookie set by `VerifyMFA` (`AuthenticationController.cs:572`). After a refresh `auth.initializer.ts` calls `RefreshToken` with no cookie -> login. Non-2FA login sets `withCredentials: true` itself (`api.service.ts:25`). Fix: one line.
- **Point 2**: not built. Proposal: `Users.MustChangePassword BIT NOT NULL DEFAULT 0` (+ VW_Users), checkbox on Add / Edit User (default checked on Add; auto-checked when an admin sets a password in Edit), flag returned by Login / VerifyMFA and as a token claim, a guard that sends the user to Change Password, UpdatePassword clears it. Open questions for the client: default checked? apply to existing users?
- **Point 3 cause**: `page-header.component.html:98` `*ngIf="isAdminUser"`; `page-header.component.ts:64-78` true only for SuperAdmin or roles with the Users / Roles screen. Also `/change-password` route guard commented out (`app-routing.module.ts:66`). Fix: show for every signed-in user, restore the guard.
- **Security findings (same controller, not asked by the client)**: A) `Login` returns the user's MFA secret on every login (lines 157-167) and `VerifyMFA` (527-574) verifies the code against the secret sent by the browser + a User ID, without a password-step check or a comparison with the stored secret -> 2FA / password bypass with only a User ID (critical). B) `AuthenticateUser` (98) and `UpdatePassword` (413) concatenate user input into SQL (critical). C) `AuthenticationController` has no `[Authorize]` and Program.cs no fallback policy -> `registerUser` and `UpdatePassword` callable anonymously (high).
- **Report**: https://claude.ai/artifact/UCpWAZg2DsQSRoP54E4xqf (private); local copy `D:\Deployment\BIZMATE\Task00008\Task00008_Report.html`; source in the session scratchpad.
- **Notes**: the user pasted the dev DB connection string with password in chat; not used, not written anywhere. No code changed for this task yet.

### Step 2: Implemented all points + security fixes; DB script run on dev; tested in Chrome
- **Decisions (user: "go with your recommendation")**: checkbox checked by default for new users; existing users not forced; an admin who sets a new password in Edit User ticks it automatically.
- **DB** (`eSyncMate.Data/Tables/Task00008-User-Access/`): `T8_01_Add_MustChangePassword.sql` (Users.MustChangePassword BIT NOT NULL DEFAULT 0 + column in the LIVE VW_Users after U.[MFASecret]), `T8_02_Verify.sql`, `T8_99_Revert.sql`. Run on the DEV DB (dry run, apply, verify OK). Repo definitions `Tables/Users.sql`, `Views/VW_Users.sql` updated.
- **API**: `AuthenticationController.cs` - `[Authorize]` on the controller, `[AllowAnonymous]` on Login / VerifyMFA / RefreshToken / Logout; all user lookups and updates parameterised (`QueryUsers`, `ExecuteUsers`); Login for a 2FA user returns a 5-minute `mfaToken` ticket (signed with a key derived from the JWT key, audience `esyncmate-mfa-pending`, so it is never a valid access token) instead of the secret; VerifyMFA takes `{MfaToken, Code}` and checks the code against the STORED secret; `CompleteLogin` issues token + refresh cookie + `mustChangePassword`; token claim `mustChangePassword`; RefreshToken re-reads the user from the DB (blocked / deleted users get no new session); UpdatePassword uses the signed-in user from the token, checks 8-15 chars and new != old, clears the flag, returns a fresh session; registerUser only for SuperAdmin or a role with Add on `edi/users`, saves MustChangePassword, CreatedBy from the token. `PermissionMiddleware.cs`: a token with mustChangePassword=true may only call UpdatePassword (403 otherwise). Models: `LoginModel`, `UserModel` (UserDataModel, UpdateUserDataModel), entity `Users.cs` (MustChangePassword after MFASecret - inside the insert/update column range, so the DB script must run first).
- **UI**: `api.service.ts` (VerifyMFA withCredentials, `mustChangePassword()`, `firstAllowedRoute()`, token field), `models.ts`, `login.component.ts` (ticket instead of secret, redirect to Change Password), `guards/authentication.guard.ts` and `authorization.guard.ts` (forced redirect; not signed in -> /login), `app-routing.module.ts` (guard on /change-password), `change-password` (forced mode: notice, no Close, saves the new session, goes to the first allowed page; userID from the token), `page-header.component.html` (Change Password for everyone), `register` (checkbox + hint, default checked), `edit-users-dialog` (checkbox, auto-tick on password change), translations en/es (3 keys).
- **Tests (local API restarted from bin\Release, UI on :5015, dev DB)**: curl - registerUser / UpdatePassword without sign-in 401; SQL text as User ID refused; VerifyMFA with own secret or forged ticket refused. Chrome - test users `t8.user` (Id 19, Operator, must change) and `t8.mfa` (Id 20, Operator, 2FA) created via Add User / Edit User; t8.user: forced page, direct navigation and reload redirected, other API 403, password changed -> dashboard, menu shows Change Password; t8.mfa: QR setup, the USER scanned the QR with an authenticator app and entered the code, then a full reload kept the session (point 1 fixed). Test passwords are in the session scratchpad file `task00008-test-users.txt` (dev only).
- **Report updated** (version 2 with results and 4 new screenshots): https://claude.ai/artifact/UCpWAZg2DsQSRoP54E4xqf ; local copy `D:\Deployment\BIZMATE\Task00008\Task00008_Report.html`.
- **Open**: not committed; Release10 (API + UI + T8_01 at the client BEFORE the API) waits for the user. Majid was signed out in the local browser by the tests.
