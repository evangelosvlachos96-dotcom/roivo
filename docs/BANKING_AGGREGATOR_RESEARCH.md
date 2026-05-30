# Banking Aggregator Research — M5

**Status:** Decision pending. Outreach sent to Noda and Salt Edge for 
sandbox access. Awaiting responses.

**Last updated:** 2026-05-26

## Why we need a banking aggregator

Roivo is a cashflow intelligence product for Greek SMBs and accountants. 
M4 delivered AADE myDATA integration (invoice data). M5 must integrate 
PSD2 banking data (transaction data) to complete the cashflow picture.

We don't connect to Greek banks directly because:
- PSD2 AISP licensing is regulatory burden (months, tens of thousands 
  of euros, ongoing compliance)
- Multi-bank integration means N separate codebases to maintain
- Aggregators absorb regulatory + operational complexity behind one API
- Operating under an aggregator's license is the standard path for 
  pre-launch fintech

We need an aggregator that:
- Covers the four major Greek banks: NBG, Piraeus, Alpha, Eurobank
  (Attica is nice-to-have)
- Offers a developer sandbox accessible without enterprise sales process
- Has REST/JSON API (we're on .NET 10)
- Has reasonable pricing for ~50 connected accounts year one

## Aggregators evaluated

### GoCardless Bank Account Data — ELIMINATED

**Why initially considered:** Free tier, 2,400+ EU banks, well-documented.

**Why eliminated:** GoCardless stopped accepting new Bank Account Data 
signups as of July 2025. Only existing customers retain access. 
Verified via Actual Budget's docs and direct attempts to register at 
bankaccountdata.gocardless.com.

**Lesson:** Easy availability can change. Document aggregator 
availability at time of decision.

### Tink (Visa-owned) — ELIMINATED

**Why initially considered:** Largest player, 3,400+ banks, Visa backing, 
free sandbox, strong data enrichment.

**Why eliminated:** Tink's official coverage is 18 markets. Greece is 
NOT in those 18. Their providers list confirmed no Greek banks 
available. Sandbox registration succeeded but no Greek connections 
exist. Tink primarily serves Nordic + UK + Western European markets.

**Lesson:** "Pan-European" claims by aggregators don't always include 
Greece. Verify country coverage explicitly, not just "EU" as a category.

### Yapily — ELIMINATED

**Why initially considered:** Developer-friendly platform, free Console 
account, mock bank for testing, 2,000+ banks.

**Why eliminated:** Yapily lists 19 supported countries. Greece is NOT 
among them. Their coverage is UK + Germany, France, Netherlands, Italy, 
Spain, Nordics, Ireland, Austria, Belgium, Portugal — but not Greece.

### Plaid — NOT EVALUATED

US-focused. Limited EU coverage, no specific Greek presence claimed. 
Skipped without detailed evaluation.

### TrueLayer — NOT EVALUATED (NOT PURSUED)

UK-focused. Some EU coverage but Greek presence unclear. Skipped in 
favor of providers with explicit Greek coverage.

### Salt Edge — IN OUTREACH

**Why considered:** Has a dedicated Greek coverage page 
(saltedge.com/products/account_information/coverage/gr). 10+ years in 
market, ISO 27001 certified, 5,000+ banks across 70+ countries. 
Established platform.

**Concerns:**
- No self-serve sandbox visible — requires sales contact
- Pricing not public
- Greece is one of 70 countries — focus may be diluted

**Status:** Contact form submitted. Awaiting response on sandbox 
access, Greek bank coverage details, and pricing.

### Noda — IN OUTREACH

**Why considered:** Greek-focused platform. Explicitly markets coverage 
of "all major Greek banks" plus 2,000+ European banks. Modern API 
design with webhooks and SCA-ready integration.

**Concerns:**
- Smaller and newer than Salt Edge
- Less battle-tested
- Long-term company viability is a real risk for a critical dependency

**Status:** Contact form submitted. Awaiting response on sandbox 
access, Greek bank coverage details, pricing, and onboarding 
requirements.

## Decision framework

When responses come in, evaluate against these criteria in order:

1. **Greek bank coverage (must-have)** — Must support NBG, Piraeus, 
   Alpha, Eurobank at minimum. Attica is bonus.

2. **Sandbox accessibility** — Must provide developer sandbox without 
   committing to enterprise contract. Self-serve is best; sales-gated 
   sandbox with quick turnaround is acceptable.

3. **Pricing fit** — At our scale (~50 accounts year 1, growing to 
   maybe 500-1000 over 2-3 years), pricing must be predictable and 
   not require enterprise minimums.

4. **API quality** — REST/JSON, good docs, .NET-friendly (no exotic 
   protocols). PSD2-standard endpoints expected.

5. **Onboarding speed** — Can we have a working sandbox in days, not 
   weeks?

6. **Long-term viability** — Will they exist in 3 years? Backed by 
   serious investors or revenue? GoCardless's July 2025 shutdown of 
   Bank Account Data is the cautionary tale.

7. **Data enrichment quality** — Counterparty parsing, categorization, 
   merchant detection. Affects M6 reconciliation algorithm difficulty. 
   Nice-to-have, not deal-breaker.

## Architectural decision: aggregator-agnostic abstraction

Regardless of which aggregator we pick, the integration goes behind 
`IBankingClient` in Application/Abstractions/Banking. The implementation 
sits in a new `Roivo.Banking` project (mirrors `Roivo.Aade`'s shape). 
Swapping aggregators later means replacing the implementation, not the 
handlers, entities, or UI.

This protects us from:
- Aggregator going out of business
- Aggregator changing pricing model unfavorably
- Aggregator dropping Greek coverage
- Aggregator quality issues

The abstraction adds maybe 1-2 hours of upfront work and saves weeks 
if we ever need to switch.

## Outreach status

| Aggregator | Date contacted | Response received | Notes |
|------------|----------------|-------------------|-------|
| Noda | 2026-05-26 | Pending | Contact form via noda.live |
| Salt Edge | 2026-05-26 | Pending | Contact form via saltedge.com/pages/contact_us |

## What "decision made" looks like

We have a chosen aggregator when we have:
- Active sandbox access with working credentials
- Verified at least 2 Greek banks visible in their providers list
- Pricing terms documented (or a written commitment that pricing is 
  acceptable for our scale)
- Clear onboarding path to production

Update this document when each milestone hits.

## What this blocks

- M5 implementation (banking integration)
- M6 reconciliation (needs both banking + invoice data)

## What this doesn't block

- M5 architectural planning (abstractions, entities, Hangfire jobs)
- Resilience patterns documentation
- Security checklist documentation  
- UI polish (toast notifications, snackbar feedback)
- Marketing/landing page work
- Mobile responsiveness audit

## Fallback path: direct bank integration

If both Noda and Salt Edge fail (don't respond, refuse access, or 
pricing is prohibitive), the fallback is direct integration with each 
Greek bank's PSD2 API. This requires:
- AISP license (regulatory + financial burden, 6+ months to obtain)
- OR partnership with an existing licensed AISP (faster but adds 
  intermediary cost)
- Separate integration code for each bank's API quirks
- Self-managed compliance (SCA, consent management, monitoring)

This is the path of last resort. Don't pursue until both aggregators 
have been definitively excluded.
