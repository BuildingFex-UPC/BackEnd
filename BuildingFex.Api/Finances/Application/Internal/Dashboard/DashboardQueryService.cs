using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Finances.Application.Internal.Dashboard;

public class DashboardQueryService(AppDbContext dbContext) : IDashboardQueryService
{
    public async Task<DashboardKpiResponse> GetKpisAsync(int ownerAdminId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthStartIso = monthStart.ToString("yyyy-MM-dd");

        var totalCollected = await dbContext.Set<Payment>()
            .Where(p => p.OwnerAdminId == ownerAdminId)
            .Where(p => p.PaidAt != null && p.PaidAt.CompareTo(monthStartIso) >= 0)
            .SumAsync(p => p.Amount, ct);

        var totalPendingDebt = await dbContext.Set<Receipt>()
            .Where(r => r.OwnerAdminId == ownerAdminId && r.Status != "Paid")
            .SumAsync(r => r.Amount + r.LateFee + r.ExtraCharges, ct);

        var residentsQuery = dbContext.Set<User>()
            .Where(u => u.Role == "resident" && u.OwnerAdminId == ownerAdminId);

        var totalResidents = await residentsQuery.CountAsync(ct);
        var occupiedUnits = await residentsQuery
            .Where(u => u.Code != null && u.Code != string.Empty)
            .CountAsync(ct);

        var kpiRecord = await dbContext.Set<KpiRecord>()
            .Where(k => k.OwnerAdminId == ownerAdminId)
            .OrderByDescending(k => k.Id)
            .FirstOrDefaultAsync(ct);

        var baselineTotalUnits = (kpiRecord?.OccupiedUnits ?? 0) + (kpiRecord?.EmptyUnits ?? 0);
        var emptyUnits = baselineTotalUnits > occupiedUnits
            ? baselineTotalUnits - occupiedUnits
            : 0;

        var totalDebt = totalPendingDebt;

        var recentPayments = await dbContext.Set<Payment>()
            .Where(p => p.OwnerAdminId == ownerAdminId)
            .OrderByDescending(p => p.Id)
            .Take(10)
            .Select(p => new RecentPaymentDto(
                p.ExternalId,
                p.ResidentExternalId,
                p.Amount,
                p.PaidAt,
                p.Method,
                p.Reference))
            .ToListAsync(ct);

        var adminExpenses = await dbContext.Set<AdminManagementExpense>()
            .Where(e => e.OwnerAdminId == ownerAdminId)
            .OrderByDescending(e => e.PurchaseDate)
            .Take(20)
            .Select(e => new AdminExpenseSummaryDto(e.Name, e.Amount, e.PurchaseDate))
            .ToListAsync(ct);

        var totalAdminExpenses = adminExpenses.Sum(e => e.Amount);

        var monthlyChart = await BuildMonthlyChartAsync(ownerAdminId, ct);

        return new DashboardKpiResponse(
            TotalCollectedThisMonth: totalCollected,
            TotalPendingDebt: totalPendingDebt,
            TotalResidents: totalResidents,
            OccupiedUnits: occupiedUnits,
            EmptyUnits: emptyUnits,
            TotalDebt: totalDebt,
            RecentPayments: recentPayments,
            TotalAdminExpenses: totalAdminExpenses,
            AdminExpenses: adminExpenses,
            MonthlyChart: monthlyChart);
    }

    private static string MonthKeyFromIso(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 7)
            return string.Empty;

        return value[..7];
    }

    private async Task<IReadOnlyList<MonthlyChartPointDto>> BuildMonthlyChartAsync(
        int ownerAdminId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var monthKeys = Enumerable.Range(0, 6)
            .Select(i => new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(i - 5))
            .Select(d => d.ToString("yyyy-MM"))
            .ToList();

        var payments = await dbContext.Set<Payment>()
            .Where(p => p.OwnerAdminId == ownerAdminId && p.PaidAt != null)
            .ToListAsync(ct);

        var paidReceipts = await dbContext.Set<Receipt>()
            .Where(r => r.OwnerAdminId == ownerAdminId && r.Status == "Paid")
            .ToListAsync(ct);

        var expenses = await dbContext.Set<AdminManagementExpense>()
            .Where(e => e.OwnerAdminId == ownerAdminId)
            .ToListAsync(ct);

        return monthKeys
            .Select(monthKey =>
            {
                var incomeFromPayments = payments
                    .Where(p => MonthKeyFromIso(p.PaidAt) == monthKey)
                    .Sum(p => p.Amount);

                var incomeFromReceipts = paidReceipts
                    .Where(r => MonthKeyFromIso(r.IssueDate) == monthKey)
                    .Sum(r => r.Amount + r.LateFee + r.ExtraCharges);

                var income = incomeFromPayments > 0 ? incomeFromPayments : incomeFromReceipts;

                var expenseTotal = expenses
                    .Where(e => MonthKeyFromIso(e.PurchaseDate) == monthKey)
                    .Sum(e => e.Amount);

                return new MonthlyChartPointDto(monthKey, income, expenseTotal);
            })
            .ToList();
    }
}
