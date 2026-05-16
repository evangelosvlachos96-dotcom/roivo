# Roivo — Later

Ideas, features, refactorings, and concerns to address AFTER we have 10 paying customers AND the core 12-week MVP is live.

**Do not work on anything in this file until that threshold is met.** If a new idea appears, capture it here, then close the file.

## Active 12-week MVP scope (the only things being worked on now)

Week 1: Foundation — solution, schema, auth, multi-tenancy, Docker, first migration  
Week 2: Auth flows — registration, login, email confirmation, password reset, tenant signup wizard  
Week 3: Tenant + Business management — onboarding, adding businesses to a tenant, role management  
Week 4: AADE integration — read-only myDATA sync (RequestDocs, RequestMyIncome, RequestMyExpenses), encrypted credential storage  
Week 5: PSD2 integration — GoCardless Bank Account Data, bank connection flow, transaction sync, encrypted token storage  
Week 6: Reconciliation engine — auto-match transactions to invoices, manual match UI  
Week 7: Cashflow forecasting — 30/60/90 day projections, chart visualization  
Week 8: Tax obligation calendar — VAT, ΕΦΚΑ, προκαταβολή, ΕΝΦΙΑ rules engine + alerts  
Week 9: Accountant multi-client dashboard — health colors, prioritized needs-attention list  
Week 10: Notifications — email + in-app alerts, customer payment reminders  
Week 11: Polish — bilingual UI (Greek + English), error handling, audit log review, security pass  
Week 12: Launch — landing page, beta deploy, 3 design partners onboarded  

## M4 enhancements

- After first successful AADE sync, compare AADE-returned company info (name, AFM, KAD) with Roivo tenant info. If mismatch, offer to update tenant data. This is the real "verify business identity" check — happens organically when the user proves they have AADE credentials, rather than artificially at registration time.

## Captured ideas (not to be worked on yet)

### Polish / UX
- AFM enumeration: currently we tell the user when an AFM is already registered (soft warning). AFMs aren't private (printed on invoices), so this is acceptable. If abuse appears, consider rate-limiting registration attempts per IP and removing the AFM duplicate warning.

### Architecture / infrastructure
- Move to microservices when team grows beyond 1 person
- Consider Neo4j for fraud-ring / money-mule detection (only if Roivo expands into AML)
- Consider Kafka or RabbitMQ if message-based integrations with third parties are needed
- Read replicas / connection pooling once we exceed ~100 concurrent users
- Schema-per-tenant or DB-per-tenant for enterprise customers requiring physical isolation

### Features
- Mobile native app (iOS + Android) — for now, PWA + responsive web is enough
- AI-powered cashflow recommendations ("you should ask customer X to pay early")
- Integration with Greek payroll providers (Epsilon Pylon Payroll, etc.)
- Integration with Greek accounting software for full bookkeeping sync
- Marketplace for accountants to advertise services to potential clients
- Open API for third-party developers to build on top of Roivo
- White-label version for banks to offer to their SMB clients
- Multi-currency support beyond EUR
- Expansion to Cyprus (similar tax regime)
- Expansion to other EU countries (different tax APIs per country)

### Compliance / business
- AADE-certified e-invoicing provider status (πάροχος ηλεκτρονικής τιμολόγησης) — €15k-€50k investment, 9-18 months
- ISO 27001 certification (needed when selling to bigger customers)
- SOC 2 Type II (only if expanding to US/UK markets)
- PCI-DSS (only if processing card data directly, which we shouldn't)
- Become a TPP (Third Party Provider) under PSD2 ourselves instead of using GoCardless

### Tech debt / refactors
- Move from Blazor Server to Blazor WebAssembly or hybrid if we hit SignalR scaling issues
- Add Redis caching layer if database load becomes a bottleneck
- Migrate to managed Postgres (Hetzner Cloud / DigitalOcean) when self-hosted becomes a burden
- Add OpenTelemetry tracing for production debugging
- Add automated security scanning in CI (Snyk, GitHub Advanced Security)
- Consider migrating from Hangfire to a more modern background job library if Hangfire shows limitations
- Set up Docker-based dev environment for new contributors when team grows beyond 1 person — for now, native Postgres install is documented in README.md

### Marketing / sales
- Build an accountant referral program (commission per referred customer)
- Sponsor a Greek accountants' conference (ΟΕΕ events)
- Write a series of SEO articles about myDATA, PSD2, and Greek tax obligations
- Build a free public tool that estimates VAT obligations — lead magnet for the paid product
- Partner with one or two Greek banks for co-marketing

## Rules for using this file

1. When a new idea appears during build phase, capture it here with one sentence.
2. Do not act on anything in this file until the 12-week MVP is live AND we have 10 paying customers.
3. Review this file at the start of every quarter after launch.
4. Items that have been completed get moved to a separate "shipped.md" with the date.
