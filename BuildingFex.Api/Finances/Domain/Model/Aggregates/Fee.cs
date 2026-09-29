using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class Fee : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string ResidentExternalId { get; private set; } = string.Empty;
    public string Month { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string DueDate { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Fee() { }

    public static Fee Create(
        string externalId,
        int ownerAdminId,
        string residentExternalId,
        string month,
        decimal amount,
        string dueDate,
        string status)
    {
        return new Fee
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            ResidentExternalId = residentExternalId,
            Month = month,
            Amount = amount,
            DueDate = dueDate,
            Status = status,
        };
    }

    public void UpdateStatus(string status) => Status = status;
}
