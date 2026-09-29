using BuildingFex.Api.Finances.Application.Internal;
using BuildingFex.Api.Finances.Application.Internal.Dashboard;
using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Finances.Interfaces.Rest.Transform;
using BuildingFex.Api.Shared.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BuildingFex.Api.Finances.Interfaces.Rest.Compat;

[ApiController]
[Route("kpi")]
[Authorize]
public class KpiCompatController(
    IKpiRepository kpiRepository,
    IDashboardQueryService dashboardQueryService,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var owner = await ownerResolver.ResolveOwnerAdminAsync(ownerAdminId, ct);
        if (owner is null)
            return BadRequest(new { code = "OWNER_ADMIN_REQUIRED", message = "ownerAdminId es obligatorio." });

        var kpis = await dashboardQueryService.GetKpisAsync(owner.Id, ct);

        return Ok(new[]
        {
            new
            {
                id = $"kpi-{owner.ExternalId}",
                ownerAdminId = owner.ExternalId,
                totalResidents = kpis.TotalResidents,
                occupiedUnits = kpis.OccupiedUnits,
                emptyUnits = kpis.EmptyUnits,
                totalDebt = kpis.TotalDebt,
                totalCollectedThisMonth = kpis.TotalCollectedThisMonth,
                totalPendingDebt = kpis.TotalPendingDebt,
                monthlyChart = kpis.MonthlyChart.Select(m => new
                {
                    monthKey = m.MonthKey,
                    income = m.Income,
                    expenses = m.Expenses,
                }),
            },
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement body, CancellationToken ct)
    {
        var ownerExternalId = body.TryGetProperty("ownerAdminId", out var ownerProp)
            ? ownerProp.GetString()
            : null;

        var owner = await ownerResolver.ResolveOwnerAdminAsync(ownerExternalId, ct);
        if (owner is null)
            return BadRequest(new { code = "OWNER_ADMIN_REQUIRED", message = "ownerAdminId es obligatorio." });

        var externalId = body.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString() ?? $"kpi-{owner.ExternalId}"
            : $"kpi-{owner.ExternalId}";

        var kpi = KpiRecord.Create(
            externalId,
            owner.Id,
            body.TryGetProperty("totalResidents", out var totalProp) ? totalProp.GetInt32() : 0,
            body.TryGetProperty("occupiedUnits", out var occupiedProp) ? occupiedProp.GetInt32() : 0,
            body.TryGetProperty("emptyUnits", out var emptyProp) ? emptyProp.GetInt32() : 0,
            body.TryGetProperty("totalDebt", out var debtProp) ? debtProp.GetDecimal() : 0);

        await kpiRepository.AddAsync(kpi, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = await kpiRepository.FindByExternalIdAsync(externalId, ct);
        return StatusCode(StatusCodes.Status201Created, FinanceCompatSerializer.KpiToJson(created!));
    }
}
