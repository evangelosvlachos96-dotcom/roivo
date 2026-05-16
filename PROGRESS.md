# Roivo — Progress Journal

A running log of what's been built, when, and any notes worth keeping. Newest entries at the top.

---

## Milestone 2 — Authentication flows ✅ Completed 2026-05-15

**Started:** 2026-05-15
**Completed:** 2026-05-15
**Originally estimated:** 1 week

### What was built

- MailKit-backed `SmtpEmailSender` + `SmtpSettings` (`required init`, validated at startup) wired through Mailtrap in dev
- `IAuditService` / `AuditService` writing structured `AuditLog` entries with IP + tenant scope; failures are logged to Serilog and swallowed so audit never crashes the request
- `TenantClaimsPrincipalFactory` adding the `tenant_id` claim that `ITenantContext` / global query filters depend on (display fields like `FullName` deliberately stay in the DB to avoid stale-claim bugs)
- Razor Pages under `Areas/Account/Pages/`: `Register`, `RegisterConfirmation`, `ConfirmEmail`, `Login`, `Logout`, `ForgotPassword`, `ResetPassword`, `AccessDenied` — all in Greek, with anti-enumeration on password reset
- Registration runs inside a DB transaction: creates `Tenant`, then `ApplicationUser` via `UserManager`, then (for Business tenants) a single `Business` row
- Email confirmation token generated via `UserManager.GenerateEmailConfirmationTokenAsync`, link rendered into an HTML email
- Login handles all five `SignInResult` branches with distinct audit actions (`LoginSucceeded`, `LoginFailed`, `LoginBlockedNotAllowed`, `AccountLockedOut`) and Greek user-facing messages
- Logout is POST-only with an antiforgery token; GET redirects to `/`
- Password reset calls `UpdateSecurityStampAsync` on success to invalidate other sessions
- Cookie hardening: `HttpOnly`, `Secure=Always`, `SameSite=Strict`, sliding 30-day expiration, custom name `roivo.auth`
- Authorization wired through `AuthorizeRouteView` + `AddCascadingAuthenticationState()` so `[Authorize]` enforces redirects from Blazor pages
- `/dashboard` Blazor page (protected) showing the current user's name + email (fetched via `UserManager.GetUserAsync`), tenant info, and a tenant-scoped Businesses count to prove the global query filter is active
- AFM checksum validator (`AfmValidator` + `[ValidAfm]` data annotation) in `Roivo.Core/Domain/Validation`, applied to the registration form; 14 unit tests in `Roivo.Core.Tests` covering the algorithm

### Blockers encountered
- None. M2 went smoothly on top of the M1 foundation + the structural refactor.

### Notes / lessons
- Razor Pages were the right call for auth flows — full page reloads, native anti-forgery, redirect-after-post all "just work", which is much fussier in interactive Blazor
- Moving `full_name` out of claims and into a `UserManager.GetUserAsync` lookup avoids a whole class of "user updated their profile, but the cookie still says the old name" bugs. Claims are for authorization; display data lives in the DB
- The AFM checksum is a free first line of defense against typos and obviously-bogus registrations. Real business-identity verification (name/AFM/KAD cross-check) is deferred to M4 once AADE credentials are connected — captured in `later.md`

---

## Refactor pass — Code organization ✅ Completed 2026-05-15

**Type:** Structural refactor, no new functionality.

### What changed
- Reorganized `Roivo.Core/Domain/` into `Entities/`, `Enums/`, `Interfaces/` subfolders
- Reorganized `Roivo.Infrastructure/` with `Persistence/Configurations/`, `MultiTenancy/`, etc.
- Extracted entity configurations into `IEntityTypeConfiguration<T>` classes — `OnModelCreating` is now a 4-line composition
- Split `Program.cs` into focused extension methods (`AddRoivoPersistence`, `AddRoivoIdentity`, etc.) — Program.cs is now 34 lines
- Moved all hardcoded config values (password policy, lockout settings, token lifetimes, cookie config, app name, log file path) to `appsettings.json` with strongly-typed settings classes using `required init` properties
- Added `ValidateOnStart()` + `ValidateDataAnnotations()` to all settings — app refuses to start if config is invalid
- Refactored `SmtpSettings` to also use `required init` with `[Required]` / `[Range]` / `[EmailAddress]` annotations (no longer carries hardcoded `noreply@roivo.gr` / `Roivo` defaults)
- Documented full config schema in `docs/CONFIGURATION.md`

### Why
Codebase organization before continuing on M2/M3. Catching the "hardcoded values are anti-patterns" instinct early so the auth surface and tenant management code grow on top of a clean composition root rather than a 150-line `Program.cs`.

### Result
- `dotnet build`: clean (0 warnings, 0 errors)
- App behavior: identical to pre-refactor for all M1 + M2 flows, with two intentional changes flagged in code review:
  - `Identity:Password:RequireLowercase` default flipped from `false` to `true` (matches the prescribed `appsettings.json` template).
  - `AuditLog` gained two indexes (`TenantId, Timestamp`) and (`UserId, Timestamp`) and a `Timestamp` default via SQL — required regenerating the migration.

---

## Milestone 1 — Foundation ✅ Completed May 15, 2026

**Started:** May 14, 2026 (Thessaloniki context, Hawaii build location)
**Completed:** May 15, 2026, ~11:57 PM Hawaii time
**Duration:** ~36 hours elapsed, including blockers
**Originally estimated:** 1 week

### What was built

- Multi-project .NET 10 solution: Roivo.Web (Blazor Server), Roivo.Core, Roivo.Infrastructure, Roivo.Aade, Roivo.Banking, Roivo.Forecasting + corresponding test projects
- PostgreSQL 16 database with EF Core 10 + Npgsql
- ASP.NET Core Identity + OpenIddict 7 for OAuth2/OIDC authentication
- Multi-tenancy: `ITenantScoped` interface + automatic global query filters via reflection
- Auto-set `TenantId` on entity creation through `SaveChangesAsync` override
- Domain entities: Tenant, ApplicationUser, Business, BankAccount, BankTransaction, Invoice, AuditLog
- Enum-to-string database conversions (Currency, TenantType, InvoiceDirection, InvoiceStatus)
- Initial migration applied — approximately 30 tables in the database
- Blazor Server home page with live DB connectivity check showing "✓ Connected"
- MudBlazor configured for UI components
- Serilog configured for structured logging
- `marketing/positioning.md` with full Greek + English product positioning
- `later.md` for capturing post-MVP ideas without acting on them
- README with dual setup paths (native Postgres / Docker)

### Blockers encountered

- WSL2 installation kept timing out on Windows (network or feature dependency issue)
- Switched from Docker-based Postgres to native PostgreSQL install — total recovery time ~30 minutes
### Notes / lessons

- Native Postgres on Windows is a completely valid dev setup; Docker is a "later" upgrade
- The `ITenantScoped` interface + reflection-based filter loop in `ApplicationDbContext.OnModelCreating` removes a whole class of multi-tenant bugs forever
- The auto-set TenantId in `SaveChangesAsync` means future entities can be added without remembering tenant logic

---

## Template for future milestones

```
## Milestone N — Name ✅ Completed YYYY-MM-DD

**Started:** YYYY-MM-DD
**Completed:** YYYY-MM-DD
**Duration:** X days elapsed
**Originally estimated:** N weeks

### What was built
- ...

### Blockers encountered
- ...

### Notes / lessons
- ...
```
