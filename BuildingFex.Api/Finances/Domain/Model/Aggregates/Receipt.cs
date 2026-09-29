using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class Receipt : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string ResidentExternalId { get; private set; } = string.Empty;
    public string? IssueDate { get; private set; }
    public string? DueDate { get; private set; }
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = "Pending";
    public decimal LateFee { get; private set; }
    public decimal ExtraCharges { get; private set; }
    public string? Concept { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Receipt() { }

    public static Receipt Create(
        string externalId,
        int ownerAdminId,
        string residentExternalId,
        string? issueDate,
        string? dueDate,
        decimal amount,
        string status,
        decimal lateFee,
        decimal extraCharges,
        string? concept)
    {
        return new Receipt
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            ResidentExternalId = residentExternalId,
            IssueDate = issueDate,
            DueDate = dueDate,
            Amount = amount,
            Status = status,
            LateFee = lateFee,
            ExtraCharges = extraCharges,
            Concept = concept,
        };
    }

    public void Patch(string? status, decimal? lateFee, decimal? extraCharges, string? concept)
    {
        if (!string.IsNullOrWhiteSpace(status))
            Status = status;
        if (lateFee.HasValue)
            LateFee = lateFee.Value;
        if (extraCharges.HasValue)
            ExtraCharges = extraCharges.Value;
        if (concept is not null)
            Concept = concept;
    }

    public void MarkAsPaid()
    {
        Status = "Paid";
    }
}
