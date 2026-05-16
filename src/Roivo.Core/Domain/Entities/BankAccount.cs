using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

public class BankAccount : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }
    public required string BankName { get; set; }
    public required string Iban { get; set; }
    public Currency Currency { get; set; } = Currency.EUR;
    public decimal CurrentBalance { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? Psd2AccessTokenEncrypted { get; set; }
    public DateTime? Psd2TokenExpiresAt { get; set; }
}
