// Local development seeder. Creates one accountant tenant with a realistic
// client book: AADE-connected and bank-connected businesses, a year of
// invoices and bank transactions, reconciliation matches at a believable mix of
// confirmed / pending / unmatched, cashflow forecasts, tax obligations and
// notification settings.
//
// It runs against the same ApplicationDbContext and the same Data Protection
// key ring as the app (keys are persisted to the database), so the encrypted
// AADE and banking credentials it writes are ones the running app can actually
// decrypt.
//
// Lives in the repository because staging needs it too: a database with the
// schema and no rows cannot be signed into, and nobody should have to
// reconstruct this fixture from memory to fix that. Run it through
// scripts/seed-database.sh, which names the database you are about to write
// to before it writes anything.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;
using Roivo.Application.Abstractions;
using Roivo.Infrastructure.Persistence;

var connectionString = Environment.GetEnvironmentVariable("ROIVO_DB")
    ?? throw new InvalidOperationException("Set ROIVO_DB.");
var email = Environment.GetEnvironmentVariable("ROIVO_SEED_EMAIL")
    ?? throw new InvalidOperationException("Set ROIVO_SEED_EMAIL.");
var password = Environment.GetEnvironmentVariable("ROIVO_SEED_PASSWORD")
    ?? throw new InvalidOperationException("Set ROIVO_SEED_PASSWORD.");

var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole().SetMinimumLevel(LogLevel.Warning));

// Empty tenant: nothing here should be auto-stamped (every entity gets its
// TenantId set explicitly), and every read in this tool asks for
// IgnoreQueryFilters, so the global filters never need to match.
services.AddSingleton<ITenantContext, SeedTenantContext>();

services.AddDbContextFactory<ApplicationDbContext>(o => o.UseNpgsql(connectionString));
services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));

// Same persistence target AND the same application name as the app. The name
// is the discriminator folded into every protector's purpose chain, so getting
// it wrong would silently produce cipher-text the app cannot decrypt — the
// AADE and banking pages would then show "connected" and fail on first use.
services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("Roivo");

services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.Password.RequiredLength = 12;
        o.User.RequireUniqueEmail = true;
        o.SignIn.RequireConfirmedEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var provider = services.BuildServiceProvider();

var factory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
var protection = provider.GetRequiredService<IDataProtectionProvider>();

var aadeProtector = protection.CreateProtector("Roivo.Aade.Credentials");
var bankProtector = protection.CreateProtector("Roivo.Banking.Credentials");

var today = DateOnly.FromDateTime(DateTime.UtcNow);
var nowUtc = DateTime.UtcNow;
var rng = new Random(20261009);

await using var db = await factory.CreateDbContextAsync();

// ---------------------------------------------------------------- the tenant

var existing = await users.FindByEmailAsync(email);
if (existing is not null)
{
    Console.WriteLine($"User {email} already exists (tenant {existing.TenantId}); wiping their data and reseeding.");
    await WipeTenantAsync(db, existing.TenantId);
}

Guid tenantId;
ApplicationUser user;

if (existing is null)
{
    var tenant = new Tenant
    {
        Name = "Βλάχος & Συνεργάτες Λογιστική",
        Afm = "094014201",
        Type = TenantType.Accountant,
        IsActive = true,
    };
    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();
    tenantId = tenant.Id;

    user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true,
        FullName = "Evangelos Vlachos",
        TenantId = tenantId,
    };

    var created = await users.CreateAsync(user, password);
    if (!created.Succeeded)
        throw new InvalidOperationException(
            "Could not create the user: " + string.Join("; ", created.Errors.Select(e => e.Description)));
}
else
{
    user = existing;
    tenantId = user.TenantId;

    // Make sure the password matches what was asked for, even on a reseed.
    // Remove-then-add rather than ResetPasswordAsync: a reset needs the token
    // providers, which this minimal host does not register.
    if (await users.HasPasswordAsync(user))
    {
        var removed = await users.RemovePasswordAsync(user);
        if (!removed.Succeeded)
            throw new InvalidOperationException(
                "Could not clear the old password: " + string.Join("; ", removed.Errors.Select(e => e.Description)));
    }

    var added = await users.AddPasswordAsync(user, password);
    if (!added.Succeeded)
        throw new InvalidOperationException(
            "Could not set the password: " + string.Join("; ", added.Errors.Select(e => e.Description)));

    user.EmailConfirmed = true;
    await users.UpdateAsync(user);
}

var userId = user.Id.ToString();
Console.WriteLine($"User:   {email}");
Console.WriteLine($"Tenant: {tenantId} (accountant)");

// ------------------------------------------------------------- the clients

