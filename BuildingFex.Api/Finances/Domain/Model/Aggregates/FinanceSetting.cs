using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Finances.Domain.Model.Aggregates;

public class FinanceSetting : IAuditableEntity, IOwnerScopedFinanceEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public decimal BaseMonthlyExpense { get; private set; }
    public decimal LateFeeRate { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string? ResidentExternalId => null;

    private FinanceSetting() { }

    public static FinanceSetting Create(
        string externalId,
        int ownerAdminId,
        decimal baseMonthlyExpense,
        decimal lateFeeRate)
    {
        return new FinanceSetting
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            BaseMonthlyExpense = baseMonthlyExpense,
            LateFeeRate = lateFeeRate,
        };
    }

    public void Patch(decimal? baseMonthlyExpense, decimal? lateFeeRate)
    {
        if (baseMonthlyExpense.HasValue)
            BaseMonthlyExpense = baseMonthlyExpense.Value;
        if (lateFeeRate.HasValue)
            LateFeeRate = lateFeeRate.Value;
    }
}
