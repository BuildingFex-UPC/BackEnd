using BuildingFex.Api.Information.Application.CommandServices;
using BuildingFex.Api.Information.Application.QueryServices;
using BuildingFex.Api.Information.Domain.Model;
using BuildingFex.Api.Information.Domain.Model.Commands;
using BuildingFex.Api.Information.Interfaces.Rest.Resources;
using BuildingFex.Api.Information.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFex.Api.Information.Interfaces.Rest.Compat;

[ApiController]
[Route("announcements")]
[Authorize]
public class AnnouncementsCompatController(
    IAnnouncementQueryService announcementQueryService,
    IAnnouncementCommandService announcementCommandService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var announcements = await announcementQueryService.ListByOwnerExternalIdAsync(ownerAdminId, ct);
        return Ok(announcements.Select(AnnouncementResourceAssembler.ToResource));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementCompatResource resource, CancellationToken ct)
    {
        var result = await announcementCommandService.Handle(
            new CreateAnnouncementCommand(
                resource.Id ?? string.Empty,
                resource.OwnerAdminId ?? resource.AuthorId ?? string.Empty,
                resource.Title ?? string.Empty,
                resource.Body ?? string.Empty,
                resource.Priority ?? "normal",
                resource.Duration,
                resource.AuthorName,
                resource.CreatedAt,
                resource.ExpiresAt),
            ct);

        if (result.IsFailure)
            return BadRequest(new { code = MapErrorCode(result.Error), message = result.Message });

        return StatusCode(StatusCodes.Status201Created, AnnouncementResourceAssembler.ToResource(result.Value!));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateAnnouncementCompatResource resource,
        CancellationToken ct)
    {
        var result = await announcementCommandService.Handle(
            new UpdateAnnouncementCommand(
                id,
                resource.Title ?? string.Empty,
                resource.Body ?? string.Empty,
                resource.Priority ?? "normal",
                resource.Duration),
            ct);

        if (result.IsFailure)
            return result.Error switch
            {
                AnnouncementError.AnnouncementNotFound => NotFound(new { code = "ANNOUNCEMENT_NOT_FOUND", message = result.Message }),
                _ => BadRequest(new { code = MapErrorCode(result.Error), message = result.Message }),
            };

        return Ok(AnnouncementResourceAssembler.ToResource(result.Value!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await announcementCommandService.Handle(new DeleteAnnouncementCommand(id), ct);
        if (result.IsFailure)
            return NotFound(new { code = "ANNOUNCEMENT_NOT_FOUND", message = result.Message });

        return NoContent();
    }

    private static string MapErrorCode(Enum? error) =>
        error switch
        {
            AnnouncementError.AnnouncementNotFound => "ANNOUNCEMENT_NOT_FOUND",
            AnnouncementError.OwnerAdminRequired => "OWNER_ADMIN_REQUIRED",
            AnnouncementError.TitleRequired => "TITLE_REQUIRED",
            _ => "ANNOUNCEMENT_ERROR",
        };
}
