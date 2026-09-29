using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class AdminManagementExpense : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string PurchaseDate { get; private set; } = string.Empty;
    public string InvoicePhotoUrl { get; private set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string? ResidentExternalId => null;

    private AdminManagementExpense() { }

    public static AdminManagementExpense Create(
        string externalId,
        int ownerAdminId,
        string name,
        decimal amount,
        string purchaseDate,
        string invoicePhotoUrl)
    {
        return new AdminManagementExpense
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            Name = name,
            Amount = amount,
            PurchaseDate = purchaseDate,
            InvoicePhotoUrl = invoicePhotoUrl,
        };
    }
}