var specs = new (string Name, string Afm, string Kad, string Address, bool Aade, bool Bank, string Flavour)[]
{
    ("Παπαδόπουλος Τεχνική ΑΕ",   "094014201", "41200", "Λεωφ. Κηφισίας 124, Αθήνα",       true,  true,  "healthy"),
    ("Αιγαίο Εμπορική ΕΠΕ",       "998765435", "46190", "Ερμού 45, Θεσσαλονίκη",           true,  true,  "tight"),
    ("Delta Software ΙΚΕ",        "090000045", "62010", "Βασ. Σοφίας 12, Αθήνα",           true,  true,  "healthy"),
    ("Θεσσαλονίκη Logistics ΑΕ",  "094289233", "49410", "Μοναστηρίου 80, Θεσσαλονίκη",     true,  false, "messy"),
    ("Κρήτη Αγροτικά ΟΕ",         "800123456", "01130", "Λεωφ. Κνωσού 220, Ηράκλειο",      false, true,  "healthy"),
    ("Νότος Τουριστική ΑΕ",       "997654320", "55100", "Παραλία, Σαντορίνη",              true,  true,  "negative"),
};

var businesses = new List<Business>();

foreach (var spec in specs)
{
    var b = Business.Create(spec.Name, spec.Afm, spec.Kad, spec.Address);
    b.TenantId = tenantId;
    b.SetVatFrequency(spec.Flavour == "healthy" ? VatFrequency.Quarterly : VatFrequency.Monthly);

    if (spec.Name.StartsWith("Παπαδόπουλος", StringComparison.Ordinal))
        b.SetEstimatedPropertyValue(450_000m);

    db.Businesses.Add(b);
    businesses.Add(b);
}

await db.SaveChangesAsync();

// ------------------------------------------------- connections, encrypted

var bankNames = new[] { "Εθνική Τράπεζα", "Alpha Bank", "Eurobank", "Πειραιώς" };
var accountsByBusiness = new Dictionary<Guid, List<BankAccount>>();

for (var i = 0; i < businesses.Count; i++)
{
    var b = businesses[i];
    var spec = specs[i];

    if (spec.Aade)
    {
        // Written through the same protector the app uses, so the AADE page
        // shows a connected business and can decrypt the masked user id.
        b.AadeUserIdEncrypted = aadeProtector.Protect($"roivo_demo_{b.Afm}");
        b.AadeSubscriptionKeyEncrypted = aadeProtector.Protect(Guid.NewGuid().ToString("N"));
        b.RecordAadeSync(nowUtc.AddHours(-rng.Next(1, 20)));
        b.RecordAadeSyncProgress(400000000000000L + rng.Next(1000), 400000000000000L + rng.Next(1000));
    }

    if (spec.Bank)
    {
        b.BankingAccessTokenEncrypted = bankProtector.Protect(Guid.NewGuid().ToString());
        // One consent is deliberately close to lapsing, so the expiry warning
        // has something to show.
        b.RecordBankingConnection(
            bankNames[i % bankNames.Length],
            nowUtc.AddDays(spec.Flavour == "tight" ? 9 : 74));
        b.RecordBankingSyncSuccess(nowUtc.AddHours(-rng.Next(1, 14)));

        var accounts = new List<BankAccount>();
        var howMany = spec.Flavour == "healthy" ? 2 : 1;

        for (var a = 0; a < howMany; a++)
        {
            var account = new BankAccount
            {
                TenantId = tenantId,
                BusinessId = b.Id,
                BankName = bankNames[(i + a) % bankNames.Length],
                Iban = $"GR16 0110 1250 0000 {rng.Next(1000, 9999)} {rng.Next(1000, 9999)} {rng.Next(100, 999)}",
                Currency = Currency.EUR,
                CurrentBalance = spec.Flavour switch
                {
                    "negative" => 1_850m,
                    "tight" => 6_400m,
                    "messy" => 14_200m,
                    _ => 38_500m + rng.Next(0, 12_000),
                },
                ExternalAccountUid = Guid.NewGuid().ToString("N"),
                LastSyncedAt = nowUtc.AddHours(-rng.Next(1, 14)),
            };
            accounts.Add(account);
            db.BankAccounts.Add(account);
        }

        accountsByBusiness[b.Id] = accounts;
    }
}

// One client is deliberately broken, so the failure banners and the alerts
// page have something real to show.
var broken = businesses.Single(x => x.Name.StartsWith("Θεσσαλονίκη", StringComparison.Ordinal));
broken.RecordAadeSyncFailure("InvalidCredentials");
typeof(Business).GetProperty(nameof(Business.AadeLastFailureAt))!
    .SetValue(broken, nowUtc.AddHours(-38));

await db.SaveChangesAsync();

// -------------------------------------------------- invoices + transactions

