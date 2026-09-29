using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;
using BuildingFex.Api.SocialSpaces.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.SocialSpaces.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SocialSpaceRepository(AppDbContext context)
    : BaseRepository<SocialSpace>(context), ISocialSpaceRepository
{
    private IQueryable<SocialSpace> WithOwner() =>
        Context.Set<SocialSpace>().Include(s => s.OwnerAdmin);

    public async Task<SocialSpace?> FindByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(s => s.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<SocialSpace>> ListByOwnerExternalIdAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(s => s.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<SocialSpace>().AnyAsync(cancellationToken);
}
