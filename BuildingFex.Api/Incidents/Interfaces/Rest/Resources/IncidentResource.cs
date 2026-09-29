namespace BuildingFex.Api.Incidents.Interfaces.Rest.Resources;

public record IncidentResource(
    string Id,
    string? ResidentId,
    string ResidentName,
    string Description,
    string Status,
    string CreatedAt,
    string? Provider,
    string OwnerAdminId);

public record CreateIncidentCompatResource(
    string? Id,
    string? ResidentId,
    string? ResidentName,
    string? Description,
    string? Status,
    string? CreatedAt,
    string? Provider,
    string? OwnerAdminId);

public record UpdateIncidentCompatResource(
    string? ResidentId,
    string? ResidentName,
    string? Description,
    string? Status,
    string? CreatedAt,
    string? Provider,
    string? OwnerAdminId);
