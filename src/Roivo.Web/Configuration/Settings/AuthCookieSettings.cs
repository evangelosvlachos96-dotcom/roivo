using System.ComponentModel.DataAnnotations;

namespace Roivo.Web.Configuration.Settings;

public class AuthCookieSettings
{
    [Required]
    public required string Name { get; init; }

    [Range(1, 3650)]
    public required int ExpirationDays { get; init; }

    [Required]
    public required string LoginPath { get; init; }

    [Required]
    public required string LogoutPath { get; init; }

    [Required]
    public required string AccessDeniedPath { get; init; }
}
