using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class SharedUtilityService : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Month { get; private set; }
    public int? ResidentCount { get; private set; }
    public decimal? PerResidentShare { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string? ResidentExternalId => null;

    private SharedUtilityService() { }

    public static SharedUtilityService Create(
        string externalId,
        int ownerAdminId,
        string type,
        decimal amount,
        string? month,
        int? residentCount,
        decimal? perResidentShare)
    {
        return new SharedUtilityService
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            Type = type,
            Amount = amount,
            Month = month,
            ResidentCount = residentCount,
            PerResidentShare = perResidentShare,
        };
    }
}
