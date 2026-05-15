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
- PostgreSQL 16+ installed locally (download from postgresql.org/download/windows)
- Visual Studio 2022 17.12+ or JetBrains Rider 2025.1+

### First-time setup

1. Clone the repo
2. `cd roivo`
3. Ensure PostgreSQL is running locally (it runs as a Windows service after install)
4. Open `Roivo.sln` in Visual Studio
5. Set `Roivo.Web` as startup project
6. In Package Manager Console: `Update-Database -Project Roivo.Infrastructure -StartupProject Roivo.Web`
7. Run (F5). Navigate to `https://localhost:5001`.

### Useful URLs (local)

- App: https://localhost:5001
- pgAdmin: installed alongside PostgreSQL, accessible via the Start menu

## Project structure

- `src/Roivo.Web/` — Blazor Server app
- `src/Roivo.Core/` — Domain entities, interfaces (no dependencies)
- `src/Roivo.Infrastructure/` — EF Core, persistence, multi-tenancy
- `src/Roivo.Aade/` — myDATA integration
- `src/Roivo.Banking/` — PSD2 / open banking integration
- `src/Roivo.Forecasting/` — Cashflow projection logic
- `tests/` — corresponding test projects
- `marketing/` — positioning and ad content
- `later.md` — future ideas and post-MVP scope

## License

Proprietary. © 2026 Roivo. All rights reserved.
