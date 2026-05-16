using System.ComponentModel.DataAnnotations;

namespace Roivo.Infrastructure.Email;

public class SmtpSettings
{
    [Required]
    public required string Host { get; init; }

    [Range(1, 65535)]
    public required int Port { get; init; }

    [Required]
    public required string Username { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required, EmailAddress]
    public required string FromEmail { get; init; }

    [Required]
    public required string FromName { get; init; }

    public required bool UseStartTls { get; init; }
}
