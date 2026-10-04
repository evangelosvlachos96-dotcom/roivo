# Roivo — Progress Journal

A running log of what's been built, when, and any notes worth keeping. Newest entries at the top.

---

## M9 + M10 — Accountant workspace and email notifications ✅ Code complete 2026-10-04

### M9 — Accountant dashboard

Three pages at `/accountant/dashboard`, `/accountant/alerts`, `/accountant/reports`,
behind `AccountantAccess.CanViewAccountantWorkspace` — a business-owner tenant
gets `Forbidden`, not a thinner version of the page.

- **Dashboard**: per client, AADE and banking status, reconciliation rate,
  cashflow health, last sync; summary bar with totals, average match rate,
  cashflow warnings and tax deadlines inside 30 days.
- **Alerts**: aggregated across clients, ordered Critical → Warning → Info.
  Only the *earliest* predicted shortfall per business is reported — later
  negative days are the same shortfall, not new ones — and a business with no
  invoices is not flagged for a 0% match rate, which is absence of work rather
  than a backlog.
- **Reports**: consolidated reconciliation, tax calendar and cashflow views,
  with a CSV export matching the corrected implementation (formula-injection
  prefix, UTF-8 BOM, full set, RFC 4180 quoting).

**Cashflow health** is a pure function over days of runway: Red under 30 days —
one ΦΠΑ/ΕΦΚΑ cycle, inside which the shortfall can no longer be fixed by
rescheduling — Yellow under 90, Green at the forecast horizon, and **Unknown**
when nothing is stored, which is deliberately not the same as healthy.

**Data isolation** is the central risk and is tested, not just intended: the
handlers use only the tenant-filtered `ListActiveAsync()`, never the
`...AcrossAllTenantsAsync` cron variants. A test wraps a repository that mirrors
production's filtered/unfiltered asymmetry, so swapping in a cron read fails the
test instead of leaking another tenant's clients.

**Query cost** is bounded by the bulk reads `ListStoredForecastsAsync` and
`ListTaxObligationsForBusinessesAsync` — two queries regardless of client count,
rather than one forecast run per business.

### M10 — Email notifications

Five kinds (daily digest, tax reminder, cashflow alert, sync failure, weekly
reconciliation) over the Resend HTTP transport that M-fix made work. Per-user,
per-business `NotificationSettings`, now mapped with a unique index on
`(BusinessId, UserId)`. Settings UI at
`/businesses/{id}/settings/notifications` with a test-send button.

Four Hangfire jobs, all Europe/Athens: **cashflow-alert 06:30** (deliberately
after the 06:00 forecast it reads — earlier would alert on yesterday's
projection), **daily-digest 07:00**, **tax-reminder 08:00** (ahead of the 09:00
failure notifications, so a deadline lands before the noise), and
**weekly-reconciliation Monday 07:00**. Ten recurring jobs now registered,
verified in Hangfire's own tables.

### Tests

**406 passed, 1 skipped** (322 → 406, +84: 47 accountant, 37 notification).

### Decisions worth knowing

- **`Roivo.Infrastructure` may reference `Roivo.Resources`** — the csproj says so
  explicitly and CODING_STANDARDS only bars Application/Core/Aade/Banking. So the
  email templates read Greek strings directly instead of having them threaded
  through from the caller.
- **Notification idempotency rides on AuditLogs.** The entity and its migration
  were frozen when the jobs were written, so `NotificationDispatchLog` records a
  ledger row per send rather than stamping a column. It works and is dedupe-keyed
  per job, but see the gap below.

### Post-build review — four defects found and fixed

Both features were reviewed adversarially after they were built. M10 came back
with **no blockers**: tenant scope is correct on all four jobs and all seven
repository calls, writes carry a real `TenantId` rather than `Guid.Empty`, and
every interpolated value in every template is HTML-escaped. M9 did not, and the
fixes are in:

