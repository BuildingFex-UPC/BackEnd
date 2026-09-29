using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class KpiRecord : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public int TotalResidents { get; private set; }
    public int OccupiedUnits { get; private set; }
    public int EmptyUnits { get; private set; }
    public decimal TotalDebt { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string? ResidentExternalId => null;

    private KpiRecord() { }

    public static KpiRecord Create(
        string externalId,
        int ownerAdminId,
        int totalResidents,
        int occupiedUnits,
        int emptyUnits,
        decimal totalDebt)
    {
        return new KpiRecord
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            TotalResidents = totalResidents,
            OccupiedUnits = occupiedUnits,
            EmptyUnits = emptyUnits,
            TotalDebt = totalDebt,
        };
    }
}
