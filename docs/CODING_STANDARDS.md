# Roivo Coding Standards

Conventions for C# code in the Roivo solution. These are mandatory unless a
section explicitly marks a rule as optional. When a rule conflicts with
generated code or a third-party framework requirement, the framework wins —
note the deviation in a comment.

---

## 1. Constructor null-guards (mandatory)

Every reference-type constructor parameter that the type stores or dereferences
must be guarded with `ArgumentNullException.ThrowIfNull`. Guards run before any
field assignment.

```csharp
public sealed class TenantOnboardingService
{
    private readonly RoivoDbContext _db;
    private readonly ILogger<TenantOnboardingService> _logger;

    public TenantOnboardingService(RoivoDbContext db, ILogger<TenantOnboardingService> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);

        _db = db;
        _logger = logger;
    }
}
```

### When to apply

- Any reference-type parameter on a constructor of a service, handler,
  repository, controller, page model, or other class instantiated by the DI
  container or by application code.
- Public/internal constructors on domain entities and aggregates.
- Method parameters that are reference types and are dereferenced inside the
  method, when the method is on a public API boundary.

### When NOT to apply

- **Value types** (`int`, `Guid`, `DateOnly`, `decimal`, enums, structs). They
  cannot be null. If you need to reject `Guid.Empty` or similar, use an
  `ArgumentException` with a specific message, not `ThrowIfNull`.
- **Nullable reference types** (`string?`, `Tenant?`). The type already
  declares that null is a legal value; guarding contradicts the signature.
- **Primary constructors on records, DTOs, and value objects.** Primary
  constructor parameters cannot be guarded inline. For these types, prefer
  `required` members or validate at the construction site instead. See §2.
- Parameters that are immediately passed through to a base constructor or
  another method that itself guards — don't double-guard.

---

## 2. Primary constructors

C# 12 primary constructors are allowed in narrow cases and banned in others.

### Allowed

- **Records and record structs** — primary constructor is idiomatic.
  ```csharp
  public sealed record TenantSummary(Guid TenantId, string DisplayName, string Afm);
  ```
- **DTOs** carrying request/response shapes across project boundaries.
- **Value objects** (e.g. `Afm`, `Iban`, `Money`) where the constructor's sole
  job is to validate and assign a small number of fields. Validation goes in
  the body, not as a parameter constraint.

### NOT allowed

- **Services, handlers, repositories, page models, controllers, hosted
  services, or any class resolved from the DI container.** Use an explicit
  constructor so null-guards (§1) and field initialization are visible and
  reviewable. Primary-constructor parameters become hidden captured locals
  that obscure DI dependencies and prevent the standard guard pattern.
- **Entity types tracked by EF Core.** EF requires accessible constructors and
  the team standard is an explicit parameterless or fully-explicit constructor
  with backing fields.

If a class needs DI but is also a record (rare), prefer composing rather than
inheriting the record shape — keep the DI-resolved type as a regular class.

---

## 3. Comments

Comments explain **why**, never **what**. The code already says what it does;
identifiers and types carry that load. A comment earns its place only when a
future reader would otherwise have to reconstruct hidden context: a non-obvious
constraint, a workaround for a specific bug, a deliberate deviation from the
obvious approach.

### Good

```csharp
// AADE rejects requests faster than 1/sec per AFM; throttle even though our
// own infra could handle bursts.
await _aadeThrottle.WaitAsync(ct);
```

```csharp
// EF Core 10 global query filter strips TenantId; bypass it here because the
// background job is intentionally cross-tenant.
var rows = await _db.Tenants.IgnoreQueryFilters().ToListAsync(ct);
```

### Bad

```csharp
// Increment the counter
counter++;

// Loop over tenants
foreach (var tenant in tenants) { ... }

// Added for the registration flow (issue #42)
ArgumentNullException.ThrowIfNull(email);
```

The first two restate the code. The third references a transient context
(an issue number, the calling flow) that belongs in the commit message or PR
description and rots as the codebase evolves.

### XML doc comments

Required on:

- Public types and members in projects consumed by other projects in the
  solution (e.g. `Roivo.Domain`, `Roivo.Application`, contracts exposed from
  `Roivo.Infrastructure` to `Roivo.Web`).
- Public extension methods.
- Anything decorated with `[PublicAPI]` or surfaced through OpenAPI.

Not required on:

- `internal` and `private` members.
- Razor `PageModel` classes and their handlers.
- Test classes and test methods.

Keep doc comments short. One `<summary>` line for the intent, `<param>` only
when the parameter name is genuinely ambiguous, `<returns>` only when the
return semantics aren't obvious from the type.

