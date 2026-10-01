# Roivo Configuration Reference

All configuration lives in `appsettings.json` (defaults) and `appsettings.Development.json` (local overrides, gitignored). Environment variables can override any value using the standard ASP.NET Core double-underscore convention (e.g., `Identity__Password__RequiredLength=14`).

Settings classes are strongly typed (`required init` properties) and validated at startup via `ValidateOnStart()` — the app refuses to start if any required value is missing or invalid.

---

## Deployment environment variables

`__` is the section separator, so a lone `_` can never sit between words: the built-in spelling of `Smtp:FromEmail` is `SMTP__FROMEMAIL`, not `SMTP_FROM_EMAIL`. Key lookup *is* case-insensitive, so `SMTP__FROMEMAIL` and `Smtp__FromEmail` are equivalent.

For readability, `Program.cs` maps the names below onto configuration keys. They are registered after every other source, so a variable set here wins over `appsettings.json`, the `.env` file, and the `__` spelling of the same key. Adding a name to the table means adding it to the map in `Program.cs` too.

| Environment variable | Configuration key | Required to start |
|---|---|---|
| `DATABASE_CONNECTION_STRING` | `ConnectionStrings:Default` | **Yes** |
| `SMTP_HOST` | `Smtp:Host` | **Yes** |
| `SMTP_PORT` | `Smtp:Port` | **Yes** |
| `SMTP_USERNAME` | `Smtp:Username` | **Yes** |
| `SMTP_PASSWORD` | `Smtp:Password` | **Yes** |
| `SMTP_FROM_EMAIL` | `Smtp:FromEmail` | **Yes** |
| `SMTP_FROM_NAME` | `Smtp:FromName` | **Yes** |
| `SMTP_USE_STARTTLS` | `Smtp:UseStartTls` | **Yes** |
| `ENABLE_BANKING_BASE_URL` | `EnableBanking:BaseUrl` | **Yes** |
| `ENABLE_BANKING_APPLICATION_ID` | `EnableBanking:ApplicationId` | **Yes** |
| `ENABLE_BANKING_PRIVATE_KEY_PATH` | `EnableBanking:PrivateKeyPath` | **Yes** |
| `ENABLE_BANKING_REDIRECT_URL` | `EnableBanking:RedirectUrl` | **Yes** |
| `APP_PUBLIC_BASE_URL` | `App:PublicBaseUrl` | No - defaults to `https://roivo.gr` |
| `HANGFIRE_AUTHORIZED_EMAILS` | `Hangfire:Dashboard:AuthorizedEmails` | No - empty denies all |

The four `ENABLE_BANKING_*` names are the same ones `DotEnvFile` reads out of `.env` for local development, so one spelling works in both places. `.env` additionally accepts `ENABLE_BANKING_SANDBOX_URL` and `ENABLE_BANKING_API_URL` as aliases for `EnableBanking:BaseUrl`; the environment map accepts only `ENABLE_BANKING_BASE_URL`.

Anything not in this table still takes the `__` spelling - e.g. `IDENTITY__PASSWORD__REQUIREDLENGTH`, `AUTHCOOKIE__EXPIRATIONDAYS`, `AADE__BASEURL`.

Not configuration, but set by the platform: `ASPNETCORE_ENVIRONMENT` (`Staging` / `Production`) and `ASPNETCORE_URLS` (the Dockerfile sets `http://+:8080`).

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
