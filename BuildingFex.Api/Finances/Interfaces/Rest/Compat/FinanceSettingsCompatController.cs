using BuildingFex.Api.Finances.Application.Internal;
using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Finances.Interfaces.Rest.Transform;
using BuildingFex.Api.Shared.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BuildingFex.Api.Finances.Interfaces.Rest.Compat;

[ApiController]
[Route("financeSettings")]
[Authorize]
public class FinanceSettingsCompatController(
    IFinanceSettingRepository settingRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var settings = await settingRepository.ListAsync(ownerAdminId, ct);
        return Ok(settings.Select(FinanceCompatSerializer.FinanceSettingToJson));
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
            ? idProp.GetString() ?? $"finance-settings-{owner.ExternalId}"
            : $"finance-settings-{owner.ExternalId}";

        var setting = FinanceSetting.Create(
            externalId,
            owner.Id,
            body.TryGetProperty("baseMonthlyExpense", out var baseProp) ? baseProp.GetDecimal() : 150,
            body.TryGetProperty("lateFeeRate", out var rateProp) ? rateProp.GetDecimal() : 0.05m);

        await settingRepository.AddAsync(setting, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = await settingRepository.FindByExternalIdAsync(externalId, ct);
        return StatusCode(StatusCodes.Status201Created, FinanceCompatSerializer.FinanceSettingToJson(created!));
    }

    [HttpPatch]
    public async Task<IActionResult> Update([FromQuery] string? ownerAdminId, [FromBody] JsonElement body, CancellationToken ct)
    {
        var owner = await ownerResolver.ResolveOwnerAdminAsync(ownerAdminId, ct);
        if (owner is null)
            return BadRequest(new { code = "OWNER_ADMIN_REQUIRED", message = "ownerAdminId es obligatorio." });

        decimal? baseMonthlyExpense = body.TryGetProperty("baseMonthlyExpense", out var baseProp)
            ? baseProp.GetDecimal()
            : null;
        decimal? lateFeeRate = body.TryGetProperty("lateFeeRate", out var rateProp)
            ? rateProp.GetDecimal()
            : null;

        var settings = await settingRepository.ListAsync(ownerAdminId, ct);
        var setting = settings.FirstOrDefault();
        var externalId = $"finance-settings-{owner.ExternalId}";

        if (setting is null)
        {
            setting = FinanceSetting.Create(
                externalId,
                owner.Id,
                baseMonthlyExpense ?? 150,
                lateFeeRate ?? 0.05m);
            await settingRepository.AddAsync(setting, ct);
        }
        else
        {
            setting.Patch(baseMonthlyExpense, lateFeeRate);
            settingRepository.Update(setting);
        }

        await unitOfWork.CompleteAsync(ct);

        var saved = await settingRepository.FindByExternalIdAsync(setting.ExternalId, ct);
        return Ok(FinanceCompatSerializer.FinanceSettingToJson(saved ?? setting));
    }
}
