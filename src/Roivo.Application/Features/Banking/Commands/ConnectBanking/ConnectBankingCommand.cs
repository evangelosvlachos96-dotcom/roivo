namespace Roivo.Application.Features.Banking.Commands.ConnectBanking;

/// <summary>
/// Starts a bank connection. <paramref name="BankName"/> is the aggregator's own
/// name for the bank, as returned by
/// <c>ListBankingProvidersQuery</c>.
/// </summary>
public sealed record ConnectBankingCommand(Guid BusinessId, string BankName);
