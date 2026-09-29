using BuildingFex.Api.Incidents.Application.CommandServices;
using BuildingFex.Api.Incidents.Application.QueryServices;
using BuildingFex.Api.Incidents.Domain.Model;
using BuildingFex.Api.Incidents.Domain.Model.Commands;
using BuildingFex.Api.Incidents.Interfaces.Rest.Resources;
using BuildingFex.Api.Incidents.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFex.Api.Incidents.Interfaces.Rest.Compat;

[ApiController]
[Route("incidents")]
[Authorize]
public class IncidentsCompatController(
    IIncidentQueryService incidentQueryService,
    IIncidentCommandService incidentCommandService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerAdminId, CancellationToken ct)
    {
        var incidents = await incidentQueryService.ListByOwnerExternalIdAsync(ownerAdminId, ct);
        return Ok(incidents.Select(IncidentResourceAssembler.ToResource));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentCompatResource resource, CancellationToken ct)
    {
        var result = await incidentCommandService.Handle(
            new CreateIncidentCommand(
                resource.Id ?? string.Empty,
                resource.OwnerAdminId ?? string.Empty,
                resource.Description ?? string.Empty,
                resource.Status ?? "open",
                resource.ResidentId,
                resource.ResidentName,
                resource.Provider,
                resource.CreatedAt),
            ct);

        if (result.IsFailure)
            return BadRequest(new { code = MapErrorCode(result.Error), message = result.Message });

        return StatusCode(StatusCodes.Status201Created, IncidentResourceAssembler.ToResource(result.Value!));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateIncidentCompatResource resource,
        CancellationToken ct)
    {
        var result = await incidentCommandService.Handle(
            new UpdateIncidentCommand(
                id,
                resource.Description ?? string.Empty,
                resource.Status ?? "open",
                resource.Provider,
                resource.ResidentId,
                resource.ResidentName),
            ct);

        if (result.IsFailure)
            return result.Error switch
            {
                IncidentError.IncidentNotFound => NotFound(new { code = "INCIDENT_NOT_FOUND", message = result.Message }),
                _ => BadRequest(new { code = MapErrorCode(result.Error), message = result.Message }),
            };

        return Ok(IncidentResourceAssembler.ToResource(result.Value!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await incidentCommandService.Handle(new DeleteIncidentCommand(id), ct);
        if (result.IsFailure)
            return NotFound(new { code = "INCIDENT_NOT_FOUND", message = result.Message });

        return NoContent();
    }

    private static string MapErrorCode(Enum? error) =>
        error switch
        {
            IncidentError.IncidentNotFound => "INCIDENT_NOT_FOUND",
            IncidentError.OwnerAdminRequired => "OWNER_ADMIN_REQUIRED",
            IncidentError.DescriptionRequired => "DESCRIPTION_REQUIRED",
            IncidentError.InvalidStatus => "INVALID_STATUS",
            IncidentError.ResidentRequired => "RESIDENT_REQUIRED",
            IncidentError.ProviderRequired => "PROVIDER_REQUIRED",
            _ => "INCIDENT_ERROR",
        };
}
