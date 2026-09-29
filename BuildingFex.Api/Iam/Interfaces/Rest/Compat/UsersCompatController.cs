using BuildingFex.Api.Iam.Application.CommandServices;
using BuildingFex.Api.Iam.Application.QueryServices;
using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Iam.Domain.Model.Commands;
using BuildingFex.Api.Iam.Interfaces.Rest.Resources;
using BuildingFex.Api.Iam.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFex.Api.Iam.Interfaces.Rest.Compat;

/// <summary>
/// Rutas compatibles con json-server para que el frontend (Fronted-1) funcione sin cambios grandes.
/// </summary>
[ApiController]
[Route("users")]
[Authorize]
public class UsersCompatController(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        [FromQuery] string? role,
        [FromQuery] string? email,
        [FromQuery] string? code,
        CancellationToken ct)
    {
        var users = await userQueryService.SearchAsync(email, role, code, ownerAdminId, ct);
        return Ok(users.Select(UserResourceAssembler.ToResource));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var user = await userQueryService.GetByExternalIdAsync(id, ct);
        return user is null ? NotFound() : Ok(UserResourceAssembler.ToResource(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCompatResource resource, CancellationToken ct)
    {
        if (string.Equals(resource.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            var result = await userCommandService.Handle(
                new RegisterAdminCommand(
                    resource.Name ?? string.Empty,
                    resource.Email ?? string.Empty,
                    resource.Password ?? string.Empty,
                    resource.Dni,
                    resource.Address,
                    resource.Company,
                    resource.Ruc),
                ct);

            if (result.IsFailure)
                return BadRequest(new { code = MapErrorCode(result.Error), message = result.Message });

            return StatusCode(StatusCodes.Status201Created, new
            {
                user = UserResourceAssembler.ToResource(result.Value.user),
                token = result.Value.token,
            });
        }

        if (!string.Equals(resource.Role, "resident", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { code = "UNSUPPORTED_ROLE", message = "Role must be 'resident' or 'admin'." });

        var createResult = await userCommandService.Handle(
            new CreateResidentCommand(
                resource.Id ?? string.Empty,
                resource.Name ?? string.Empty,
                resource.Floor ?? string.Empty,
                resource.Code ?? string.Empty,
                resource.OwnerAdminId ?? string.Empty,
                resource.AdmissionDate),
            ct);

        if (createResult.IsFailure)
            return BadRequest(new { code = MapErrorCode(createResult.Error), message = createResult.Message });

        return StatusCode(StatusCodes.Status201Created, UserResourceAssembler.ToResource(createResult.Value!));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(
        string id,
        [FromBody] UpdateResidentCredentialsResource resource,
        CancellationToken ct)
    {
        var result = await userCommandService.Handle(
            new UpdateResidentCredentialsCommand(
                id,
                resource.Email ?? string.Empty,
                resource.Password ?? string.Empty),
            ct);

        if (result.IsFailure)
            return BadRequest(new { code = MapErrorCode(result.Error), message = result.Message });

        return Ok(UserResourceAssembler.ToResource(result.Value!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await userCommandService.Handle(new DeleteResidentCommand(id), ct);
        if (result.IsFailure)
            return NotFound(new { code = MapErrorCode(result.Error), message = result.Message });

        return NoContent();
    }

    private static string MapErrorCode(Enum? error) =>
        error switch
        {
            IamError.ResidentFieldsRequired => "RESIDENT_FIELDS_REQUIRED",
            IamError.ResidentCodeAlreadyExists => "RESIDENT_CODE_ALREADY_EXISTS",
            IamError.ResidentOwnerRequired => "RESIDENT_OWNER_REQUIRED",
            IamError.ResidentNotFound => "RESIDENT_NOT_FOUND",
            IamError.ResidentPlanLimitReached => "RESIDENT_PLAN_LIMIT_REACHED",
            IamError.EmailAlreadyExists => "EMAIL_ALREADY_EXISTS",
            _ => "USER_ERROR",
        };
}