- **Match rate could exceed 100%.** `GetCountsAsync` counted confirmed and
  pending matches over all time against a window-filtered invoice count. Latent
  since M6 — invisible on a 90-day dashboard with little history, glaring on the
  accountant report's one-month window, where a long-reconciled client would read
  several hundred percent. It also voided the "below 50%" alert. Both counts are
  now scoped to the window's invoices, the fake mirrors the real repository, and
  a regression test pins it.
- **Overdue tax obligations were invisible.** Both accountant queries floored
  their window at today, so an unpaid obligation vanished on the morning it
  became late — and `TaxObligation.IsOverdue` could never return true, which made
  the overdue UI unreachable dead code. Both now look back 90 days.
- **One undeliverable address aborted a whole job run.** Resend throws on any
  4xx and Polly deliberately does not retry those, so a single rejected recipient
  starved everyone ordered after it — on that run and identically on every run
  after. All four jobs now isolate each send and continue.
- **A doc comment promised tenant filtering that does not exist.** The bulk
  cashflow reads use `IgnoreQueryFilters()`; confinement comes entirely from the
  ids passed in. The comment now says so.

### Known gaps / next steps

- **The dispatch ledger belongs in its own table.** A `NotificationDispatches`
  table — `TenantId`, `DispatchKey`, `SentAt`, unique on `(TenantId, DispatchKey)`
  — would replace the AuditLogs ledger and turn the current check-then-send into
  one atomic insert, closing the race if two Hangfire workers run a job at once.
  A `LastSentAt` column on `NotificationSettings` would *not* do: the tax reminder
  dedupes per obligation per lead-time stage, which needs a row per key.
- **Reconciliation counts are still N+1, and it is worse than it looks.**
  `GetCountsAsync` is eight SQL round trips, not one, and
  `IReconciliationRepository` has no bulk variant — so a 50-client accountant
  page issues ~403 statements, doubled to ~806 because `App.razor` prerenders
  without `prerender: false`. The digest and weekly jobs pay the same cost. The
  fix is a `GetCountsForBusinessesAsync(IReadOnlyCollection<Guid>, from, to)`
  returning a dictionary with zeroed entries for businesses that have no rows,
  mirroring the two bulk cashflow reads.
- **Two notification jobs ignore bulk reads that already exist.**
  `CashflowAlertJob` passes a one-element array to a collection-taking API fifty
  times, and `TaxReminderJob` never uses `ListTaxObligationsForBusinessesAsync`.
- **No `[DisableConcurrentExecution]` on the notification jobs.** Two Hangfire
  servers would both pass the "already sent?" check before either stamps the
  ledger. The attribute needs a Hangfire reference that `Roivo.Infrastructure`
  deliberately does not have — its jobs are plain classes invoked reflectively —
  so the unique-index fix above is the right close, not a package dependency.
- Both migrations are applied to staging; existing businesses defaulted to
  `Quarterly` ΦΠΑ and a null property value, so no ΕΝΦΙΑ is projected for them
  until someone enters a value.

---

## M8 — Tax calendar completion, plus a shared clock ✅ Code complete 2026-10-03

### M8

- **ΕΝΦΙΑ (property tax)** in `GreekTaxCalendar`: five monthly instalments,
  September through January. Generated **only** when the owner has supplied
  `Business.EstimatedPropertyValue` — Roivo cannot see the property register, so
  with no value it projects nothing rather than inventing a figure.
- **Monthly vs quarterly ΦΠΑ** keyed off the new `Business.VatFrequency`.
  Quarterly stays the 20th after the quarter closes; monthly is the 20th of the
  following month. A business gets one or the other, never both.
- Both read through a new `ICashflowRepository.GetTaxProfileAsync` projection,
  so the calendar reads two fields instead of loading (and risking mutating) a
  whole aggregate.

### Shared clock

`IClock` / `SystemClock` in `Roivo.Application.Abstractions`, registered as a
singleton. The reconciliation dashboard was seeding its date range from
`DateTime.Today` (server local) while every handler worked in UTC — Greece is
UTC+2/+3, so for up to three hours a day the two disagreed about which day it
was, and the same obligation read "pending" on one screen and "overdue" on
another. Entity `CreatedAt = DateTime.UtcNow` defaults are deliberately left
alone: entities are not DI-resolved, and they were never the inconsistency.