---

## 4. Language conventions

- **Code, comments, commit messages, log messages, exception messages, XML
  docs, and identifier names: English only.** This includes thrown exception
  `Message` strings — those are diagnostic surfaces, not user-facing.
- **User-facing strings: Greek (primary) and English (secondary).** Anything
  that ends up in a Razor view, an email template, a validation summary shown
  in the UI, or a localized resource file follows the bilingual UI plan from
  Week 11 of the MVP. Default culture is `el-GR`; English strings live behind
  the same resource keys with the `en` culture.
- Greek text in `.resx` resources, not inlined in `.cshtml` or `.razor`. The
  one exception is purely structural markup with no translatable content.
- Do not mix scripts inside an identifier. `AfmValidator` is correct;
  `ΑΦΜValidator` is not.

---

## 5. Naming conventions

| Kind                                            | Convention      | Example                       |
| ----------------------------------------------- | --------------- | ----------------------------- |
| Namespaces, types, public members, constants    | `PascalCase`    | `TenantOnboardingService`     |
| Method parameters and local variables           | `camelCase`     | `tenantId`, `cancellationToken` |
| Private and internal fields                     | `_camelCase`    | `_db`, `_logger`              |
| Interfaces                                      | `IPascalCase`   | `ITenantRepository`           |
| Generic type parameters                         | `TPascalCase`   | `TEntity`, `TResult`          |
| Async methods                                   | `…Async` suffix | `LoadAsync`, `SyncMyDataAsync` |
| Boolean members                                 | affirmative     | `IsActive`, not `IsNotActive` |

Other rules:

- No Hungarian notation, no `m_` prefixes, no `s_` for statics.
- Acronyms longer than two letters are PascalCased: `AfmValidator`, `IbanParser`
  — not `AFMValidator`. Two-letter acronyms stay uppercase: `IO`, `DB`.
- File name matches the primary type it contains.
- One public type per file. Nested private types are fine.

---

## 6. Async patterns

- Methods that perform I/O, await another async method, or return `Task`/
  `ValueTask`/`IAsyncEnumerable<T>` end in `Async`.
- Every async method on a public boundary accepts a `CancellationToken`,
  named `cancellationToken` (or `ct` in tight internal call chains where the
  team has already adopted the short form consistently), and passes it through
  to every awaited call that accepts one.
- `CancellationToken` is the **last** parameter, after any `params` array if
  one exists.
- **Never** call `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, or
  `Task.Run(async …).Result` on an async operation in production code. These
  cause deadlocks in some hosting models and waste a thread-pool thread in
  all of them.
- Don't return `Task` from a method that has no `await` and could simply
  return a value — return the value directly or, if you must produce a `Task`,
  use `Task.FromResult` / `ValueTask.FromResult`.
- `async void` is reserved for event handlers. Anywhere else, return `Task`.
- For hot paths returning synchronously most of the time, prefer `ValueTask<T>`.
- Configure-await: we run on ASP.NET Core (no synchronization context), so
  `ConfigureAwait(false)` is not required in app code. It is still required
  inside library projects that may run in other contexts
  (`Roivo.Domain`, `Roivo.Application`).

---

## 7. Error handling

- **No bare `catch (Exception)` blocks.** Catch the specific exception type
  you can actually recover from. If you catch `Exception` for a logging or
  audit boundary, re-throw it or wrap and re-throw — never swallow.
- **No `catch` without a body.** An empty catch is a silent bug.
- **External APIs (AADE, GoCardless, SMTP, etc.) use Polly** for retry,
  timeout, and circuit-breaker policies. This becomes mandatory from M4
  onward, when the AADE integration lands. Until then, code calling external
  services should still surface failures cleanly so the Polly wrapping is a
  drop-in change.
- **Audit logging must never crash the request.** Audit writes are wrapped
  so a failure to persist an audit row is logged via `ILogger` and discarded;
  the originating request continues. The inverse — letting a failed audit
  write surface as a 500 — is a correctness regression.
- Domain validation failures throw domain exceptions (e.g. `AfmInvalidException`)
  or return a result object — pick one per bounded context and stay
  consistent. Don't reach for `InvalidOperationException` as a catch-all.
- Use `throw;` to rethrow, never `throw ex;` (the latter resets the stack
  trace).
- Exceptions thrown from constructors must leave no partially-initialized
  state behind. Prefer guarding (§1) before any field mutation.

---

## DbContext usage in Blazor Server

Blazor Server keeps a single scoped DbContext per circuit. Components that render concurrently (e.g., layout + page) can issue overlapping queries against the same context, which Npgsql cannot handle — resulting in "A second operation was started on this context instance" or NpgsqlOperationInProgressException.

Pattern:

- DbContextFactory is registered; the scoped ApplicationDbContext is sourced from it (one fresh context per scope).
- Razor Components that own a single lifecycle and do sequential queries can inject ApplicationDbContext directly.
- Razor Components that render concurrently with other components (layouts, cross-cutting widgets, anything that calls UserManager or runs a query during navigation) should inject IDbContextFactory<ApplicationDbContext> and create short-lived contexts inside each method:

```csharp
  await using var db = await DbFactory.CreateDbContextAsync();
  var result = await db.Foos.ToListAsync();
