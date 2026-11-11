using System.Text.Json;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class MaintenanceCheckoutOrder : IAuditableEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string PreferenceId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public string ResidentExternalId { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string ReceiptIdsJson { get; private set; } = "[]";
    public string FeeIdsJson { get; private set; } = "[]";
    public string? MpPaymentId { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private MaintenanceCheckoutOrder() { }

    public static MaintenanceCheckoutOrder Create(
        string externalId,
        int ownerAdminId,
        string residentExternalId,
        decimal totalAmount,
        IEnumerable<string> receiptExternalIds,
        IEnumerable<string> feeExternalIds)
    {
        return new MaintenanceCheckoutOrder
        {
            ExternalId = externalId.Trim(),
            OwnerAdminId = ownerAdminId,
            ResidentExternalId = residentExternalId.Trim(),
            TotalAmount = totalAmount,
            Status = "Pending",
            ReceiptIdsJson = JsonSerializer.Serialize(receiptExternalIds),
            FeeIdsJson = JsonSerializer.Serialize(feeExternalIds),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetPreferenceId(string preferenceId)
    {
        PreferenceId = preferenceId.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsApproved(string mpPaymentId)
    {
        Status = "Approved";
        MpPaymentId = mpPaymentId.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsCancelled()
    {
        Status = "Cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<string> GetReceiptExternalIds()
    {
        if (string.IsNullOrWhiteSpace(ReceiptIdsJson)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(ReceiptIdsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public IReadOnlyList<string> GetFeeExternalIds()
    {
        if (string.IsNullOrWhiteSpace(FeeIdsJson)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(FeeIdsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