### Tests

`Roivo.Application.Tests` 213 → **229**. Suite total **322 passed, 1 skipped**.
Tax-calendar tests now run against a `FakeClock` rather than absolute dates, so
they cannot rot the way two earlier ones did.

### Verified against staging, not just compiled

Seeded 60 days of bank transactions for a test business and loaded the cashflow
page: the forecast engine produced a real 90-day projection (balance €8,500 →
€12,560), and **the MudChart rendered with data for the first time** — three
series, scaled axes, a populated confidence band. Until then every chart in M6
and M7 had only ever been seen behind an empty-data guard, so the MudBlazor 9.4
`ChartSeries<double>` / `ChartData<double>` wiring was unproven.

### Known gaps / next steps

- **M9 (accountant dashboard) and M10 (email notifications) are NOT built.**
  Only scaffolding exists: `Features/Accountant/AccountantAccess.cs` and
  `Services/CashflowHealth.cs`, plus `Roivo.Resources/Notifications.cs`. No
  handlers, pages, templates or jobs. The `NotificationSettings` entity exists
  but is deliberately **not** mapped — no `DbSet`, no EF configuration, no
  migration — so it is inert groundwork rather than half-applied schema.
- The `AddBusinessTaxProfile` migration covers only the two `Business` columns.
  `VatFrequency`'s default is set to `Quarterly` by hand; EF generated `""`,
  which every pre-existing row would have failed to parse.
- ΕΝΦΙΑ uses a single flat rate as a coarse projection. The real figure is
  per-property and location-based; this is a cashflow estimate, not a
  calculation, and the rate belongs in configuration before anyone relies on it.

---

## M6 + M7 — Reconciliation engine and cashflow forecasting ✅ Code complete 2026-10-01

Built together as one epic because M7 cannot forecast without M6's matched data,
and both land on the same two new UI surfaces.

### M6 — Reconciliation engine

**Domain:** `ReconciliationMatch` (automatic / manual / suggested, pending /
confirmed / rejected) and `ReconciliationRule` (per-business amount and date
tolerance, optional counterparty pattern). `IsReconciled` + `ReconciledAt` added
to `Invoice` (via `MarkReconciled` / `ClearReconciliation`) and
`BankTransaction`.

**Matching:** `ReconciliationEngine` scores every eligible pair — exact amount
0.4, within tolerance 0.2; date within 1 day 0.3, 3 days 0.2, 7 days 0.1;
counterparty 0.3. At or above 0.8 the match is applied automatically; 0.5 and up
is suggested for review. Assignment is greedy over a globally sorted candidate
list, so the strongest pair claims a transaction before a weaker one competing
for it.

**Handlers:** `RunReconciliation`, `ConfirmSuggestedMatch`,
`RejectSuggestedMatch`, `ManualMatch`, `GetReconciliationDashboard`,
`GetUnreconciledItems`.

**Job:** `NightlyReconciliationJob` at 05:00 Europe/Athens, an hour after the
banking sync, over the trailing 90 days. Only businesses with *both* AADE and
banking connected are eligible — with one side missing every invoice would
report as unreconciled.

### M7 — Cashflow forecasting

**Domain:** `CashflowForecast` (prediction plus the actuals backfilled later),
`CashflowCategory` (recurring rent / payroll / subscriptions), `TaxObligation`.

**Forecasting:** `CashflowForecastEngine` blends a day-of-week and a
day-of-month profile from the last 365 days of bank activity, layers on
recurring categories and unpaid tax obligations, and runs a 90-day balance
forward. The confidence band widens with the square root of the horizon.
Alerts fire for negative balance, low balance, a tax date, and an outlier
outflow.

**Tax calendar:** `GreekTaxCalendar` generates ΦΠΑ (quarterly, 20th after
quarter end), Παρακρατούμενος Φόρος (monthly, 20th), ΕΦΚΑ (monthly, month end),
and the three income-tax instalments with their prepayment and Τέλος
Επιτηδεύματος. VAT is estimated from actual invoice VAT (output less input,
floored at zero); income tax from projected revenue.

