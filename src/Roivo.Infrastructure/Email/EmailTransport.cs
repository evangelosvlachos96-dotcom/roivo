namespace Roivo.Infrastructure.Email;

/// <summary>How outbound mail leaves the process.</summary>
public enum EmailTransport
{
    /// <summary>Resend's HTTPS API. The only transport that works on Render.</summary>
    Resend = 1,

    /// <summary>Direct SMTP. For local development against a sandbox mail server.</summary>
    Smtp = 2,
}
