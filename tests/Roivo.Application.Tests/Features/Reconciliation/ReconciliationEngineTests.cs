using FluentAssertions;
using Roivo.Application.Features.Reconciliation.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Reconciliation;

public class ReconciliationEngineTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly DateOnly IssueDate = new(2026, 6, 15);
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 12, 31);

    private const string CounterpartyAfm = "094014201";

    private static (ReconciliationEngine Engine, FakeReconciliationRepository Repo) BuildSut()
    {
        var repo = new FakeReconciliationRepository();
        return (new ReconciliationEngine(repo), repo);
    }

    private static Invoice SeedInvoice(
        FakeReconciliationRepository repo,
        decimal gross,
        DateOnly? issueDate = null,
        string? counterpartyName = "ACME AE",
        InvoiceDirection direction = InvoiceDirection.Issued)
    {
        var invoice = Invoice.Create(
            BusinessId,
            aadeMark: Guid.NewGuid().ToString(),
            direction: direction,
            invoiceType: "1.1",
            issueDate: issueDate ?? IssueDate,
            counterpartyAfm: CounterpartyAfm,
            counterpartyName: counterpartyName,
            netAmount: gross,
            vatAmount: 0m,
            grossAmount: gross,
            currency: Currency.EUR,
            cancelledByMark: null);

        repo.Invoices.Add(invoice);
        return invoice;
    }

    /// <summary>
    /// A reference carrying the counterparty AFM is what earns the counterparty
    /// points; passing null leaves that component unscored.
    /// </summary>
    private static BankTransaction SeedTransaction(
        FakeReconciliationRepository repo,
        decimal amount,
        DateOnly? bookingDate = null,
        string? reference = CounterpartyAfm,
        string? counterpartyName = null)
    {
        var transaction = new BankTransaction
        {
            ExternalId = Guid.NewGuid().ToString(),
            BankAccountId = Guid.NewGuid(),
            BookingDate = bookingDate ?? IssueDate,
            Amount = amount,
            Reference = reference,
            CounterpartyName = counterpartyName,
        };

        repo.Transactions.Add(transaction);
        repo.TransactionBusiness[transaction.Id] = BusinessId;
        return transaction;
    }

    [Fact]
    public async Task ExactAmountAndDate_ReturnsAutoMatch()
    {
        var (engine, repo) = BuildSut();
        var invoice = SeedInvoice(repo, 1000m);
        var transaction = SeedTransaction(repo, 1000m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().ContainSingle();
        var match = result.AutoMatched[0];
        match.InvoiceId.Should().Be(invoice.Id);
        match.BankTransactionId.Should().Be(transaction.Id);
        match.MatchType.Should().Be(ReconciliationMatchType.Automatic);
        match.Status.Should().Be(ReconciliationMatchStatus.Confirmed);
        // Exact amount 0.4 + same day 0.3 + counterparty 0.3.
        match.MatchConfidence.Should().Be(1.0m);

        result.Suggested.Should().BeEmpty();
        result.UnmatchedInvoices.Should().BeEmpty();
        result.UnmatchedTransactions.Should().BeEmpty();
        result.Summary.AutoMatchRate.Should().Be(1m);
        result.Summary.TotalReconciledAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task AmountWithinTolerance_ReturnsSuggested()
    {
        var (engine, repo) = BuildSut();
        SeedInvoice(repo, 1000m, counterpartyName: null);

        // 0.3% off, inside the 0.5% default, and no counterparty signal:
        // 0.2 + 0.3 = 0.5, which suggests without auto-confirming.
        SeedTransaction(repo, 1003m, reference: null);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().BeEmpty();
        result.Suggested.Should().ContainSingle();
        result.Suggested[0].MatchType.Should().Be(ReconciliationMatchType.Suggested);
        result.Suggested[0].Status.Should().Be(ReconciliationMatchStatus.Pending);
        result.Suggested[0].MatchConfidence.Should().Be(0.5m);
    }

    [Fact]
    public async Task NoMatch_ReturnsUnmatched()
    {
        var (engine, repo) = BuildSut();
        var invoice = SeedInvoice(repo, 1000m);
        var transaction = SeedTransaction(repo, 4500m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().BeEmpty();
        result.Suggested.Should().BeEmpty();
        result.UnmatchedInvoices.Should().ContainSingle().Which.Id.Should().Be(invoice.Id);
        result.UnmatchedTransactions.Should().ContainSingle().Which.Id.Should().Be(transaction.Id);
        result.Summary.AutoMatchRate.Should().Be(0m);
    }

    [Fact]
    public async Task AlreadyReconciled_IsSkipped()
    {
        var (engine, repo) = BuildSut();
        var invoice = SeedInvoice(repo, 1000m);
        invoice.MarkReconciled(DateTime.UtcNow);
        SeedTransaction(repo, 1000m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().BeEmpty();
        result.UnmatchedInvoices.Should().BeEmpty();
        result.Summary.TotalInvoices.Should().Be(0);
        // The transaction is still open; only the invoice was already accounted for.
        result.UnmatchedTransactions.Should().ContainSingle();
    }

    [Fact]
    public async Task MultipleTransactions_BestMatchWins()
    {
        var (engine, repo) = BuildSut();
        var invoice = SeedInvoice(repo, 1000m);

        var weak = SeedTransaction(repo, 1004m, IssueDate.AddDays(5), reference: null);
        var best = SeedTransaction(repo, 1000m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().ContainSingle();
        result.AutoMatched[0].BankTransactionId.Should().Be(best.Id);
        result.AutoMatched[0].InvoiceId.Should().Be(invoice.Id);

        // The loser stays available rather than being consumed by the weaker score.
        result.UnmatchedTransactions.Should().ContainSingle().Which.Id.Should().Be(weak.Id);
    }

    [Fact]
    public async Task EmptyInvoices_ReturnsEmpty()
    {
        var (engine, repo) = BuildSut();
        SeedTransaction(repo, 1000m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().BeEmpty();
        result.Suggested.Should().BeEmpty();
        result.UnmatchedInvoices.Should().BeEmpty();
        result.UnmatchedTransactions.Should().ContainSingle();
        result.Summary.TotalInvoices.Should().Be(0);
        result.Summary.AutoMatchRate.Should().Be(0m);
    }

    [Fact]
    public async Task EmptyTransactions_AllInvoicesUnmatched()
    {
        var (engine, repo) = BuildSut();
        SeedInvoice(repo, 1000m);
        SeedInvoice(repo, 2000m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().BeEmpty();
        result.UnmatchedInvoices.Should().HaveCount(2);
        result.UnmatchedTransactions.Should().BeEmpty();
        result.Summary.UnmatchedInvoiceCount.Should().Be(2);
    }

    [Fact]
    public async Task RejectedPair_IsNotReproposed()
    {
        var (engine, repo) = BuildSut();
        var invoice = SeedInvoice(repo, 1000m);
        var transaction = SeedTransaction(repo, 1000m);
        repo.RejectedPairs.Add((invoice.Id, transaction.Id));

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().BeEmpty();
        result.Suggested.Should().BeEmpty();
        result.UnmatchedInvoices.Should().ContainSingle();
    }

    [Fact]
    public async Task WrongDirection_DoesNotMatch()
    {
        var (engine, repo) = BuildSut();
        SeedInvoice(repo, 1000m);

        // A debit cannot settle an invoice we issued, however well it otherwise scores.
        SeedTransaction(repo, -1000m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.AutoMatched.Should().BeEmpty();
        result.Suggested.Should().BeEmpty();
        result.UnmatchedInvoices.Should().ContainSingle();
    }

    [Fact]
    public async Task CancelledInvoice_IsExcluded()
    {
        var (engine, repo) = BuildSut();
        var invoice = SeedInvoice(repo, 1000m);
        invoice.RecordCancellation("CANCEL-1");
        SeedTransaction(repo, 1000m);

        var result = await engine.ReconcileAsync(BusinessId, From, To);

        result.Summary.TotalInvoices.Should().Be(0);
        result.AutoMatched.Should().BeEmpty();
    }

    [Fact]
    public async Task CustomRule_WidensAmountTolerance()
    {
        var (engine, repo) = BuildSut();
        SeedInvoice(repo, 1000m, counterpartyName: null);
        SeedTransaction(repo, 1020m, reference: null);

        // 2% off is outside the 0.5% default, so nothing matches by default.
        var withDefaults = await engine.ReconcileAsync(BusinessId, From, To);
        withDefaults.Suggested.Should().BeEmpty();

        repo.ActiveRule = ReconciliationRule.Create(
            BusinessId, "Loose", amountTolerancePercent: 3m, dateToleranceDays: 3, counterpartyPattern: null);

        var withRule = await engine.ReconcileAsync(BusinessId, From, To);
        withRule.Suggested.Should().ContainSingle();
    }
}
