using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;

namespace BuildingFex.Api.SocialSpaces.Domain.Repositories;

public interface ISocialSpaceRepository
{
    Task<SocialSpace?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SocialSpace>> ListByOwnerExternalIdAsync(string? ownerAdminExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(SocialSpace entity, CancellationToken cancellationToken = default);
    void Update(SocialSpace entity);
    void Remove(SocialSpace entity);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
