# Roivo — Progress Journal

A running log of what's been built, when, and any notes worth keeping. Newest entries at the top.

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
