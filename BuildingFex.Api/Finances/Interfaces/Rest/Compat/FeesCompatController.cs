using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Finances.Interfaces.Rest.Transform;
using BuildingFex.Api.Shared.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BuildingFex.Api.Finances.Interfaces.Rest.Compat;

[ApiController]
[Route("fees")]
[Authorize]
public class FeesCompatController(
    IFeeRepository feeRepository,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        [FromQuery] string? residentId,
        CancellationToken ct)
    {
        var fees = await feeRepository.ListAsync(ownerAdminId, residentId, ct);
        return Ok(fees.Select(FinanceCompatSerializer.FeeToJson));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromBody] JsonElement body, CancellationToken ct)
    {
        var fee = await feeRepository.FindByExternalIdAsync(id, ct);
        if (fee is null)
            return NotFound();

        if (body.TryGetProperty("status", out var statusElement))
            fee.UpdateStatus(statusElement.GetString() ?? fee.Status);

        feeRepository.Update(fee);
        await unitOfWork.CompleteAsync(ct);

        var updated = await feeRepository.FindByExternalIdAsync(id, ct);
        return Ok(FinanceCompatSerializer.FeeToJson(updated!));
    }
}
