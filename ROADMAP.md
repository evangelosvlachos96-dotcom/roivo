# Roivo — Roadmap to MVP

12 milestones from empty repo to launched product. Estimated original target: August 2026. Now ahead of schedule after Milestone 1 completing in ~36 hours.

This roadmap is intentional, not aspirational. Each milestone has a clear "done" definition. If a milestone slips, we adjust the date but not the scope. If a new feature idea appears that's not in this roadmap, it goes in `later.md` and is not worked on until MVP is live with paying customers.

---

## Milestone 1 — Foundation ✅
Multi-tenant .NET solution, PostgreSQL, Identity + OpenIddict, first migration, home page connects to DB.
**Status:** Complete (May 15, 2026)

## Milestone 2 — Authentication flows 🔜
Registration, login, email confirmation, password reset, logout. Tenant signup wizard (accountant vs business owner path). First protected page. Tenant context middleware reading JWT claims.
**Done when:** A new user can register, confirm email, log in, see a "Welcome, $name (Tenant: $tenantName)" page, log out, log back in. Failed login attempts get locked out after 5 attempts.

## Milestone 3 — Tenant + Business management
Tenant settings page. Add/edit/list businesses under a tenant. Roles: TenantOwner, TenantMember. Accountant tenants can manage many businesses; Business tenants manage exactly one.
**Done when:** An accountant can sign up, add 5 client businesses, and the data is properly tenant-scoped (verified by integration test).

## Milestone 4 — AADE / myDATA integration (read-only)
Register on AADE dev portal. Implement RequestDocs, RequestMyIncome, RequestMyExpenses, RequestTransmittedDocs. Encrypted credential storage. Background sync job (Hangfire, hourly). UI for tenant to enter AADE credentials per-business. Display synced invoices.
**Done when:** A business adds AADE credentials, Roivo fetches their last 30 days of invoices and displays them in a list with proper VAT amounts, dates, counterparties.

## Milestone 5 — PSD2 / Open Banking integration
Sign up with GoCardless Bank Account Data (free tier). Implement bank institution selection, consent flow, transaction fetching. Encrypted access token storage. Background sync job. UI for connecting bank accounts.
**Done when:** A business connects their Piraeus / Eurobank / Alpha / Ethniki account, Roivo fetches the last 90 days of transactions and displays them.

## Milestone 6 — Reconciliation engine
Auto-match algorithm: bank transactions ↔ invoices based on amount, date proximity, counterparty similarity, reference matching. Confidence scoring. Manual matching UI for unmatched transactions. Update invoice status to Paid when matched.
**Done when:** 70%+ of bank transactions are auto-matched correctly on a test dataset of 100 real-world cases. UI lets user resolve the remaining 30%.

## Milestone 7 — Cashflow forecasting
Forecasting engine: 30/60/90-day projections using current balance, open invoices, detected recurring expenses, scheduled tax obligations. Chart visualization with confidence bands.
**Done when:** A business owner sees a chart of projected cash over the next 90 days, with markers for known events.

## Milestone 8 — Tax obligation calendar
Rules engine for Greek tax obligations: ΦΠΑ (monthly or quarterly based on books type), ΕΦΚΑ, προκαταβολή φόρου, ΕΝΦΙΑ. Auto-populate upcoming obligations onto the cashflow chart. Alerts when projected cash won't cover an obligation.
**Done when:** A business with low cash sees a red marker on the cashflow chart for an upcoming VAT date they can't afford.

## Milestone 9 — Accountant multi-client dashboard
Accountant tenant sees all their client businesses in one view. Health colors (red/yellow/green based on cashflow status). Prioritized "needs attention" list. Click-through to per-client detail view.
**Done when:** An accountant with 10 test client businesses sees a dashboard matching the design mockup, with at least 2 in red/yellow status correctly flagged.

## Milestone 10 — Notifications
Email + in-app notifications. Customer payment reminders (send email to overdue customers from within Roivo). Daily digest for accountants. Configurable notification preferences.
**Done when:** A test trigger (overdue invoice, upcoming tax obligation, sync failure) results in a correctly-formatted email to the right recipient.

## Milestone 11 — Polish + security pass
Bilingual UI (Greek + English) — all UI strings in resource files. Comprehensive error handling. Audit log review tool. Security headers verified (CSP, HSTS, etc.). OWASP ZAP automated scan. Encryption-at-rest audit. Backup procedure documented and tested.
**Done when:** Automated security scan passes. Manual UX walkthrough completes without crashes or untranslated strings.

## Milestone 12 — Launch
Landing page (roivo.gr — separate marketing site or section). Beta deploy to production VPS. SSL via Let's Encrypt. Domain configured. Email sending via Postmark or Resend. Onboard 3 design partner accountants for free 6-month access.
**Done when:** A real accountant in Thessaloniki is using Roivo on their own data and has given feedback.

---

## Rules

1. Milestones are completed in order. No skipping ahead.
2. New ideas during a milestone go to `later.md`, not into the current milestone's scope.
3. When a milestone completes, append an entry to `PROGRESS.md`.
4. Estimated dates adjust based on actual velocity. The order doesn't change.
