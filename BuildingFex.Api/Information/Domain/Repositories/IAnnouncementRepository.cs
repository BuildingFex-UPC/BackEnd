using BuildingFex.Api.Information.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Information.Domain.Repositories;

public interface IAnnouncementRepository : IBaseRepository<Announcement>
{
    Task<Announcement?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Announcement>> ListByOwnerAdminIdAsync(int ownerAdminId, CancellationToken cancellationToken = default);
    Task<bool> AnyAnnouncementsAsync(CancellationToken cancellationToken = default);
}
