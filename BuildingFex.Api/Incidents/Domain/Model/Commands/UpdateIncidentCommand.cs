namespace BuildingFex.Api.Incidents.Domain.Model.Commands;

public record UpdateIncidentCommand(
    string ExternalId,
    string Description,
    string Status,
    string? Provider,
    string? ResidentExternalId,
    string? ResidentName);
