using BuildingFex.Api.SocialSpaces.Application.Internal;
using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;
using BuildingFex.Api.SocialSpaces.Domain.Repositories;
using BuildingFex.Api.SocialSpaces.Interfaces.Rest.Transform;
using BuildingFex.Api.Shared.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BuildingFex.Api.SocialSpaces.Interfaces.Rest.Compat;

[ApiController]
[Route("socialSpaces")]
[Authorize]
public class SocialSpacesCompatController(
    ISocialSpaceRepository socialSpaceRepository,
    SocialSpacesOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var spaces = await socialSpaceRepository.ListByOwnerExternalIdAsync(ownerAdminId, ct);
        return Ok(spaces.Select(SocialSpacesCompatSerializer.SpaceToJson));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var space = await socialSpaceRepository.FindByExternalIdAsync(id, ct);
        if (space is null)
            return NotFound();

        return Ok(SocialSpacesCompatSerializer.SpaceToJson(space));
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

        var name = body.TryGetProperty("name", out var nameProp) ? nameProp.GetString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { code = "SPACE_NAME_REQUIRED", message = "El nombre es obligatorio." });

        var externalId = body.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString() ?? $"space-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : $"space-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        int? capacity = null;
        if (body.TryGetProperty("capacity", out var capProp) && capProp.ValueKind == JsonValueKind.Number)
            capacity = capProp.GetInt32();

        var space = SocialSpace.Create(
            externalId,
            owner.Id,
            name,
            body.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty,
            capacity,
            body.TryGetProperty("imageUrl", out var imgProp) ? imgProp.GetString() ?? string.Empty : string.Empty);

        await socialSpaceRepository.AddAsync(space, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = await socialSpaceRepository.FindByExternalIdAsync(externalId, ct);
        return StatusCode(StatusCodes.Status201Created, SocialSpacesCompatSerializer.SpaceToJson(created!));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromBody] JsonElement body, CancellationToken ct)
    {
        var space = await socialSpaceRepository.FindByExternalIdAsync(id, ct);
        if (space is null)
            return NotFound(new { code = "SPACE_NOT_FOUND", message = "Espacio no encontrado." });

        var name = body.TryGetProperty("name", out var nameProp) ? nameProp.GetString()?.Trim() : space.Name;
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { code = "SPACE_NAME_REQUIRED", message = "El nombre es obligatorio." });

        int? capacity = space.Capacity;
        if (body.TryGetProperty("capacity", out var capProp))
            capacity = capProp.ValueKind == JsonValueKind.Null ? null : capProp.GetInt32();

        string? imageUrl = body.TryGetProperty("imageUrl", out var imgProp)
            ? imgProp.GetString() ?? string.Empty
            : null;

        space.Patch(
            name,
            body.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : space.Description,
            capacity,
            imageUrl);

        socialSpaceRepository.Update(space);
        await unitOfWork.CompleteAsync(ct);

        var updated = await socialSpaceRepository.FindByExternalIdAsync(id, ct);
        return Ok(SocialSpacesCompatSerializer.SpaceToJson(updated!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var space = await socialSpaceRepository.FindByExternalIdAsync(id, ct);
        if (space is null)
            return NotFound(new { code = "SPACE_NOT_FOUND", message = "Espacio no encontrado." });

        socialSpaceRepository.Remove(space);
        await unitOfWork.CompleteAsync(ct);
        return NoContent();
    }
}
