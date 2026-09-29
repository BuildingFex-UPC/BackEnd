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
[Route("sharedUtilityServices")]
[Authorize]
public class SharedUtilityServicesCompatController(
    ISharedUtilityServiceRepository serviceRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var rows = await serviceRepository.ListAsync(ownerAdminId, ct);
        return Ok(rows.Select(FinanceCompatSerializer.SharedServiceToJson));
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
            ? idProp.GetString() ?? $"shared-svc-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : $"shared-svc-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        int? residentCount = body.TryGetProperty("residentCount", out var countProp)
            ? countProp.GetInt32()
            : null;
        decimal? perShare = body.TryGetProperty("perResidentShare", out var shareProp)
            ? shareProp.GetDecimal()
            : null;

        var service = SharedUtilityService.Create(
            externalId,
            owner.Id,
            body.GetProperty("type").GetString() ?? string.Empty,
            body.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
            body.TryGetProperty("month", out var monthProp) ? monthProp.GetString() : null,
            residentCount,
            perShare);

        await serviceRepository.AddAsync(service, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = (await serviceRepository.ListAsync(owner.ExternalId, ct))
            .FirstOrDefault(s => s.ExternalId == externalId);

        return StatusCode(StatusCodes.Status201Created, FinanceCompatSerializer.SharedServiceToJson(created!));
    }
}
