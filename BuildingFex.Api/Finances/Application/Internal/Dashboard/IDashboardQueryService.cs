namespace BuildingFex.Api.Finances.Application.Internal.Dashboard;

public interface IDashboardQueryService
{
    Task<DashboardKpiResponse> GetKpisAsync(int ownerAdminId, CancellationToken ct = default);
}
