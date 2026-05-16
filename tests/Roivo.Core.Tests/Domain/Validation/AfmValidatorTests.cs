using FluentAssertions;
using Roivo.Core.Domain.Validation;

namespace Roivo.Core.Tests.Domain.Validation;

public class AfmValidatorTests
{
    [Theory]
    [InlineData("09401425")]
    [InlineData("12345678")]
    [InlineData("99999999")]
    [InlineData("00000001")]
    [InlineData("87654321")]
    public void IsValid_returns_true_for_computed_valid_afm(string first8)
    {
        var validAfm = ComputeValidAfm(first8);

        AfmValidator.IsValid(validAfm).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]      // too short
    [InlineData("1234567890")] // too long
    [InlineData("12345678a")]  // non-digit
    [InlineData("000000000")]  // all zeros — explicitly rejected
    public void IsValid_returns_false_for_invalid_inputs(string? afm)
    {
        AfmValidator.IsValid(afm).Should().BeFalse();
    }

    [Fact]
    public void IsValid_rejects_random_string_of_nine_digits_with_wrong_checksum()
    {
        // Construct an AFM with a deliberately-wrong checksum digit
        var first8 = "12345678";
        var validAfm = ComputeValidAfm(first8);
        var wrongCheck = (validAfm[8] - '0' + 1) % 10;
        var bad = first8 + wrongCheck.ToString();

        AfmValidator.IsValid(bad).Should().BeFalse();
    }

    [Fact]
    public void IsValid_catches_single_digit_typo()
    {
        var validAfm = ComputeValidAfm("12345678");
        // Flip one of the leading digits — the precomputed checksum no longer matches.
        var typo = '9' + validAfm.Substring(1);

        AfmValidator.IsValid(typo).Should().BeFalse();
    }

    private static string ComputeValidAfm(string first8Digits)
    {
        if (first8Digits.Length != 8 || !first8Digits.All(char.IsDigit))
            throw new ArgumentException("Need exactly 8 digits");

        int sum = 0;
        for (int i = 0; i < 8; i++)
            sum += (first8Digits[i] - '0') * (1 << (8 - i));

        int checkDigit = (sum % 11) % 10;
        return first8Digits + checkDigit.ToString();
    }
}