var counterparties = new (string Name, string Afm)[]
{
    ("ΟΤΕ ΑΕ", "094019245"), ("ΔΕΗ ΑΕ", "090000045"), ("Cosmote", "094019245"),
    ("Plaisio Computers", "094004954"), ("Γερμανός ΑΒΕΕ", "094279801"),
    ("Σκλαβενίτης ΑΕΕ", "094029953"), ("Μεταφορική Ελλάς ΕΠΕ", "800456123"),
    ("Office Solutions ΙΚΕ", "801234567"), ("Attica Media ΑΕ", "094325220"),
};

var invoices = new List<Invoice>();
var transactions = new List<BankTransaction>();
var matches = new List<ReconciliationMatch>();

for (var i = 0; i < businesses.Count; i++)
{
    var b = businesses[i];
    var spec = specs[i];
    var hasBank = accountsByBusiness.TryGetValue(b.Id, out var accounts);

    // Twelve months back, so the month pickers and the 90-day forecast all have
    // history on either side of today.
    for (var monthsBack = 11; monthsBack >= 0; monthsBack--)
    {
        var month = today.AddMonths(-monthsBack);
        var perMonth = spec.Flavour == "messy" ? 14 : 8;

        for (var n = 0; n < perMonth; n++)
        {
            var day = Math.Min(rng.Next(1, 29), DateTime.DaysInMonth(month.Year, month.Month));
            var issued = new DateOnly(month.Year, month.Month, day);
            var cp = counterparties[rng.Next(counterparties.Length)];
            var issuedDirection = n % 3 != 0;

            var net = Math.Round((decimal)(rng.NextDouble() * (issuedDirection ? 4200 : 1500) + 180), 2);
            var vat = Math.Round(net * 0.24m, 2);
            var gross = net + vat;

            var invoice = Invoice.Create(
                b.Id,
                $"4000{rng.Next(10000000, 99999999)}",
                issuedDirection ? InvoiceDirection.Issued : InvoiceDirection.Received,
                issuedDirection ? "1.1" : "13.1",
                issued,
                cp.Afm,
                cp.Name,
                net, vat, gross,
                Currency.EUR,
                null);
            invoice.TenantId = tenantId;
            invoices.Add(invoice);
            db.Invoices.Add(invoice);

            if (!hasBank || accounts is null) continue;

            // Most invoices clear the bank; the rest stay unreconciled so the
            // manual-matching screen has a real queue.
            var clears = spec.Flavour switch
            {
                "messy" => rng.NextDouble() < 0.55,
                "negative" => rng.NextDouble() < 0.70,
                _ => rng.NextDouble() < 0.88,
            };
            if (!clears) continue;

            var account = accounts[rng.Next(accounts.Count)];
            var booked = issued.AddDays(rng.Next(0, 6));
            if (booked > today) continue;

            var transaction = new BankTransaction
            {
                TenantId = tenantId,
                BankAccountId = account.Id,
                ExternalId = Guid.NewGuid().ToString("N"),
                BookingDate = booked,
                ValueDate = booked,
                Amount = issuedDirection ? gross : -gross,
                Currency = Currency.EUR,
                CounterpartyName = cp.Name,
                CounterpartyIban = $"GR96 0810 0010 0000 {rng.Next(1000, 9999)} {rng.Next(1000, 9999)} {rng.Next(100, 999)}",
                Reference = $"INV {invoice.AadeMark[^6..]}",
            };
            transactions.Add(transaction);
            db.BankTransactions.Add(transaction);

            // A believable spread: mostly auto-confirmed, some pending review.
            var confidence = rng.NextDouble() switch
            {
                < 0.72 => 0.95m + (decimal)(rng.NextDouble() * 0.05),
                < 0.90 => 0.70m + (decimal)(rng.NextDouble() * 0.15),
                _ => 0m,
            };
            if (confidence == 0m) continue;

            var match = ReconciliationMatch.CreateFromEngine(b.Id, invoice.Id, transaction.Id, confidence);
            match.TenantId = tenantId;
            matches.Add(match);
            db.ReconciliationMatches.Add(match);

            if (match.Status == ReconciliationMatchStatus.Confirmed)
            {
                invoice.MarkReconciled(nowUtc);
                transaction.IsReconciled = true;
                transaction.ReconciledAt = nowUtc;
                transaction.MatchedInvoiceId = invoice.Id;
            }
        }
    }
}

await db.SaveChangesAsync();

// --------------------------------------------------------------- forecasts

