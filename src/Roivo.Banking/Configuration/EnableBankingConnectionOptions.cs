using Microsoft.Extensions.Options;
using Roivo.Application.Abstractions.Banking;

namespace Roivo.Banking.Configuration;

/// <summary>
/// Exposes the slice of <see cref="EnableBankingSettings"/> that Application
/// handlers need, so they don't take a dependency on this project.
/// </summary>
public sealed class EnableBankingConnectionOptions : IBankingConnectionOptions
{
    private readonly EnableBankingSettings _settings;

    public EnableBankingConnectionOptions(IOptions<EnableBankingSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
    }

    public string Country => _settings.Country;
    public string RedirectUrl => _settings.RedirectUrl;
    public int ConsentValidDays => _settings.ConsentValidDays;
    public int SyncLookbackDays => _settings.SyncLookbackDays;
}
