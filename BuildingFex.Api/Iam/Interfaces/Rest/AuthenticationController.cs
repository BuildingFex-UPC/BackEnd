using BuildingFex.Api.Iam.Application.CommandServices;
using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Iam.Domain.Model.Commands;
using BuildingFex.Api.Iam.Application.QueryServices;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Iam.Interfaces.Rest.Resources;
using BuildingFex.Api.Iam.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFex.Api.Iam.Interfaces.Rest;

/// <summary>
/// API versionada (estilo learning-center). Uso recomendado a largo plazo.
/// </summary>
[ApiController]
[Route("api/v1/authentication")]
[AllowAnonymous]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService,
    IUserRepository userRepository) : ControllerBase
{
    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInResource resource, CancellationToken ct)
    {
        var result = await userCommandService.Handle(new SignInCommand(resource.Email, resource.Password), ct);
        if (result.IsFailure)
            return BadRequest(new { code = MapErrorCode(result.Error), message = result.Message });

        return Ok(new AuthenticatedUserResource(
            UserResourceAssembler.ToResource(result.Value!.user),
            result.Value.token));
    }

    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminResource resource, CancellationToken ct)
    {
        var result = await userCommandService.Handle(
            new RegisterAdminCommand(
                resource.Name,
                resource.Email,
                resource.Password,
                resource.Dni,
                resource.Address,
                resource.Company,
                resource.Ruc),
            ct);

        if (result.IsFailure)
            return BadRequest(new { code = MapErrorCode(result.Error), message = result.Message });

        return Ok(new AuthenticatedUserResource(
            UserResourceAssembler.ToResource(result.Value!.user),
            result.Value.token));
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email, CancellationToken ct)
    {
        var exists = await userRepository.ExistsByEmailAsync(email ?? string.Empty, ct);
        return Ok(new { exists });
    }

    [HttpGet("residents/invite")]
    public async Task<IActionResult> GetResidentInvite([FromQuery] string code, CancellationToken ct)
    {
        var residents = await userQueryService.SearchAsync(
            role: "resident",
            code: code ?? string.Empty,
            ownerAdminExternalId: null,
            cancellationToken: ct);

        var resident = residents.FirstOrDefault();
        if (resident is null)
            return NotFound(new { code = "RESIDENT_NOT_FOUND", message = "Residente no encontrado." });

        return Ok(UserResourceAssembler.ToResource(resident));
    }

    [HttpPost("residents/set-credentials")]
    public async Task<IActionResult> SetResidentCredentials(
        [FromBody] SetResidentCredentialsByCodeResource resource,
        CancellationToken ct)
    {
        var residents = await userQueryService.SearchAsync(
            role: "resident",
            code: resource.Code ?? string.Empty,
            ownerAdminExternalId: null,
            cancellationToken: ct);

        var resident = residents.FirstOrDefault();
        if (resident is null)
            return BadRequest(new { code = "RESIDENT_NOT_FOUND", message = "Residente no encontrado." });

        var updateResult = await userCommandService.Handle(
            new UpdateResidentCredentialsCommand(
                resident.ExternalId,
                resource.Email ?? string.Empty,
                resource.Password ?? string.Empty),
            ct);

        if (updateResult.IsFailure)
            return BadRequest(new { code = MapErrorCode(updateResult.Error), message = updateResult.Message });

        var signInResult = await userCommandService.Handle(
            new SignInCommand(resource.Email ?? string.Empty, resource.Password ?? string.Empty),
            ct);

        if (signInResult.IsFailure)
            return BadRequest(new { code = MapErrorCode(signInResult.Error), message = signInResult.Message });

        return Ok(new AuthenticatedUserResource(
            UserResourceAssembler.ToResource(signInResult.Value!.user),
            signInResult.Value.token));
    }

    private static string MapErrorCode(Enum? error) =>
        error switch
        {
            IamError.EmailNotFound => "EMAIL_NOT_FOUND",
            IamError.InvalidPassword => "INVALID_PASSWORD",
            IamError.EmailAlreadyExists => "EMAIL_ALREADY_EXISTS",
            IamError.InvalidCredentials => "INVALID_PASSWORD",
            IamError.ResidentNotFound => "RESIDENT_NOT_FOUND",
            IamError.ResidentFieldsRequired => "RESIDENT_FIELDS_REQUIRED",
            _ => "AUTH_ERROR",
        };
}
