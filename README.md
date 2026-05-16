# Roivo

Cashflow intelligence for Greek SMBs and accountants. Combines PSD2 open banking with myDATA tax data to automate reconciliation, forecast cashflow 90 days ahead, and warn about upcoming tax obligations.

## Status

🚧 Pre-launch development. MVP target: August 2026.

## Stack

- .NET 10 (LTS)
- ASP.NET Core 10 + Blazor Server
- PostgreSQL 16
- EF Core 10 with Npgsql
- ASP.NET Core Identity + OpenIddict for auth
- Hangfire for background jobs
- MudBlazor for UI
- Serilog for logging

## Local development

### Prerequisites
- .NET 10 SDK
- Visual Studio 2022 17.12+ or JetBrains Rider 2025.1+
- PostgreSQL — see "Database setup" below

### Database setup — choose one

#### Option A: Native PostgreSQL on Windows (current)
1. Download PostgreSQL 16 from postgresql.org/download/windows
2. Install with defaults; set a memorable password for the `postgres` superuser
3. Open pgAdmin (installed alongside Postgres):
   - Create login role: `roivo`, password `roivo_local_dev_password`, with "Can login?" and "Create databases?" enabled
   - Create database: `roivo`, owner `roivo`

#### Option B: Docker (recommended for teams)
1. Install Docker Desktop with WSL2 backend
2. `docker compose up -d` from the repo root
3. pgAdmin available at http://localhost:5050 (admin@roivo.local / admin)

Both options expose Postgres on `localhost:5432` — the connection string is identical.

### First-time setup
1. Clone the repo
2. Set up Postgres (Option A or B above)
3. Open `Roivo.sln` in Visual Studio
4. Set `Roivo.Web` as startup project
5. Apply migrations: in Package Manager Console (with `Roivo.Infrastructure` as Default project): `Update-Database -StartupProject Roivo.Web`
6. Press F5. App runs at https://localhost:7027 (or the port shown in console).

## Project structure

- `src/Roivo.Web/` — Blazor Server app
  - `Areas/Account/Pages/` — Razor Pages for auth flows (register, login, confirm email, reset password)
  - `Components/` — Blazor components and pages (Home, Dashboard, layout)
  - `Configuration/` — composition-root extension methods (`AddRoivoPersistence`, `AddRoivoIdentity`, etc.) and strongly-typed `Settings/` classes
- `src/Roivo.Core/` — Domain layer (no dependencies)
  - `Domain/Entities/` — EF Core entities
  - `Domain/Enums/` — domain enums
  - `Domain/Interfaces/` — domain abstractions (`ITenantScoped`)
- `src/Roivo.Infrastructure/`
  - `Persistence/` — `ApplicationDbContext` + `Configurations/` (one `IEntityTypeConfiguration<T>` per entity)
  - `MultiTenancy/` — `ITenantContext` + HTTP-claim-backed implementation
  - `Identity/`, `Email/`, `Auditing/` — Identity factory, MailKit sender, audit log writer
  - `Migrations/` — EF Core migrations
- `src/Roivo.Aade/` — myDATA integration (M4)
- `src/Roivo.Banking/` — PSD2 / open banking integration (M5)
- `src/Roivo.Forecasting/` — Cashflow projection logic (M7)
- `tests/` — corresponding test projects
- `docs/CONFIGURATION.md` — full configuration schema reference
- `marketing/` — positioning and ad content
- `later.md` — future ideas and post-MVP scope

## License

Proprietary. © 2026 Roivo. All rights reserved.