foreach (var b in businesses)
{
    var spec = specs[businesses.IndexOf(b)];
    var balance = accountsByBusiness.TryGetValue(b.Id, out var accs)
        ? accs.Sum(a => a.CurrentBalance)
        : 12_000m;

    for (var d = 0; d <= 90; d++)
    {
        var date = today.AddDays(d);

        var inflow = spec.Flavour switch
        {
            "negative" => 380m + (decimal)(rng.NextDouble() * 260),
            "tight" => 620m + (decimal)(rng.NextDouble() * 420),
            _ => 1_250m + (decimal)(rng.NextDouble() * 900),
        };
        var outflow = spec.Flavour switch
        {
            "negative" => 1_050m + (decimal)(rng.NextDouble() * 500),
            "tight" => 780m + (decimal)(rng.NextDouble() * 420),
            _ => 980m + (decimal)(rng.NextDouble() * 700),
        };

        balance += inflow - outflow;

        var spread = Math.Abs(balance) * 0.12m + 400m;
        var forecast = CashflowForecast.Create(
            b.Id, date,
            Math.Round(inflow, 2),
            Math.Round(outflow, 2),
            Math.Round(balance, 2),
            Math.Round(balance - spread, 2),
            Math.Round(balance + spread, 2));
        forecast.TenantId = tenantId;
        db.CashflowForecasts.Add(forecast);
    }
}

await db.SaveChangesAsync();

// -------------------------------------------------------- tax obligations

var taxPlan = new (TaxType Type, int DaysFromToday, decimal Amount, string Period)[]
{
    (TaxType.Vat,              -12, 3_480.00m, "Q3 2026"),
    (TaxType.Vat,               18, 4_120.50m, "Q4 2026"),
    (TaxType.WithholdingTax,     6, 1_240.00m, "Οκτώβριος 2026"),
    (TaxType.SocialSecurity,    11, 2_050.75m, "Οκτώβριος 2026"),
    (TaxType.IncomeTax,         44, 7_900.00m, "Δόση 3/6 2026"),
    (TaxType.PropertyTax,       27, 1_180.00m, "ΕΝΦΙΑ δόση 2/5"),
    (TaxType.ProfessionalTax,   62,   650.00m, "2026"),
    (TaxType.TaxPrepayment,     51, 2_300.00m, "Προκαταβολή 2027"),
};

foreach (var b in businesses)
{
    foreach (var plan in taxPlan)
    {
        // The property tax only belongs to the client that has a property value.
        if (plan.Type == TaxType.PropertyTax && b.EstimatedPropertyValue is null)
            continue;

        var obligation = TaxObligation.Create(
            b.Id, plan.Type, today.AddDays(plan.DaysFromToday),
            Math.Round(plan.Amount * (0.75m + (decimal)rng.NextDouble() * 0.7m), 2),
            plan.Period);
        obligation.TenantId = tenantId;

        // The oldest one is settled, so the paid/unpaid split is visible.
        if (plan.DaysFromToday < -5 && rng.NextDouble() < 0.6)
            obligation.MarkPaid(obligation.EstimatedAmount, nowUtc.AddDays(-3));

        db.TaxObligations.Add(obligation);
    }
}

await db.SaveChangesAsync();

// ---------------------------------------------------- notification settings

foreach (var b in businesses)
{
    var settings = NotificationSettings.CreateDefault(b.Id, userId, isAccountant: true);
    settings.TenantId = tenantId;
    db.NotificationSettings.Add(settings);
}

await db.SaveChangesAsync();

// ------------------------------------------------------------------ report

Console.WriteLine();
Console.WriteLine("Seeded:");
Console.WriteLine($"  businesses            {businesses.Count}");
Console.WriteLine($"  AADE connected        {specs.Count(s => s.Aade)}");
Console.WriteLine($"  bank connected        {specs.Count(s => s.Bank)}");
Console.WriteLine($"  bank accounts         {accountsByBusiness.Sum(a => a.Value.Count)}");
Console.WriteLine($"  invoices              {invoices.Count}");
Console.WriteLine($"  bank transactions     {transactions.Count}");
Console.WriteLine($"  reconciliation matches{matches.Count,6}  " +
                  $"({matches.Count(m => m.Status == ReconciliationMatchStatus.Confirmed)} confirmed, " +
                  $"{matches.Count(m => m.Status == ReconciliationMatchStatus.Pending)} pending)");
Console.WriteLine($"  cashflow forecasts    {businesses.Count * 91}");
Console.WriteLine($"  tax obligations       {await db.TaxObligations.IgnoreQueryFilters().CountAsync()}");
Console.WriteLine();
Console.WriteLine("Done.");

static async Task WipeTenantAsync(ApplicationDbContext db, Guid tenantId)
{
    // Children first; nothing here relies on cascade behaviour being configured.
    await db.ReconciliationMatches.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.CashflowForecasts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.TaxObligations.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.NotificationSettings.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.BankTransactions.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.BankAccounts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.IncomeBookEntries.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.Invoices.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
    await db.Businesses.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
}

/// <summary>No ambient tenant: the seeder writes across all of them.</summary>
file sealed class SeedTenantContext : ITenantContext
{
    public Guid CurrentTenantId => Guid.Empty;
    public Roivo.Core.Domain.Enums.TenantType CurrentTenantType
        => Roivo.Core.Domain.Enums.TenantType.Accountant;
}
