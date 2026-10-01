using Roivo.Application.Abstractions.Banking;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeBankingConnectionOptions : IBankingConnectionOptions
{
    public string Country { get; set; } = "GR";
    public string RedirectUrl { get; set; } = "https://localhost/banking/callback";
    public int ConsentValidDays { get; set; } = 90;
    public int SyncLookbackDays { get; set; } = 90;
}
