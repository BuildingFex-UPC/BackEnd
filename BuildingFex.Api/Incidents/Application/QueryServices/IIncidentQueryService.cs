using BuildingFex.Api.Incidents.Domain.Model.Aggregates;

namespace BuildingFex.Api.Incidents.Application.QueryServices;

public interface IIncidentQueryService
{
    Task<IEnumerable<Incident>> ListByOwnerExternalIdAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default);
}
