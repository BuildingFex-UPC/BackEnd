using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Incidents.Interfaces.Rest.Resources;

namespace BuildingFex.Api.Incidents.Interfaces.Rest.Transform;

public static class IncidentResourceAssembler
{
    public static IncidentResource ToResource(Incident incident) =>
        new(
            incident.ExternalId,
            incident.ResidentExternalId,
            incident.ResidentName,
            incident.Description,
            incident.Status,
            incident.ReportedAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            incident.Provider ?? string.Empty,
            incident.OwnerAdmin?.ExternalId ?? string.Empty);
}
