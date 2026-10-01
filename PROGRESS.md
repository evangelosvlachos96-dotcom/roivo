# Roivo — Progress Journal

A running log of what's been built, when, and any notes worth keeping. Newest entries at the top.

---

## Cashflow UI v1 — synced AADE data viewer ✅ Completed 2026-05-21

First UI surfacing the data M4 syncs from AADE (incoming `Invoices` + outgoing aggregated `IncomeBookEntries`). Per-business page at `/businesses/{id}/invoices` with two tabs.

**What was built:**
- **Summary tab:** incoming/outgoing gross totals, net flow (outgoing − incoming), last-sync timestamp, and the 10 most recent rows across both tables. Totals are computed server-side in SQL (`GroupBy` aggregation, no client-side summing). A standing "non-reconciled" disclaimer makes clear this is raw AADE data, not bank-matched.
- **Detailed tab:** server-paginated `MudTable` (`ServerData`) over a UNION of both tables, with filters (direction, date range, counterparty-AFM search, cancelled status) and sortable columns (issue date, net, gross). Scales to thousands of rows — paging/filtering/sorting all run in the database.
- **Navigation:** a Receipt icon on the businesses list and a "Τιμολόγια" button on the business-edit AADE section, both shown only when the business has AADE credentials connected.

**Layering:** queries + `IInvoiceQueryRepository` abstraction in Application; `InvoiceQueryRepository` (factory-per-call) in Infrastructure; page in Web. All Greek strings in new `Roivo.Resources/Invoices.cs`. Both handlers follow the canonical order and return `Success | BusinessNotFound`. The UI gets a small `BusinessHeaderInfo` record, not the `Business` entity, across the boundary.

**Test counts:** Roivo.Application.Tests 55 → **65** (+10: 2 summary, 8 paged). `FakeInvoiceQueryRepository` mirrors the real filter/sort/paginate logic. Core/Infrastructure/Aade unchanged. Build clean.

**No migration, no new packages.**

**Architectural note worth flagging:** the detailed-tab UNION combines `Invoices.IssueDate` (`date`) and `IncomeBookEntries.IssueDate` (`timestamptz`). Both are projected to a `date`-derived `timestamp` in the LINQ projection so the EF `Concat` produces matching column types. This is the one path that needs live-DB verification — the handler tests use an in-memory fake and don't exercise the Npgsql translation.

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

## Architecture Refactor ✅ Completed 2026-05-16

Introduced Roivo.Application layer organized by feature. Extracted all business logic from Razor components into command/query handlers. Created repository abstractions, implemented in Infrastructure. Added unit tests for all Business handlers. Net: same behavior, dramatically more testable and maintainable codebase.

**Files moved / restructured:**
- Roivo.Infrastructure/MultiTenancy/ITenantContext.cs → Roivo.Application/Abstractions/ITenantContext.cs
- Roivo.Infrastructure/Auditing/AuditService.cs → Roivo.Infrastructure/Auditing/AuditWriter.cs (renamed; new contract takes anonymous-object details)
- Razor components no longer reference EF Core, DbContext, or JsonSerializer

**New projects:**
- Roivo.Application
- Roivo.Application.Tests

**Test coverage:**
- 24 unit tests for Business + query handlers
- All passing

**Dependency rules now enforced:**
- Roivo.Core has zero EF Core references
- Roivo.Application has zero EF Core references
- Razor components have zero EF Core or DbContext references
- Razor components have zero System.Text.Json references

## Milestone 4: AADE myDATA integration ✅ Completed 2026-05-17

Full read-only sync against the AADE myDATA dev API. Per-business credentials stored encrypted (ASP.NET Data Protection). Nightly Hangfire cron at 03:00 Europe/Athens fans out per-business sync jobs. UI for connect/disconnect/sync-now with AFM-mismatch hard-block.

