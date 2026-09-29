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
[Route("adminManagementExpenses")]
[Authorize]
public class AdminManagementExpensesCompatController(
    IAdminManagementExpenseRepository expenseRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var rows = await expenseRepository.ListAsync(ownerAdminId, ct);
        return Ok(rows.Select(FinanceCompatSerializer.AdminExpenseToJson));
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
            ? idProp.GetString() ?? $"admin-exp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : $"admin-exp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var expense = AdminManagementExpense.Create(
            externalId,
            owner.Id,
            body.GetProperty("name").GetString() ?? string.Empty,
            body.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
            body.GetProperty("purchaseDate").GetString() ?? string.Empty,
            body.TryGetProperty("invoicePhotoUrl", out var photoProp) ? photoProp.GetString() ?? string.Empty : string.Empty);

        await expenseRepository.AddAsync(expense, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = (await expenseRepository.ListAsync(owner.ExternalId, ct))
            .FirstOrDefault(e => e.ExternalId == externalId);

        return StatusCode(StatusCodes.Status201Created, FinanceCompatSerializer.AdminExpenseToJson(created!));
    }
}
