using FluentAssertions;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Reconciliation.Commands.ConfirmSuggestedMatch;
using Roivo.Application.Features.Reconciliation.Commands.ManualMatch;
using Roivo.Application.Features.Reconciliation.Commands.RejectSuggestedMatch;
using Roivo.Application.Features.Reconciliation.Commands.RunReconciliation;
using Roivo.Application.Features.Reconciliation.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Reconciliation;

public class ReconciliationHandlerTests
{
    private const string ValidAfm = "094014201";
    private const string UserId = "user-1";

    private static readonly DateOnly IssueDate = new(2026, 6, 15);
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 12, 31);

    private sealed record Sut(
        FakeReconciliationRepository Repo,
        FakeBusinessRepository Businesses,
        FakeAuditWriter Audit,
        Business Business);

    private static Sut BuildSut()
    {
        var repo = new FakeReconciliationRepository();
        var businesses = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        var business = Business.Create("Acme", ValidAfm, null, null);
        businesses.Store[business.Id] = business;
        return new Sut(repo, businesses, audit, business);
    }

    private static RunReconciliationHandler RunHandler(Sut sut)
        => new(new ReconciliationEngine(sut.Repo), sut.Repo, sut.Businesses, sut.Audit, new FakeTenantContext());

    private static Invoice SeedInvoice(Sut sut, decimal gross)
    {
        var invoice = Invoice.Create(
            sut.Business.Id, Guid.NewGuid().ToString(), InvoiceDirection.Issued, "1.1",
            IssueDate, ValidAfm, "ACME AE", gross, 0m, gross, Currency.EUR, null);
        sut.Repo.Invoices.Add(invoice);
        return invoice;
    }

    private static BankTransaction SeedTransaction(Sut sut, decimal amount)
    {
        var transaction = new BankTransaction
        {
            ExternalId = Guid.NewGuid().ToString(),
            BankAccountId = Guid.NewGuid(),
            BookingDate = IssueDate,
            Amount = amount,
            Reference = ValidAfm,
        };
        sut.Repo.Transactions.Add(transaction);
        sut.Repo.TransactionBusiness[transaction.Id] = sut.Business.Id;
        return transaction;
    }

    [Fact]
    public async Task RunReconciliation_ReturnsResults()
    {
        var sut = BuildSut();
        SeedInvoice(sut, 1000m);
        SeedTransaction(sut, 1000m);

        var result = await RunHandler(sut).Handle(new RunReconciliationCommand(sut.Business.Id, From, To));

        var success = result.Should().BeOfType<RunReconciliationResult.Success>().Subject;
        success.Result.Summary.AutoMatchedCount.Should().Be(1);
        success.PersistedMatches.Should().Be(1);

        sut.Repo.Matches.Should().ContainSingle()
            .Which.Status.Should().Be(ReconciliationMatchStatus.Confirmed);

        // A confirmed match marks both sides reconciled.
        sut.Repo.Invoices[0].IsReconciled.Should().BeTrue();
        sut.Repo.Transactions[0].IsReconciled.Should().BeTrue();

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.ReconciliationRun);
    }

    [Fact]
    public async Task RunReconciliation_InvalidDateRange_IsRejected()
    {
        var sut = BuildSut();

        var result = await RunHandler(sut).Handle(new RunReconciliationCommand(sut.Business.Id, To, From));

        result.Should().BeOfType<RunReconciliationResult.InvalidDateRange>();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task RunReconciliation_UnknownBusiness_IsNotFound()
    {
        var sut = BuildSut();

        var result = await RunHandler(sut).Handle(new RunReconciliationCommand(Guid.NewGuid(), From, To));

        result.Should().BeOfType<RunReconciliationResult.NotFound>();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmMatch_UpdatesStatus()
    {
        var sut = BuildSut();
        var invoice = SeedInvoice(sut, 1000m);
        var transaction = SeedTransaction(sut, 1000m);

        var match = ReconciliationMatch.CreateFromEngine(sut.Business.Id, invoice.Id, transaction.Id, 0.6m);
        sut.Repo.Matches.Add(match);

        var handler = new ConfirmSuggestedMatchHandler(sut.Repo, sut.Audit, new FakeTenantContext());
        var result = await handler.Handle(new ConfirmSuggestedMatchCommand(match.Id, UserId));

        result.Should().BeOfType<ConfirmSuggestedMatchResult.Success>();
        match.Status.Should().Be(ReconciliationMatchStatus.Confirmed);
        match.MatchedByUserId.Should().Be(UserId);

        invoice.IsReconciled.Should().BeTrue();
        transaction.IsReconciled.Should().BeTrue();
        transaction.MatchedInvoiceId.Should().Be(invoice.Id);

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.ReconciliationMatchConfirmed);
    }

    [Fact]
    public async Task ConfirmMatch_AlreadyDecided_ReportsNotPending()
    {
        var sut = BuildSut();
        var invoice = SeedInvoice(sut, 1000m);
        var transaction = SeedTransaction(sut, 1000m);

        // An automatic match is born confirmed, so it is never pending.
        var match = ReconciliationMatch.CreateFromEngine(sut.Business.Id, invoice.Id, transaction.Id, 0.95m);
        sut.Repo.Matches.Add(match);

        var handler = new ConfirmSuggestedMatchHandler(sut.Repo, sut.Audit, new FakeTenantContext());
        var result = await handler.Handle(new ConfirmSuggestedMatchCommand(match.Id, UserId));

        result.Should().BeOfType<ConfirmSuggestedMatchResult.NotPending>()
            .Which.Status.Should().Be(nameof(ReconciliationMatchStatus.Confirmed));
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmMatch_Missing_IsNotFound()
    {
        var sut = BuildSut();
        var handler = new ConfirmSuggestedMatchHandler(sut.Repo, sut.Audit, new FakeTenantContext());

        var result = await handler.Handle(new ConfirmSuggestedMatchCommand(Guid.NewGuid(), UserId));

        result.Should().BeOfType<ConfirmSuggestedMatchResult.NotFound>();
    }

    [Fact]
    public async Task RejectMatch_UpdatesStatus()
    {
        var sut = BuildSut();
        var invoice = SeedInvoice(sut, 1000m);
        var transaction = SeedTransaction(sut, 1000m);

        var match = ReconciliationMatch.CreateFromEngine(sut.Business.Id, invoice.Id, transaction.Id, 0.6m);
        sut.Repo.Matches.Add(match);

        var handler = new RejectSuggestedMatchHandler(sut.Repo, sut.Audit, new FakeTenantContext());
        var result = await handler.Handle(new RejectSuggestedMatchCommand(match.Id, UserId, "Not this one"));

        result.Should().BeOfType<RejectSuggestedMatchResult.Success>();
        match.Status.Should().Be(ReconciliationMatchStatus.Rejected);
        match.Notes.Should().Be("Not this one");

        // A rejection releases both sides back to the queue.
        invoice.IsReconciled.Should().BeFalse();
        transaction.IsReconciled.Should().BeFalse();

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.ReconciliationMatchRejected);
    }

    [Fact]
    public async Task ManualMatch_CreatesMatch()
    {
        var sut = BuildSut();
        var invoice = SeedInvoice(sut, 1000m);
        var transaction = SeedTransaction(sut, 985m);

        var handler = new ManualMatchHandler(sut.Repo, sut.Audit, new FakeTenantContext());
        var result = await handler.Handle(new ManualMatchCommand(
            sut.Business.Id, invoice.Id, transaction.Id, UserId, "Partial settlement"));

        result.Should().BeOfType<ManualMatchResult.Success>();

        var match = sut.Repo.Matches.Should().ContainSingle().Subject;
        match.MatchType.Should().Be(ReconciliationMatchType.Manual);
        match.Status.Should().Be(ReconciliationMatchStatus.Confirmed);
        match.MatchConfidence.Should().Be(1.0m, "a human decision outranks any score");
        match.MatchedByUserId.Should().Be(UserId);

        invoice.IsReconciled.Should().BeTrue();
        transaction.IsReconciled.Should().BeTrue();

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.ReconciliationMatchCreatedManually);
    }

    [Fact]
    public async Task ManualMatch_ForeignRows_AreNotFound()
    {
        var sut = BuildSut();
        var invoice = SeedInvoice(sut, 1000m);

        var handler = new ManualMatchHandler(sut.Repo, sut.Audit, new FakeTenantContext());
        var result = await handler.Handle(new ManualMatchCommand(
            sut.Business.Id, invoice.Id, Guid.NewGuid(), UserId, null));

        result.Should().BeOfType<ManualMatchResult.NotFound>();
        sut.Repo.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task ManualMatch_SideAlreadyReconciled_IsRefused()
    {
        var sut = BuildSut();
        var invoice = SeedInvoice(sut, 1000m);
        var first = SeedTransaction(sut, 1000m);
        var second = SeedTransaction(sut, 1000m);

        var handler = new ManualMatchHandler(sut.Repo, sut.Audit, new FakeTenantContext());
        await handler.Handle(new ManualMatchCommand(sut.Business.Id, invoice.Id, first.Id, UserId, null));

        // The same invoice cannot also settle against a second transaction.
        var result = await handler.Handle(new ManualMatchCommand(
            sut.Business.Id, invoice.Id, second.Id, UserId, null));

        result.Should().BeOfType<ManualMatchResult.SideAlreadyReconciled>();
        sut.Repo.Matches.Should().ContainSingle();
    }

    // --------------------------------------------------- batched counts

    /// <summary>
    /// The accountant pages read counts for every client in their book. Asking
    /// per business made the query count scale with the book, so the batched
    /// call has to agree with the single one exactly — otherwise the two
    /// surfaces would quietly report different match rates for the same client.
    /// </summary>
    [Fact]
    public async Task BatchedCounts_MatchTheSingleBusinessCall_ForEveryBusiness()
    {
        var repo = new FakeReconciliationRepository();
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);

        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        SeedInvoice(repo, first, new DateOnly(2026, 1, 10), 100m);
        SeedInvoice(repo, first, new DateOnly(2026, 1, 20), 250m);
        SeedInvoice(repo, second, new DateOnly(2026, 1, 15), 75m);

        var batched = await repo.GetCountsForBusinessesAsync([first, second], from, to);

        foreach (var id in new[] { first, second })
        {
            var single = await repo.GetCountsAsync(id, from, to);
            batched[id].Should().Be(single, "business {0} must read the same either way", id);
        }
    }

    /// <summary>
    /// A client with nothing in the window still needs a row on the accountant
    /// dashboard. Returning no entry would make the handler throw on lookup.
    /// </summary>
    [Fact]
    public async Task BatchedCounts_ReturnZeroedEntries_ForBusinessesWithNothingInTheWindow()
    {
        var repo = new FakeReconciliationRepository();
        var quiet = Guid.NewGuid();

        var batched = await repo.GetCountsForBusinessesAsync(
            [quiet], new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        batched.Should().ContainKey(quiet);
        batched[quiet].Should().Be(ReconciliationCounts.Empty);
    }

    [Fact]
    public async Task BatchedCounts_AreEmpty_WhenAskedForNoBusinesses()
    {
        var repo = new FakeReconciliationRepository();

        var batched = await repo.GetCountsForBusinessesAsync(
            [], new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        batched.Should().BeEmpty();
    }

    private static void SeedInvoice(
        FakeReconciliationRepository repo, Guid businessId, DateOnly issued, decimal gross)
    {
        repo.Invoices.Add(Invoice.Create(
            businessId, Guid.NewGuid().ToString(), InvoiceDirection.Issued, "1.1",
            issued, "094014201", "ACME AE", gross, 0m, gross, Currency.EUR, null));
    }
}
