using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Incidents.Domain.Repositories;

public interface IIncidentRepository : IBaseRepository<Incident>
{
    Task<Incident?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Incident>> ListByOwnerAdminIdAsync(int ownerAdminId, CancellationToken cancellationToken = default);
    Task<bool> AnyIncidentsAsync(CancellationToken cancellationToken = default);
}
