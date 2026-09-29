using BuildingFex.Api.Information.Domain.Model.Aggregates;
using BuildingFex.Api.Information.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Information.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class AnnouncementRepository(AppDbContext context)
    : BaseRepository<Announcement>(context), IAnnouncementRepository
{
    private IQueryable<Announcement> WithOwner() =>
        Context.Set<Announcement>().Include(a => a.OwnerAdmin);

    public async Task<Announcement?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(a => a.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<Announcement>> ListByOwnerAdminIdAsync(
        int ownerAdminId,
        CancellationToken cancellationToken = default)
        => await WithOwner()
            .Where(a => a.OwnerAdminId == ownerAdminId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> AnyAnnouncementsAsync(CancellationToken cancellationToken = default)
        => await Context.Set<Announcement>().AnyAsync(cancellationToken);
}
