using System.ComponentModel.DataAnnotations;

namespace Roivo.Web.Configuration.Settings;

public class OpenIddictSettings
{
    [Range(1, int.MaxValue)]
    public required int AccessTokenLifetimeMinutes { get; init; }

    [Range(1, int.MaxValue)]
    public required int RefreshTokenLifetimeDays { get; init; }

    [Required]
    public required string AuthorizationEndpoint { get; init; }

    [Required]
    public required string TokenEndpoint { get; init; }

    [Required]
    public required string UserInfoEndpoint { get; init; }
}
