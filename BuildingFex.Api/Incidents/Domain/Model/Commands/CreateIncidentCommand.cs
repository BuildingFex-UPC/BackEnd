namespace BuildingFex.Api.Incidents.Domain.Model.Commands;

public record CreateIncidentCommand(
    string ExternalId,
    string OwnerAdminExternalId,
    string Description,
    string Status,
    string? ResidentExternalId,
    string? ResidentName,
    string? Provider,
    string? CreatedAt);
