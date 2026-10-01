namespace Roivo.Application.Features.Banking.Commands.CompleteBankingConnection;

/// <summary>
/// Finishes a bank connection using the authorization code the bank returned on
/// the post-consent redirect.
/// </summary>
public sealed record CompleteBankingConnectionCommand(Guid BusinessId, string AuthorizationCode);