**Handlers:** `GetCashflowForecast`, `GetCashflowDashboard`, `GetTaxCalendar`,
`MarkTaxPaid`, `AddRecurringItem`.

**Job:** `NightlyCashflowForecastJob` at 06:00 Europe/Athens, after
reconciliation. Skips any business with under 30 days of history.

### Tests

`Roivo.Application.Tests` 136 → **208** (+72). Engine scoring 13, tax calendar
14, forecast engine 10, reconciliation handlers 10, cashflow handlers 25.
A seeded 100-invoice synthetic dataset holds the engine to the milestone's
70% auto-match bar and asserts that every automatic match is correct — a wrong
auto-match is worse than none, because nobody reviews it.

### Decisions worth knowing

- **Both engines live in Application over repository abstractions**, not in
  `Roivo.Forecasting` (still empty) — they are pure computation over rows we
  already hold, with no third-party integration to isolate.
- **A rejected match is kept, never deleted.** It is the only record that the
  engine proposed a pair and a human said no, which is what stops the next
  nightly run proposing it again. The unique index on
  `(InvoiceId, BankTransactionId)` is filtered to exclude rejected rows, so a
  pair can still be matched by hand afterwards.
- **Direction has to agree**: an invoice we issued is settled by a credit, one
  we received by a debit. Without that check a refund of the same size scores
  identically to the payment.
- **Cancelled invoices are excluded** from matching — AADE voided them, so
  there is no money to find, and leaving them in would permanently depress the
  match rate.
- **Withholding tax and ΕΦΚΑ estimate zero.** Both depend on payroll, which
  Roivo never sees. A user-entered recurring category is the honest way to get
  them into the forecast; a guess would read as authoritative.
- **No history produces no projection**, not a flat line at the current
  balance. A flat line reads as a prediction.
- **Tax obligations are identified by `(business, type, period)`**, so
  regenerating the calendar is idempotent and never clobbers a recorded payment.

### Known gaps / next steps

- **M8 is now mostly built.** The spec folded the Greek tax calendar into M7, so
  ROADMAP's M8 is reduced to ΕΝΦΙΑ, the monthly-vs-quarterly ΦΠΑ distinction by
  books type, and surfacing obligations as markers on the cashflow chart.
- **Invoice status is not set to Paid on match.** M6's roadmap line asks for it;
  the build sets `IsReconciled` / `ReconciledAt` only, leaving
  `Invoice.Status` as AADE reported it. Deliberate — the two facts are
  different, and conflating them would lose the distinction — but it is a gap
  against the roadmap wording.
- **Critical cashflow alerts are logged, not emailed.** Wiring them into
  `EmailJob` needs a per-business dedupe stamp, or a persistent shortfall mails
  the owner every night.
- **Tax rates are hardcoded** (ΦΠΑ 24%, income tax 22%, prepayment 80%, Τέλος
  Επιτηδεύματος €650). They belong in configuration before anyone relies on the
  figures, and AADE moves statutory deadlines by decision most years.
- **Forecast accuracy is unmeasured against reality.** `CashflowForecast` stores
  `Actual*` columns and a `BalanceError` for exactly this, but nothing backfills
  them yet.
- The engine's 70% bar is met against *generated* data shaped like Greek SMB
  activity. It is a regression guard on the thresholds, not evidence about real
  bank feeds.

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
- ~~Pre-existing, outside M5: `NightlyAadeSyncJob`'s per-business job logs
  "Business … not found; skipping" for every business~~ — **fixed 2026-10-02.**
  `SyncBusinessInvoicesCommand` gained `BypassTenantScope`, and the tenant id
  now comes from the loaded aggregate rather than ambient context. The identity
  probes in `InvoiceRepository.UpsertAsync` and both
  `IncomeBookEntryRepository` reads also needed `IgnoreQueryFilters()`: without
  them the tenant-less cron missed every existing row and either inserted
  orphaned `TenantId = Guid.Empty` duplicates or collided with the unique
  indexes. Three regression tests cover it.
