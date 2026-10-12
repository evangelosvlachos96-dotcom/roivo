using FluentAssertions;
using Roivo.Application.Features.Reconciliation.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Reconciliation;

/// <summary>
/// Exercises the engine against a synthetic dataset shaped like real Greek SMB
/// activity, to hold it to the milestone's accuracy bar rather than only to
/// single-pair unit cases.
/// </summary>
/// <remarks>
/// The data is generated, not sampled from production, so this measures the
/// scoring rules against the messiness we chose to model — late settlement,
/// rounding, renamed counterparties, noise transactions. It is a regression
/// guard on the thresholds, not evidence about real bank data.
/// </remarks>
public class ReconciliationEngineRealisticDatasetTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly DateOnly WindowStart = new(2026, 1, 1);
    private static readonly DateOnly WindowEnd = new(2026, 12, 31);

    private const string CounterpartyAfm = "094014201";

    /// <summary>Accuracy bar from the milestone definition.</summary>
    private const double RequiredAutoMatchRate = 0.70;

    [Fact]
    public async Task HundredCaseDataset_AutoMatchesAtLeastSeventyPercent()
    {
        var repo = new FakeReconciliationRepository();
        var engine = new ReconciliationEngine(repo);
        var random = new Random(Seed: 20261001);

        var expectedPairs = new Dictionary<Guid, Guid>();

        for (var i = 0; i < 100; i++)
        {
            var issueDate = WindowStart.AddDays(random.Next(0, 300));
            var gross = Math.Round((decimal)(random.NextDouble() * 4000 + 50), 2);
            var counterpartyName = $"ΠΕΛΑΤΗΣ {i % 17} AE";

            var invoice = Invoice.Create(
                BusinessId, Guid.NewGuid().ToString(), InvoiceDirection.Issued, "1.1",
                issueDate, CounterpartyAfm, counterpartyName,
                gross, 0m, gross, Currency.EUR, null);
            repo.Invoices.Add(invoice);

            // The settling transaction, with the kinds of drift a real bank feed shows.
            var settlementDate = issueDate.AddDays(random.Next(0, 4));
            var amount = i % 10 == 0
                ? Math.Round(gross * 1.002m, 2)   // small rounding or fee difference
                : gross;

            // A fifth of statements carry no usable counterparty text at all.
            var hasReference = i % 5 != 0;

            var transaction = new BankTransaction
            {
                ExternalId = Guid.NewGuid().ToString(),
                BankAccountId = Guid.NewGuid(),
                BookingDate = settlementDate,
                Amount = amount,
                Reference = hasReference ? $"ΕΞΟΦΛΗΣΗ {CounterpartyAfm}" : null,
                CounterpartyName = hasReference ? counterpartyName : null,
            };

            repo.Transactions.Add(transaction);
            repo.TransactionBusiness[transaction.Id] = BusinessId;
            expectedPairs[invoice.Id] = transaction.Id;
        }

        // Unrelated traffic the engine has to ignore: card fees, transfers, salaries.
        for (var i = 0; i < 40; i++)
        {
            var noise = new BankTransaction
            {
                ExternalId = Guid.NewGuid().ToString(),
                BankAccountId = Guid.NewGuid(),
                BookingDate = WindowStart.AddDays(random.Next(0, 300)),
                Amount = Math.Round((decimal)(random.NextDouble() * 900 + 5), 2),
                Reference = "ΠΡΟΜΗΘΕΙΑ",
                CounterpartyName = "ΤΡΑΠΕΖΑ",
            };
            repo.Transactions.Add(noise);
            repo.TransactionBusiness[noise.Id] = BusinessId;
        }

        var result = await engine.ReconcileAsync(BusinessId, WindowStart, WindowEnd);

        var correct = result.AutoMatched.Count(m =>
            expectedPairs.TryGetValue(m.InvoiceId, out var expected) && expected == m.BankTransactionId);

        var accuracy = (double)correct / expectedPairs.Count;
        accuracy.Should().BeGreaterThanOrEqualTo(RequiredAutoMatchRate,
            "the milestone requires 70%+ of cases to auto-match correctly");

        // Every automatic match must be right: a wrong auto-match is worse than
        // no match, because nobody reviews it.
        result.AutoMatched.Should().OnlyContain(m =>
            expectedPairs[m.InvoiceId] == m.BankTransactionId);
    }

    [Fact]
    public async Task NoiseOnlyDataset_ProducesNoMatches()
    {
        var repo = new FakeReconciliationRepository();
        var engine = new ReconciliationEngine(repo);

        var invoice = Invoice.Create(
            BusinessId, Guid.NewGuid().ToString(), InvoiceDirection.Issued, "1.1",
            new DateOnly(2026, 3, 10), CounterpartyAfm, "ΠΕΛΑΤΗΣ AE",
            1500m, 0m, 1500m, Currency.EUR, null);
        repo.Invoices.Add(invoice);

        // Nothing close in amount, date, or party.
        foreach (var amount in new[] { 12.50m, 89m, 430m, 7200m })
        {
            var noise = new BankTransaction
            {
                ExternalId = Guid.NewGuid().ToString(),
                BankAccountId = Guid.NewGuid(),
                BookingDate = new DateOnly(2026, 8, 1),
                Amount = amount,
                Reference = "ΠΡΟΜΗΘΕΙΑ",
            };
            repo.Transactions.Add(noise);
            repo.TransactionBusiness[noise.Id] = BusinessId;
        }

        var result = await engine.ReconcileAsync(BusinessId, WindowStart, WindowEnd);

        result.AutoMatched.Should().BeEmpty();
        result.Suggested.Should().BeEmpty();
        result.UnmatchedInvoices.Should().ContainSingle();
    }
}
