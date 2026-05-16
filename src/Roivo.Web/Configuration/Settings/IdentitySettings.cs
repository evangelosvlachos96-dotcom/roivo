using System.ComponentModel.DataAnnotations;

namespace Roivo.Web.Configuration.Settings;

public class IdentitySettings
{
    [Required, ValidateObjectMembers]
    public required IdentityPasswordSettings Password { get; init; }

    [Required, ValidateObjectMembers]
    public required IdentityLockoutSettings Lockout { get; init; }

    [Required, ValidateObjectMembers]
    public required IdentitySignInSettings SignIn { get; init; }
}

public class IdentityPasswordSettings
{
    [Range(1, 1024)]
    public required int RequiredLength { get; init; }
    public required bool RequireDigit { get; init; }
    public required bool RequireUppercase { get; init; }
    public required bool RequireLowercase { get; init; }
    public required bool RequireNonAlphanumeric { get; init; }
}

public class IdentityLockoutSettings
{
    [Range(1, 100)]
    public required int MaxFailedAccessAttempts { get; init; }

    [Range(1, 10080)]
    public required int DefaultLockoutMinutes { get; init; }
}

public class IdentitySignInSettings
{
    public required bool RequireConfirmedEmail { get; init; }
}

internal sealed class ValidateObjectMembersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success;
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(value);
        if (Validator.TryValidateObject(value, ctx, results, validateAllProperties: true))
        {
            return ValidationResult.Success;
        }
        var msg = string.Join("; ", results.Select(r => r.ErrorMessage));
        return new ValidationResult($"{context.MemberName}: {msg}");
    }
}
