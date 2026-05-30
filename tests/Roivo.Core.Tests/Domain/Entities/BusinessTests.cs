using FluentAssertions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Core.Tests.Domain.Entities;

public class BusinessTests
{
    private const string ValidAfm = "094014201";
    private const string AnotherValidAfm = "123456783";

    [Fact]
    public void Create_throws_when_AFM_invalid()
    {
        var act = () => Business.Create("Acme", "12345", null, null);

        act.Should().Throw<InvalidAfmException>()
            .Which.AttemptedAfm.Should().Be("12345");
    }

    [Fact]
    public void Create_throws_when_name_empty()
    {
        var act = () => Business.Create("   ", ValidAfm, null, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_trims_whitespace_on_all_fields()
    {
        var b = Business.Create("  Acme  ", $"  {ValidAfm}  ", "  62.01  ", "  Athens  ");

        b.Name.Should().Be("Acme");
        b.Afm.Should().Be(ValidAfm);
        b.Kad.Should().Be("62.01");
        b.Address.Should().Be("Athens");
    }

    [Fact]
    public void Create_normalizes_blank_kad_and_address_to_null()
    {
        var b = Business.Create("Acme", ValidAfm, "   ", "");

        b.Kad.Should().BeNull();
        b.Address.Should().BeNull();
    }

    [Fact]
    public void Deactivate_throws_when_already_inactive()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.Deactivate();

        var act = () => b.Deactivate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reactivate_throws_when_already_active()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);

        var act = () => b.Reactivate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeAfm_throws_when_invalid()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);

        var act = () => b.ChangeAfm("nope");

        act.Should().Throw<InvalidAfmException>();
        b.Afm.Should().Be(ValidAfm);
    }

    [Fact]
    public void ChangeAfm_updates_when_valid()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);

        b.ChangeAfm(AnotherValidAfm);

        b.Afm.Should().Be(AnotherValidAfm);
    }

    [Fact]
    public void Rename_throws_when_empty()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);

        var act = () => b.Rename("   ");

        act.Should().Throw<DomainException>();
        b.Name.Should().Be("Acme");
    }

    [Fact]
    public void RecordAadeSyncFailure_OnFirstFailure_SetsTimestamp()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);

        var before = DateTime.UtcNow;
        b.RecordAadeSyncFailure("InvalidCredentials");
        var after = DateTime.UtcNow;

        b.HasAadeFailure.Should().BeTrue();
        b.AadeLastFailureReason.Should().Be("InvalidCredentials");
        b.AadeLastFailureAt.Should().NotBeNull();
        b.AadeLastFailureAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        b.AadeFailureEmailSentAt.Should().BeNull();
    }

    [Fact]
    public void RecordAadeSyncFailure_OnRepeatFailure_PreservesOriginalTimestamp()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.RecordAadeSyncFailure("InvalidCredentials");
        var firstTimestamp = b.AadeLastFailureAt;

        // Force a measurable wall-clock gap.
        Thread.Sleep(10);
        b.RecordAadeSyncFailure("InvalidCredentials");

        b.AadeLastFailureAt.Should().Be(firstTimestamp);
    }

    [Fact]
    public void ClearAadeSyncFailure_ResetsAllFields()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.RecordAadeSyncFailure("InvalidCredentials");
        b.RecordAadeFailureEmailSent();

        b.ClearAadeSyncFailure();

        b.HasAadeFailure.Should().BeFalse();
        b.AadeLastFailureAt.Should().BeNull();
        b.AadeLastFailureReason.Should().BeNull();
        b.AadeFailureEmailSentAt.Should().BeNull();
    }

    [Fact]
    public void RecordAadeFailureEmailSent_ThrowsWhenNoActiveFailure()
    {
        var b = Business.Create("Acme", ValidAfm, null, null);

        var act = () => b.RecordAadeFailureEmailSent();

        act.Should().Throw<InvalidOperationException>();
    }
}
