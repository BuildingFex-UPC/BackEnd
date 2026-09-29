namespace BuildingFex.Api.Finances.Application.Internal.Dashboard;

public record RecentPaymentDto(
    string Id,
    string ResidentId,
    decimal Amount,
    string? PaidAt,
    string? Method,
    string? Reference);

public record AdminExpenseSummaryDto(
    string Name,
    decimal Amount,
    string PurchaseDate);

public record MonthlyChartPointDto(
    string MonthKey,
    decimal Income,
    decimal Expenses);

public record DashboardKpiResponse(
    decimal TotalCollectedThisMonth,
    decimal TotalPendingDebt,
    int TotalResidents,
    int OccupiedUnits,
    int EmptyUnits,
    decimal TotalDebt,
    IEnumerable<RecentPaymentDto> RecentPayments,
    decimal TotalAdminExpenses,
    IEnumerable<AdminExpenseSummaryDto> AdminExpenses,
    IReadOnlyList<MonthlyChartPointDto> MonthlyChart);
