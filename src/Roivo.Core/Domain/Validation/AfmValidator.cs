namespace Roivo.Core.Domain.Validation;

/// <summary>
/// Validates Greek tax identification numbers (ΑΦΜ).
/// Algorithm: 9 digits where the last is a checksum of the first 8.
/// Multiply each of the first 8 digits by descending powers of 2 (2^8 .. 2^1),
/// sum, mod 11, mod 10 -> expected check digit.
/// </summary>
public static class AfmValidator
{
    public static bool IsValid(string? afm)
    {
        if (string.IsNullOrWhiteSpace(afm)) return false;
        if (afm.Length != 9) return false;
        if (!afm.All(char.IsDigit)) return false;
        if (afm == "000000000") return false;

        int sum = 0;
        for (int i = 0; i < 8; i++)
        {
            int digit = afm[i] - '0';
            sum += digit * (1 << (8 - i));
        }

        int expectedCheckDigit = (sum % 11) % 10;
        int actualCheckDigit = afm[8] - '0';

        return expectedCheckDigit == actualCheckDigit;
    }
}
