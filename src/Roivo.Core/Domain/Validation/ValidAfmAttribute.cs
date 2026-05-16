using System.ComponentModel.DataAnnotations;

namespace Roivo.Core.Domain.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ValidAfmAttribute : ValidationAttribute
{
    public ValidAfmAttribute() : base("Μη έγκυρο ΑΦΜ.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true; // [Required] handles null/empty separately
        if (value is not string afm) return false;
        return AfmValidator.IsValid(afm);
    }
}
