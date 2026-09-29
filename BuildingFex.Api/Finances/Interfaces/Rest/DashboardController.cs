using BuildingFex.Api.Finances.Application.Internal;
using BuildingFex.Api.Finances.Application.Internal.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFex.Api.Finances.Interfaces.Rest;

[ApiController]
[Route("api/v1/finances")]
[Tags("Dashboard")]
[Authorize]
public class DashboardController(
    IDashboardQueryService dashboardQueryService,
    FinanceOwnerResolver ownerResolver) : ControllerBase
{
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpis(
        [FromQuery] string? ownerAdminId,
        CancellationToken ct)
    {
        var owner = await ownerResolver.ResolveOwnerAdminAsync(ownerAdminId, ct);
        if (owner is null)
            return BadRequest(new { code = "OWNER_ADMIN_REQUIRED", message = "ownerAdminId es obligatorio." });

        var kpis = await dashboardQueryService.GetKpisAsync(owner.Id, ct);

        return Ok(new
        {
            totalCollectedThisMonth = kpis.TotalCollectedThisMonth,
            totalPendingDebt = kpis.TotalPendingDebt,
            totalResidents = kpis.TotalResidents,
            occupiedUnits = kpis.OccupiedUnits,
            emptyUnits = kpis.EmptyUnits,
            totalDebt = kpis.TotalDebt,
            recentPayments = kpis.RecentPayments.Select(p => new
            {
                id = p.Id,
                residentId = p.ResidentId,
                amount = p.Amount,
                paidAt = p.PaidAt,
                method = p.Method,
                reference = p.Reference,
            }),
            totalAdminExpenses = kpis.TotalAdminExpenses,
            adminExpenses = kpis.AdminExpenses.Select(e => new
            {
                name = e.Name,
                amount = e.Amount,
                purchaseDate = e.PurchaseDate,
            }),
            monthlyChart = kpis.MonthlyChart.Select(m => new
            {
                monthKey = m.MonthKey,
                income = m.Income,
                expenses = m.Expenses,
            }),
        });
    }
}
