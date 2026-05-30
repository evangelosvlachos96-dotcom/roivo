# Resilience Patterns

How Roivo handles failures when integrating with third-party systems 
(AADE myDATA, banking aggregators, email providers, etc.).

## Failure modes we design for

Third-party integrations fail in distinct ways, each needing a 
different response. Conflating them leads to either too much 
defensive code (slow, brittle) or too little (data loss, user 
frustration).

### Transient failure

Symptom: 500/502/503/504 errors, timeouts, connection resets. The 
next call would likely succeed.

Response: retry with exponential backoff. Polly policies in HTTP 
client setup.

Standard parameters:
- Max retries: 3
- Initial delay: 1 second
- Backoff multiplier: 2x
- Jitter: yes (avoid thundering herd)
- Total timeout cap: 30 seconds

Current implementations:
- AADE HTTP client (Roivo.Aade) ✅

Future implementations:
- Banking HTTP client (Roivo.Banking) — apply same pattern

### Rate limiting

Symptom: HTTP 429, or 200 with rate-limit error in body.

Response: respect Retry-After header if present; otherwise back off 
significantly. For background syncs, reschedule via Hangfire with 
delay. For interactive operations, show user-facing message and 
fail gracefully.

Current implementations:
- AADE: detects 429 generically, returns RateLimited result variant. 
  Caller decides what to do. **Gap**: doesn't read Retry-After header.

Future improvements:
- Read Retry-After header and use it for the delay
- For Hangfire jobs, reschedule with the suggested delay
- For interactive sync, show user "AADE is rate limiting, retry in 
  N minutes"

### Authentication/credential expiry

Symptom: credentials that worked yesterday no longer work. Provider 
returns 401/403, sometimes with specific error fragments in body.

Response: cannot retry — needs user intervention. Mark connection 
state, notify user, halt automatic syncs for that connection.

Current implementations:
- AADE AFM mismatch: detected via body fragment match, returns 
  AfmMismatch result variant with extracted AFM. User sees 
  permission-aware error message.

**Gaps:**
- No general "credentials revoked/expired" detection beyond AFM 
  mismatch
- No user-facing UI for "your AADE connection has stopped working"
- No automatic disabling of recurring syncs when credentials fail

Future implementations:
- Banking 90-day token expiry (PSD2-mandated) requires this pattern
- Detect provider auth failure → mark connection.Status = Expired
- Notify user via email + UI banner
- Halt syncs until user re-authorizes

### Total outage / sustained unavailability

Symptom: provider is fully down. Multiple consecutive sync attempts 
fail.

Response: circuit breaker pattern (don't keep trying), exponential 
backoff between attempts, status page integration when available.

Current implementations:
- None. We retry per-call but don't track consecutive failures.

**Gap acceptance:** at our scale (pre-launch, <100 customers), 
not having circuit breakers is acceptable. The cost is occasional 
log spam when AADE is down, not customer-visible. Revisit if/when:
- Multiple customers report repeated failures
- Logs show >1% sync failure rate over 24 hours
- We hit the "thundering herd" pattern after a provider recovers

## Per-integration matrix

| Integration | Transient retry | Rate limit | Auth expiry | Outage |
|-------------|-----------------|------------|-------------|--------|
| AADE        | ✅ Polly        | ⚠️ Partial | ⚠️ AFM only | ❌ |
| Banking (M5)| To build        | To build   | To build (90d) | Defer |
| Email (SMTP)| ⚠️ Basic | N/A | ⚠️ Detect bounce | Defer |

## Implementation patterns

### Result-type returns over exceptions

We return result types (Success, Failure variants) from integration 
clients, not exceptions. Exceptions are for programmer errors 
(null args, invariant violations), not expected external failures.

Example: AadeFetchResult has Success, InvalidCredentials, AfmMismatch, 
RateLimited, NetworkError, AadeServerError variants. Handlers 
pattern-match and route accordingly.

### Diagnostic logging

Every external HTTP call logs request URL + response status + body 
preview. Subscription keys and user IDs are NEVER logged. The 
pattern from AadeHttpClient.FetchAndParseAsync is the reference.

### Hangfire job idempotency

Background sync jobs must be safe to run twice. Upserts by natural 
key (AADE mark, bank transaction ID) prevent duplicates. Mark 
advancement is "only forward" to prevent regression.

## Decision: M5 banking applies all patterns from day one

We don't ship M5 with weaker resilience than M4 and retrofit later. 
The banking integration applies:
- Polly retry on transient
- Rate limit handling with Retry-After
- 90-day expiry detection + user-facing flow
- Result types for all client returns

This adds ~2 hours to M5 vs. quick-and-dirty implementation. Pays 
back the first time a bank is briefly unavailable.

## Open questions

- Circuit breaker library: Polly has CircuitBreakerPolicy. Worth 
  enabling for AADE? Probably not yet (gap acceptance above), but 
  document the option.
- Outbox pattern for audit + state changes: would prevent partial 
  failures (action succeeded but audit failed). Currently we accept 
  occasional lost audit entries. Revisit if SOC2 compliance becomes 
  needed.
- Provider health endpoint scraping: AADE and banking providers 
  have status pages. Could parse them to short-circuit calls during 
  known outages. Premature optimization for now.
