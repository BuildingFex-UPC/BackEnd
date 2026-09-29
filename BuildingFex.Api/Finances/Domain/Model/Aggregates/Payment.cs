using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class Payment : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string ResidentExternalId { get; private set; } = string.Empty;
    public string? FeeExternalId { get; private set; }
    public string? FeeMonth { get; private set; }
    public decimal Amount { get; private set; }
    public string? PaidAt { get; private set; }
    public string? Method { get; private set; }
    public string? Reference { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Payment() { }

    public static Payment Create(
        string externalId,
        int ownerAdminId,
        string residentExternalId,
        decimal amount,
        string? feeExternalId,
        string? feeMonth,
        string? paidAt,
        string? method,
        string? reference)
    {
        return new Payment
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            ResidentExternalId = residentExternalId,
            Amount = amount,
            FeeExternalId = feeExternalId,
            FeeMonth = feeMonth,
            PaidAt = paidAt,
            Method = method,
            Reference = reference,
        };
    }
}