**Test counts:** Roivo.Application.Tests 49 (was 29, +20), Roivo.Core.Tests 28 (unchanged). All passing.

**Files created (~30):**
- Application abstractions: `IAadeClient`, `IAadeCredentialStore`, `IAadeRateLimiter`, `IInvoiceRepository`, `AadeValidationResult`, `AadeFetchResult` + `AadeInvoiceDto`
- 5 Aade features: ConnectAade, DisconnectAade, SyncBusinessInvoices (commands + results + handlers), GetAadeConnectionStatus, ListConnectedBusinesses (queries + handlers)
- Roivo.Aade project filled out: `AadeHttpClient`, `EncryptedCredentialStore`, `InMemoryAadeRateLimiter`, `NightlyAadeSyncJob`, `SyncSingleBusinessJob`, `AadeSettings`, `AadeServiceCollectionExtensions`
- Infrastructure: `InvoiceRepository`, `ApplicationDbContextDesignTimeFactory`
- UI: `BusinessAade.razor` (3-state connect page)
- Tests: 4 fakes (`FakeAadeClient`, `FakeAadeCredentialStore`, `FakeAadeRateLimiter`, `FakeInvoiceRepository`) + 4 test classes

**Migration applied:** `20260518013159_AddAadeFieldsToBusiness` — `RenameColumn AadeUserId → AadeUserIdEncrypted`, `AddColumn LastAadeSyncAt`.

**Architectural notes / known gaps for smoke testing:**
- `InMemoryAadeRateLimiter` is per-process; multi-instance deploys will lose the global ceiling (acceptable for MVP, documented in later.md).
- Hangfire dashboard at `/hangfire` is dev-only and unauthenticated; prod gating noted in later.md.
- `Newtonsoft.Json` 13.0.3 overrides the vulnerable 11.0.1 transitive from `Hangfire.Core`.
- `ListConnectedBusinessesHandler` returns IDs across all tenants (cron is system-wide); the new `IBusinessRepository.ListIdsWithAadeCredentialsAcrossAllTenantsAsync` uses `IgnoreQueryFilters`.
- `AadeHttpClient`'s `ValidateCredentialsAsync` extracts the caller's AFM from the first invoice's `issuer/vatNumber`. If the user has no invoices in the probe window, the AFM-mismatch check can't fire — needs a real-world test with a clean AADE account to confirm.
- Smoke test needed: log in → connect a business → click "Συγχρονισμός τώρα" against the AADE dev endpoint; verify invoices appear and `LastAadeSyncAt` updates.

## Milestone 5: Enable Banking PSD2 integration — code complete, blocked on credentials

