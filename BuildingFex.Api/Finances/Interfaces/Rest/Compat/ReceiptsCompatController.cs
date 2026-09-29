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
[Route("receipts")]
[Authorize]
public class ReceiptsCompatController(
    IReceiptRepository receiptRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        [FromQuery] string? residentId,
        CancellationToken ct)
    {
        var receipts = await receiptRepository.ListAsync(ownerAdminId, residentId, ct);
        return Ok(receipts.Select(FinanceCompatSerializer.ReceiptToJson));
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

        var externalId = body.TryGetProperty("id", out var idProp)
            ? FinanceCompatSerializer.NormalizeExternalId(idProp)
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        var receipt = Receipt.Create(
            externalId,
            owner.Id,
            body.GetProperty("residentId").GetString() ?? string.Empty,
            body.TryGetProperty("issueDate", out var issueProp) ? issueProp.GetString() : null,
            body.TryGetProperty("dueDate", out var dueProp) ? dueProp.GetString() : null,
            body.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
            body.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? "Pending" : "Pending",
            body.TryGetProperty("lateFee", out var lateProp) ? lateProp.GetDecimal() : 0,
            body.TryGetProperty("extraCharges", out var extraProp) ? extraProp.GetDecimal() : 0,
            body.TryGetProperty("concept", out var conceptProp) ? conceptProp.GetString() : null);

        await receiptRepository.AddAsync(receipt, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = await receiptRepository.FindByExternalIdAsync(externalId, ct);
        return StatusCode(StatusCodes.Status201Created, FinanceCompatSerializer.ReceiptToJson(created!));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromBody] JsonElement body, CancellationToken ct)
    {
        var receipt = await receiptRepository.FindByExternalIdAsync(id, ct);
        if (receipt is null)
            return NotFound();

        string? status = body.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
        decimal? lateFee = body.TryGetProperty("lateFee", out var lateProp)
            ? lateProp.GetDecimal()
            : null;
        decimal? extraCharges = body.TryGetProperty("extraCharges", out var extraProp)
            ? extraProp.GetDecimal()
            : null;
        string? concept = body.TryGetProperty("concept", out var conceptProp) ? conceptProp.GetString() : null;

        receipt.Patch(status, lateFee, extraCharges, concept);
        receiptRepository.Update(receipt);
        await unitOfWork.CompleteAsync(ct);

        var updated = await receiptRepository.FindByExternalIdAsync(id, ct);
        return Ok(FinanceCompatSerializer.ReceiptToJson(updated!));
    }
}