```

- Background services and Hangfire jobs always use the factory — they have no DI scope of their own.
- Identity (UserManager, SignInManager) keeps its standard wiring; the factory-backed scoped DbContext means UserManager's queries no longer collide with page queries when both fire in the same render tick.

---

## Filtering by entity state when loading by ID

The global tenant query filter scopes all queries by TenantId. It does NOT scope by IsActive or other state flags — those are business-rule concerns specific to the operation.

When loading an entity by ID for an action:

- Filter by IsActive (or relevant state) at the callsite
- Don't rely on the global filter to hide deactivated/archived records

Example:

```csharp
// Loading a business for an "edit active business" page
var business = await db.Businesses
    .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
```

When loading an entity for a flow that explicitly needs inactive records (e.g., a "reactivate" action, an "archived items" tab), filter by IsActive == false instead. The global filter never sees IsActive — every callsite decides.

---

## Layered architecture

Strict dependency flow:

Roivo.Core → no references
Roivo.Application → Roivo.Core only
Roivo.Infrastructure → Roivo.Core + Roivo.Application (implements abstractions)
Roivo.Web → all three

### What lives where

**Roivo.Core**
- Domain entities (Business, Tenant, BankAccount, etc.)
- Value objects (Currency enum, etc.)
- Domain validators (AfmValidator)
- Domain interfaces (ITenantScoped)
- NO EF Core. NO infrastructure concerns.

**Roivo.Application**
- Features/{Domain}/Commands/{ActionName}/ — Command + Handler + Result trio
- Features/{Domain}/Queries/{QueryName}/ — Query + Handler trio
- Abstractions/ — repository interfaces, infrastructure-facing interfaces
- Common/ — shared base types

Features are organized vertically. Working on businesses? Everything in Features/Businesses/. Working on auth? Features/Auth/.

**Roivo.Infrastructure**
- Persistence/ApplicationDbContext.cs
- Persistence/Configurations/ — IEntityTypeConfiguration<T> classes
- Persistence/Repositories/ — implementations of Roivo.Application.Abstractions interfaces
- Auditing/AuditWriter.cs
- Identity/HttpTenantContext.cs, TenantClaimsPrincipalFactory.cs
- External integrations (later: AADE, PSD2)
- EF Core lives ONLY here.

**Roivo.Web**
- Razor components and Razor Pages
- DI registration in Configuration/ extensions
- Components inject handlers from Application, NOT repositories or DbContext
- Components are thin: render UI, call handlers, map result to UI state

### Component discipline rules

A Razor component must not:
- Import Microsoft.EntityFrameworkCore
- Import Roivo.Infrastructure
- Inject DbContext, DbContextFactory, or any repository
- Call SaveChangesAsync, Add, Update, etc.
- Serialize JSON for audit logging
- Use System.Text.Json directly

A Razor component should:
- Inject command/query handlers
- Build commands/queries from form input
- Pattern-match on result types to determine UI behavior
- Stay focused on rendering and user interaction

---

## DbContext lifecycle rule (MANDATORY)

ApplicationDbContext must never be injected directly into types we author.

- Repositories, audit writers, claims factories, and any custom service that touches the database must inject IDbContextFactory<ApplicationDbContext>.
- Each method creates a fresh context via `await using var db = await _factory.CreateDbContextAsync(ct);`.
- No private fields holding DbContext state across method calls.
- Repository write methods save internally before returning.
- Entities returned from a repository are detached — mutate them, then call UpdateAsync to persist.

The only exception: framework types we don't control (Identity's UserStore, OpenIddict's stores, Hangfire's job storage). These manage their own context scoping.

Why this rule exists:
- Blazor Server shares scoped services across prerender + interactive render.
- Two near-simultaneous calls on the same context cause "A second operation was started on this context instance" errors.
- Stateless services with per-method contexts make this class of bug impossible.

Enforcement:
- grep the codebase for "ApplicationDbContext" as a constructor parameter — should appear ONLY in framework-related registration code, never in our own services.

---