Read-only bank-transaction sync behind an aggregator-agnostic `IBankingClient`,
implemented against Enable Banking. Per-business session stored encrypted
(ASP.NET Data Protection, same pattern as AADE). Nightly Hangfire cron at 04:00
Europe/Athens fans out per-business sync jobs; 09:00 cron emails owners whose
connection has been broken for over 24 hours. UI for connect (bank picker →
bank's consent page → callback) / sync-now / disconnect.

**Not yet verified end-to-end:** Enable Banking answers every call with
`403 "Application does not exist"` for the application id in `.env`
(`26a1b402-…`). The JWT itself is accepted — a malformed token returns a
different 401 — so this is an account-side issue (application not activated, or
the wrong id), not a code one. Everything downstream of a live connection is
covered by unit tests but has never touched a real bank.

**Test counts:** Roivo.Application.Tests 136 (was 49, +87), Roivo.Banking.Tests
28 (new; 1 opt-in live smoke test skipped), Roivo.Infrastructure.Tests 11
(was 5, +6), Roivo.Core.Tests 32 (unchanged). All passing.

**Files created (~30):**
- Application abstractions: `IBankingClient`, `IBankingCredentialStore`,
  `IBankingConnectionOptions`, `IBankAccountRepository`,
  `IBankTransactionRepository`, `BankingResults.cs` (5 result unions + 4 DTOs)
- 6 Banking features: ConnectBanking, CompleteBankingConnection,
  DisconnectBanking, SyncBusinessBankTransactions (commands + results +
  handlers), GetBankingConnectionStatus, ListBankingProviders,
  ListBankingConnectedBusinesses (queries + handlers)
- Roivo.Banking project filled out: `EnableBankingClient`,
  `EnableBankingJwtFactory`, `EncryptedBankingCredentialStore`,
  `NightlyBankingSyncJob`, `SyncSingleBusinessBankingJob`,
  `EnableBankingSettings`, `EnableBankingConnectionOptions`, `DotEnvFile`,
  `EnableBankingServiceCollectionExtensions`, `Models/EnableBankingModels.cs`
- Infrastructure: `BankAccountRepository`, `BankTransactionRepository`,
  `BankingFailureNotificationJob`
- UI: `BusinessBanking.razor` (3-state page), `BankingCallback.razor`,
  banking section on `BusinessEdit.razor`, `Roivo.Resources/Banking.cs`
- Tests: 4 new fakes + 5 handler test classes + 3 Banking test classes

**Migration applied:** `20260930181101_AddBankingFieldsToBusiness` — banking
columns on `Businesses`, `ExternalAccountUid` on `BankAccounts`, unique indexes
on `(BusinessId, ExternalAccountUid)` and `(BankAccountId, ExternalId)`, plus
filtered index on `BankingAccessTokenEncrypted` for the cron.

**Decisions worth knowing:**
- `IBankingClient` models the generic PSD2 flow (list banks → start
  authorization → exchange code for session → read transactions per account),
  not Enable Banking's vocabulary. Swapping aggregators replaces
  `Roivo.Banking`, nothing above it.
- The stored credential is the aggregator **session id**, not an OAuth access
  token. It lives in `Business.BankingAccessTokenEncrypted` (name kept for
  continuity) under Data Protection purpose `Roivo.Banking.Credentials` —
  deliberately distinct from the AADE purpose.
- Transactions reuse the M1-era `BankAccount` / `BankTransaction` entities
  rather than introducing a parallel table. Idempotency is enforced by a unique
  index on `(BankAccountId, ExternalId)`, not just by application logic.
- `Amount` is normalised to a signed decimal in the client (Enable Banking sends
  an unsigned magnitude plus `credit_debit_indicator`), so nothing above the
  aggregator layer has to know that convention.
- A sync fails whole rather than partially: one account erroring aborts the run
  without stamping `LastBankingSyncAt`, so a gap can't hide behind a green tick.
- `.env` is read by `DotEnvFile` into configuration keys at startup; real
  environment variables are layered after it and win, so deployments need no
  `.env` at all.

**Known gaps / next steps:**
- Resolve the Enable Banking application id before anything can be smoke-tested
  against a real bank. Run `ROIVO_BANKING_SMOKE=1 dotnet test
  tests/Roivo.Banking.Tests` to re-check once fixed.
- `EnableBanking:RedirectUrl` is `https://localhost:7027/banking/callback` and
  must be registered with the Enable Banking application; production needs the
  real host.
- `state` on the consent redirect carries only the business id. The completion
  handler re-checks tenant ownership and permissions, so this is not an
  authorisation hole, but a per-attempt nonce would also close replay of a
  stale redirect.
- Consent expiry (90 days) is stored and displayed but nothing proactively warns
  the user before it lapses — today they find out when a sync fails.
- Pre-existing, outside M5: `NightlyAadeSyncJob`'s per-business job logs
  "Business … not found; skipping" for every business, because
  `SyncBusinessInvoicesHandler` loads through the tenant-filtered repository
  while the cron runs with no tenant scope. The banking sync hit the same trap
  and works around it with an explicit `BypassTenantScope` flag; AADE needs the
  same fix or its nightly sync is a no-op.
