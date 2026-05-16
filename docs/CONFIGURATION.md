# Roivo Configuration Reference

All configuration lives in `appsettings.json` (defaults) and `appsettings.Development.json` (local overrides, gitignored). Environment variables can override any value using the standard ASP.NET Core double-underscore convention (e.g., `Identity__Password__RequiredLength=14`).

Settings classes are strongly typed (`required init` properties) and validated at startup via `ValidateOnStart()` — the app refuses to start if any required value is missing or invalid.

---

## App

Application-wide identity and infrastructure paths.

| Key | Default | Description |
|---|---|---|
| `Name` | `Roivo` | Application name. Used as the Data Protection key isolation name — changing this invalidates all existing auth cookies and protected payloads. |
| `LogFilePath` | `logs/roivo-.log` | Serilog file sink path. The trailing dash + extension is required for daily rolling (`roivo-20260515.log`). |

## ConnectionStrings

| Key | Default | Description |
|---|---|---|
| `Default` | _(none — required)_ | PostgreSQL connection string. Set per environment. Typical dev value: `Host=localhost;Port=5432;Database=roivo;Username=roivo;Password=...`. |

## Identity:Password

ASP.NET Core Identity password policy. Enforced on registration and password reset.

| Key | Default | Description |
|---|---|---|
| `RequiredLength` | `12` | Minimum password length. |
| `RequireDigit` | `true` | Password must contain at least one digit (0-9). |
| `RequireUppercase` | `true` | Password must contain at least one uppercase letter. |
| `RequireLowercase` | `true` | Password must contain at least one lowercase letter. |
| `RequireNonAlphanumeric` | `true` | Password must contain at least one symbol. |

## Identity:Lockout

Account lockout after consecutive failed login attempts.

| Key | Default | Description |
|---|---|---|
| `MaxFailedAccessAttempts` | `5` | Lock the account after this many consecutive failed sign-ins. |
| `DefaultLockoutMinutes` | `15` | Duration of the lockout, in minutes. |

## Identity:SignIn

| Key | Default | Description |
|---|---|---|
| `RequireConfirmedEmail` | `true` | Users must confirm their email before they can sign in. Disabling this skips the M2 confirmation flow. |

## OpenIddict

OAuth2 / OpenID Connect server endpoints and token lifetimes. Endpoint paths are app-relative.

| Key | Default | Description |
|---|---|---|
| `AccessTokenLifetimeMinutes` | `15` | How long an access token is valid. Keep short — refresh tokens cover longer sessions. |
| `RefreshTokenLifetimeDays` | `30` | How long a refresh token is valid before the user must re-authenticate. |
| `AuthorizationEndpoint` | `/connect/authorize` | OAuth2 authorization endpoint path. |
| `TokenEndpoint` | `/connect/token` | OAuth2 token endpoint path. |
| `UserInfoEndpoint` | `/connect/userinfo` | OIDC userinfo endpoint path. |

## AuthCookie

ASP.NET Core authentication cookie settings.

| Key | Default | Description |
|---|---|---|
| `Name` | `roivo.auth` | Cookie name written to the browser. |
| `ExpirationDays` | `30` | Cookie lifetime. Sliding expiration is enabled, so active sessions extend automatically. |
| `LoginPath` | `/Account/Login` | Where unauthenticated users get redirected. |
| `LogoutPath` | `/Account/Logout` | Logout endpoint (POST). |
| `AccessDeniedPath` | `/Account/AccessDenied` | Where authenticated-but-unauthorized users get redirected (HTTP 403). |

## Smtp

Outbound email via MailKit (used for registration confirmation, password reset). **Dev credentials live in `appsettings.Development.json` and are gitignored.**

| Key | Default | Description |
|---|---|---|
| `Host` | _(none — required)_ | SMTP server hostname (e.g., `sandbox.smtp.mailtrap.io`). |
| `Port` | _(none — required)_ | SMTP port. `2525` for Mailtrap sandbox; `587` for most STARTTLS providers. |
| `Username` | _(none — required)_ | SMTP auth username. |
| `Password` | _(none — required)_ | SMTP auth password. Treat as a secret. |
| `FromEmail` | _(none — required)_ | Address shown in the `From:` header. Production default: `noreply@roivo.gr`. |
| `FromName` | _(none — required)_ | Display name shown in the `From:` header. Production default: `Roivo`. |
| `UseStartTls` | _(none — required)_ | If true, upgrades the connection with STARTTLS after the initial handshake. |

## Serilog

Standard Serilog configuration block read via `ReadFrom.Configuration()`. The file/console sinks are appended in code from the `App:LogFilePath` setting — additional sinks can be added here. See [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) for the schema.

| Key | Default | Description |
|---|---|---|
| `MinimumLevel:Default` | `Information` | Default log level. |
| `MinimumLevel:Override:Microsoft` | `Warning` (dev only) | Suppresses framework noise. |
