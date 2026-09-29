using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Incidents.Application.QueryServices;
using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Incidents.Domain.Repositories;

namespace BuildingFex.Api.Incidents.Application.Internal.QueryServices;

public class IncidentQueryService(
    IIncidentRepository incidentRepository,
    IUserRepository userRepository) : IIncidentQueryService
{
    public async Task<IEnumerable<Incident>> ListByOwnerExternalIdAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Incident> incidents;
        if (string.IsNullOrWhiteSpace(ownerAdminExternalId))
        {
            return await incidentRepository.ListAsync(cancellationToken);
        }  else
        {
            var owner = await userRepository.FindByExternalIdAsync(ownerAdminExternalId, cancellationToken);
            if (owner is null) return [];
            incidents = await incidentRepository.ListByOwnerAdminIdAsync(owner.Id, cancellationToken);
        }
        incidents = incidents.OrderByDescending(i => i.ReportedAt);

        return incidents;
    }
}
