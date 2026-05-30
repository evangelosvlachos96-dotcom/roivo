# Security Checklist

Status of security requirements between current state and production launch.

Status legend:
- ✅ Done
- 🟡 Partial / dev-only
- ❌ Not started
- ⏭️ Deferred (with rationale)

## Credential storage

| Item | Status | Notes |
|------|--------|-------|
| Encryption at rest for AADE creds | ✅ | DataProtection API |
| Per-tenant key scoping | ✅ | Each tenant's data uses tenant-scoped keys |
| DataProtection keys in Key Vault (prod) | ❌ | Currently disk-based; production blocker |
| Encryption for banking tokens (M5) | ❌ | Apply same pattern as AADE |
| Secrets out of appsettings.json | 🟡 | Dev uses appsettings.Development.json (not committed). Prod plan: env vars + Key Vault |

## Authentication

| Item | Status | Notes |
|------|--------|-------|
| Password hashing | ✅ | PBKDF2 100k iterations (ASP.NET Identity default) |
| Account lockout | ❌ | Not enabled in Identity options |
| Password complexity rules | ✅ | Default ASP.NET Identity rules |
| Email verification | ✅ | Required before login |
| 2FA | ❌ | OpenIddict supports it, not enabled. Roadmap: M10+ |
| Session expiration | 🟡 | Default 30 days. Review for production |

## Authorization

| Item | Status | Notes |
|------|--------|-------|
| Per-tenant data isolation via query filter | ✅ | EF Core global filter |
| Tenant filter in raw SQL | 🟡 | Manual; we got it right in InvoiceQueryRepository. **Footgun**: easy to forget |
| Permission checks in handlers | ✅ | BusinessPermissions class, applied before load |
| Audit logging | ✅ | AuditAction enum, audit-after-success pattern |
| Permission system for accountant→client relationships | ✅ | Implemented in M3 |

## Transport security

| Item | Status | Notes |
|------|--------|-------|
| HTTPS in dev | ✅ | https://localhost:7027 |
| HTTPS enforced in prod | ❌ | Configure during Azure deployment |
| HSTS header | ❌ | Configure during Azure deployment |
| TLS 1.2+ minimum | ❌ | Configure during Azure deployment |

## Cookies

| Item | Status | Notes |
|------|--------|-------|
| HttpOnly | ❓ | Need to verify in Identity options |
| Secure (HTTPS only) | ❓ | Need to verify; should be true in prod |
| SameSite | ❓ | Need to verify; should be Lax or Strict |
| Anti-forgery tokens | ✅ | ASP.NET Core default for Razor Pages |

Action: audit current cookie settings, document defaults.

## Input validation

| Item | Status | Notes |
|------|--------|-------|
| Parameterized SQL queries | ✅ | EF Core LINQ + parameterized raw SQL |
| XSS protection | ✅ | Blazor escapes by default |
| File upload validation | N/A | No file uploads currently |
| Input length limits | 🟡 | Mostly enforced via Identity + EF column lengths |

## Audit & monitoring

| Item | Status | Notes |
|------|--------|-------|
| Action audit logs | ✅ | AuditAction enum, written after success |
| Failed login tracking | ❓ | Identity tracks but no alerting |
| Application Insights | ❌ | To configure during Azure deployment |
| Error alerting (Sentry, etc.) | ❌ | Decide tool, configure during pre-launch |

## GDPR / data protection

| Item | Status | Notes |
|------|--------|-------|
| Data export per user | ❌ | Required by GDPR. M11 |
| Data deletion per user | 🟡 | Soft-delete exists, hard-delete flow needed for GDPR |
| Privacy policy | ❌ | Required before launch |
| Cookie consent | ❌ | Required for EU. To add before launch |
| Data processor agreements | ❌ | Need with AADE, banking aggregator, SMTP provider |
| Breach notification process | ❌ | Document before launch |

## OWASP Top 10 review

| Category | Status |
|----------|--------|
| A01 Broken access control | ✅ Tenant isolation + permissions |
| A02 Cryptographic failures | 🟡 DataProtection in dev; prod needs Key Vault |
| A03 Injection | ✅ Parameterized queries everywhere |
| A04 Insecure design | ✅ Layered architecture, reviewed |
| A05 Security misconfiguration | ❌ Production hardening not started |
| A06 Vulnerable components | 🟡 Dependabot not configured |
| A07 Identification/auth failures | 🟡 Account lockout, 2FA pending |
| A08 Software/data integrity | ✅ Standard ASP.NET protections |
| A09 Logging/monitoring | 🟡 Serilog setup; no alerting |
| A10 SSRF | ✅ No user-controlled URL fetches |

## Production launch blockers

The minimum security work between now and launch:
1. DataProtection keys in Azure Key Vault
2. Cookie security audit + production config
3. HTTPS/HSTS enforced in production
4. Account lockout enabled
5. Application Insights + error alerting
6. Privacy policy + cookie consent
7. Dependency vulnerability scanning (Dependabot or similar)

Items deferred to post-launch with explicit rationale:
- 2FA: nice-to-have, not legally required, takes UI effort
- Outbox pattern for audit: scale-driven, not at our volume
- Penetration testing: when we have paying customers
