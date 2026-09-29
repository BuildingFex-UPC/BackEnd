using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class FixedPayoutRecipient : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Dni { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public decimal Salary { get; private set; }
    public int IntervalDays { get; private set; }
    public string NextPaymentDate { get; private set; } = string.Empty;
    public string PhotoUrl { get; private set; } = string.Empty;
    public string PaymentHistoryJson { get; private set; } = "[]";
    public string? CreatedAtIso { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string? ResidentExternalId => null;

    private FixedPayoutRecipient() { }

    public static FixedPayoutRecipient Create(
        string externalId,
        int ownerAdminId,
        string name,
        string dni,
        string phone,
        decimal salary,
        int intervalDays,
        string nextPaymentDate,
        string photoUrl,
        string paymentHistoryJson,
        string? createdAtIso)
    {
        return new FixedPayoutRecipient
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            Name = name,
            Dni = dni,
            Phone = phone,
            Salary = salary,
            IntervalDays = intervalDays,
            NextPaymentDate = nextPaymentDate,
            PhotoUrl = photoUrl,
            PaymentHistoryJson = paymentHistoryJson,
            CreatedAtIso = createdAtIso,
        };
    }

    public void ReplaceFrom(
        string name,
        string dni,
        string phone,
        decimal salary,
        int intervalDays,
        string nextPaymentDate,
        string photoUrl,
        string paymentHistoryJson,
        string? createdAtIso)
    {
        Name = name;
        Dni = dni;
        Phone = phone;
        Salary = salary;
        IntervalDays = intervalDays;
        NextPaymentDate = nextPaymentDate;
        PhotoUrl = photoUrl;
        PaymentHistoryJson = paymentHistoryJson;
        CreatedAtIso = createdAtIso;
    }
}
