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
[Route("payments")]
[Authorize]
public class PaymentsCompatController(
    IPaymentRepository paymentRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        [FromQuery] string? residentId,
        CancellationToken ct)
    {
        var payments = await paymentRepository.ListAsync(ownerAdminId, residentId, ct);
        return Ok(payments.Select(FinanceCompatSerializer.PaymentToJson));
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
            ? FinanceCompatSerializer.NormalizeExternalId(idProp, "MP-")
            : $"MP-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var feeId = body.TryGetProperty("feeId", out var feeProp)
            ? FinanceCompatSerializer.NormalizeOptionalExternalId(feeProp)
            : null;

        var payment = Payment.Create(
            externalId,
            owner.Id,
            body.GetProperty("residentId").GetString() ?? string.Empty,
            body.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
            feeId,
            body.TryGetProperty("feeMonth", out var monthProp) ? monthProp.GetString() : null,
            body.TryGetProperty("paidAt", out var paidProp) ? paidProp.GetString() : null,
            body.TryGetProperty("method", out var methodProp) ? methodProp.GetString() : null,
            body.TryGetProperty("reference", out var refProp) ? refProp.GetString() : null);

        await paymentRepository.AddAsync(payment, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = await paymentRepository.FindByExternalIdAsync(externalId, ct);
        return StatusCode(StatusCodes.Status201Created, FinanceCompatSerializer.PaymentToJson(created!));
    }
}
