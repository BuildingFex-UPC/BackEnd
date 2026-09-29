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
[Route("fixedPayoutRecipients")]
[Authorize]
public class FixedPayoutRecipientsCompatController(
    IFixedPayoutRecipientRepository recipientRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var rows = await recipientRepository.ListAsync(ownerAdminId, ct);
        return Ok(rows.Select(FinanceCompatSerializer.FixedPayoutToJson));
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
            ? idProp.GetString() ?? $"fp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : $"fp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var paymentHistoryJson = body.TryGetProperty("paymentHistory", out var histProp)
            ? histProp.GetRawText()
            : "[]";

        var recipient = FixedPayoutRecipient.Create(
            externalId,
            owner.Id,
            body.GetProperty("name").GetString() ?? string.Empty,
            body.GetProperty("dni").GetString() ?? string.Empty,
            body.GetProperty("phone").GetString() ?? string.Empty,
            body.TryGetProperty("salary", out var salaryProp) ? salaryProp.GetDecimal() : 0,
            body.TryGetProperty("intervalDays", out var intervalProp) ? intervalProp.GetInt32() : 30,
            body.GetProperty("nextPaymentDate").GetString() ?? string.Empty,
            body.TryGetProperty("photoUrl", out var photoProp) ? photoProp.GetString() ?? string.Empty : string.Empty,
            paymentHistoryJson,
            body.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetString() : null);

        await recipientRepository.AddAsync(recipient, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = await recipientRepository.FindByExternalIdAsync(externalId, ct);
        return StatusCode(StatusCodes.Status201Created, FinanceCompatSerializer.FixedPayoutToJson(created!));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] JsonElement body, CancellationToken ct)
    {
        var recipient = await recipientRepository.FindByExternalIdAsync(id, ct);
        if (recipient is null)
            return NotFound();

        var paymentHistoryJson = body.TryGetProperty("paymentHistory", out var histProp)
            ? histProp.GetRawText()
            : recipient.PaymentHistoryJson;

        recipient.ReplaceFrom(
            body.GetProperty("name").GetString() ?? recipient.Name,
            body.GetProperty("dni").GetString() ?? recipient.Dni,
            body.GetProperty("phone").GetString() ?? recipient.Phone,
            body.TryGetProperty("salary", out var salaryProp) ? salaryProp.GetDecimal() : recipient.Salary,
            body.TryGetProperty("intervalDays", out var intervalProp) ? intervalProp.GetInt32() : recipient.IntervalDays,
            body.GetProperty("nextPaymentDate").GetString() ?? recipient.NextPaymentDate,
            body.TryGetProperty("photoUrl", out var photoProp) ? photoProp.GetString() ?? string.Empty : recipient.PhotoUrl,
            paymentHistoryJson,
            body.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetString() : recipient.CreatedAtIso);

        recipientRepository.Update(recipient);
        await unitOfWork.CompleteAsync(ct);

        var updated = await recipientRepository.FindByExternalIdAsync(id, ct);
        return Ok(FinanceCompatSerializer.FixedPayoutToJson(updated!));
    }
}
