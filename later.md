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

## M3 follow-ups (revisit during UI polish pass)

- Business list display: currently shows Name, AFM, KAD, status. Decide whether to show Address inline in the list, or only in a future Business Profile detail page (e.g., /businesses/{id}). Detail page route doesn't exist yet — only Edit. Adding a profile page is M4-M5 work alongside AADE/banking data display per business.

## M4 follow-ups (revisit after first AADE sync runs in dev)

- **AFM-update flow on mismatch**: today, if AADE returns a different AFM than the Business holds, the connect page links the user back to the business edit form. There's no direct "update the business AFM and retry" inline path. Wire one once we see real users hit this.
- **AADE production environment switch**: M4 hits the dev host only (`mydata-dev.azure-api.net`). Production switch is config-driven — add a second `AadeSettings` profile + a guard against accidentally pointing dev at prod.
- **Distributed rate limiter**: `InMemoryAadeRateLimiter` is per-process. When we scale beyond one instance, swap to Redis-backed counters (linked from the "adaptive sync scheduling" entry below).
- **Smart adaptive sync scheduling**: see existing "Cost optimization (post-scale)" entry — relevant once AADE rate-of-change data starts informing per-business cron timing.
- **Hangfire dashboard authorization**: mounted at `/hangfire` in dev only. For prod, gate behind an admin-only authorization filter rather than removing it.

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

### Pricing tiers and premium features

These are product-level decisions about what's free/basic vs premium. Revisit when pricing tiers are designed (post-launch, after first 10 paying customers reveal what they value).

#### Forecast horizon

- Free / basic tier: 90 days (covers core VAT + ΕΦΚΑ + προκαταβολή window)
- Premium tier: 30 / 60 / 90 / 180 / 365 day toggle
- Premium tier: accuracy indicator showing confidence dropping past 90 days
- Rationale: longer-horizon planning is genuinely more valuable for tax planning, especially for accountants planning annual obligations. Real differentiation, not artificial gating.

#### Sync frequency and on-demand refresh

- Free / basic tier: daily sync at 3am (covers the "tell me what happened yesterday" use case)
- Premium tier: on-demand refresh button (2-3 manual refreshes per day, rate-limited)
- Premium tier: smart adaptive sync — auto-refresh when user logs in, immediate sync after detected high-value transactions
- Rationale: bank API calls (PSD2 via GoCardless/Tink) cost roughly €0.10–0.30 per account per refresh. Premium tier covers this cost. Daily-only at scale (1000 customers) ≈ €100-300/month total. Multiple-times-daily would be €2k-6k/month — needs to be monetized.

#### Other potential premium features (capture as we think of them)

- White-label option for accounting firms (their logo, custom domain)
- API access for accountants to integrate Roivo data into their own tools
- Multi-currency support (for businesses with EU clients beyond Greece)
- SMS notifications (vs free email only)
- Priority support / faster response SLA
- Advanced reports / custom report builder
- Data export to common accounting software (Pylon, Softone import format)

#### Tier design principles to remember

- Every premium feature should have real cost behind it (API spend, infrastructure, support load) — not just artificial limits to force upgrades
- Free tier must be genuinely useful on its own — most users stay free, premium covers the cost of the few who upgrade
- Avoid quota-based pricing (X invoices/month) for accountants — they manage many businesses and quotas create friction at exactly the wrong moment

## Cost optimization (post-scale)

Ideas to reduce per-customer infrastructure cost once volume justifies the engineering effort. Do not build any of these until the underlying metric (API spend, hosting bill, etc.) crosses the threshold noted in each entry.

### Adaptive sync scheduling (M11+, scale-dependent)

Idea: instead of syncing every customer at a fixed daily time, learn each customer's behavior pattern and schedule syncs to be fresh when they're active. Reduces API cost by avoiding wasted refreshes for inactive accounts.

#### Why this matters

- PSD2 bank API calls cost real money (roughly €0.10-0.30 per refresh at GoCardless/Tink scale).
- At 50 customers: maybe €50-100/month potential savings — not worth the engineering investment.
- At 1,000 customers: ~€1,000-2,000/month savings — worth building.
- At 10,000+ customers: critical — €10k+/month at stake.

Decision: do not build until we cross ~500 paying customers. Track the API spend metric to know when.

#### Two signals worth capturing

1. **Login times** — tells us when the user wants fresh data. UX signal.
2. **Bank transaction timestamps** — tells us when the bank's data actually changes. Freshness signal.

These often don't align (owner checks in the morning, transactions happen throughout the day). The "best sync time" depends on which signal we optimize for. Probably need both — sync slightly before typical login if the user has a pattern, otherwise sync after the bank's likely activity window.

#### Two possible implementations

**Option A: Per-customer learned schedule (complex)**
- Collect telemetry: login timestamps, transaction timestamps
- Cluster per customer to find their "active window"
- Schedule sync to land 30-60 minutes before their active window
- Re-cluster monthly for new customers, every 6 months once stable
- User can manually reset the recommendation (e.g., when their business policy changes)
- Estimated effort: 4-8 weeks
- Failure modes: new customers (no history), erratic patterns, vacations/holidays, accountant vs client hour misalignment

**Option B: Bucket-based schedule (pragmatic)**
- 3-4 fixed buckets:
  - "Morning checker" → sync at 5am
  - "Evening checker" → sync at 5pm
  - "Always-on" → sync 2x/day
  - "Sporadic" → sync every 2 days
- Each customer assigned to a bucket based on first 30 days of behavior
- Re-assigned quarterly
- Estimated effort: 1-2 weeks
- Captures ~80% of personalization value at much lower complexity

Probably start with Option B when we cross the threshold, evolve to Option A only if data shows meaningful additional savings.

#### Subscription tier interaction

- Free tier: daily sync at fixed off-peak time (e.g., 3am Greece). No personalization.
- Premium tier: adaptive scheduling enabled. Sync runs at the learned-optimal time. Plus 2-3 on-demand refreshes per day.
- Enterprise/accountant tier (future): sync runs multiple times per day, can be configured by user, plus on-demand.

#### Telemetry and privacy notes

- Collecting login times and transaction patterns counts as behavioral profiling under GDPR.
- Privacy policy must disclose: "We analyze your usage patterns to optimize when we refresh your data, minimizing costs and ensuring freshness when you're active."
- Users must be able to opt out (defaults to fixed schedule if they do).
- Data retention: behavioral data older than 6 months can be deleted (no value beyond pattern learning).

#### User controls

- "Reset sync recommendation" button — when a business changes its operating pattern, user can wipe the learned schedule and restart learning.
- "Show me when Roivo last synced" — transparency, builds trust.
- "Force refresh now" — manual override, always available (counts against premium quota if applicable).

#### Open questions to resolve when we build this

- Does the accountant's pattern matter, or the client business's pattern? They could be different.
- How to handle customers who use Roivo across multiple time zones (rare in Greece, but for future EU expansion)?
- Cron precision — do we need minute-level scheduling, or is 15-minute buckets fine?

## Rules for using this file

1. When a new idea appears during build phase, capture it here with one sentence.
2. Do not act on anything in this file until the 12-week MVP is live AND we have 10 paying customers.
3. Review this file at the start of every quarter after launch.
4. Items that have been completed get moved to a separate "shipped.md" with the date.
