using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Incidents.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Incidents.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class IncidentRepository(AppDbContext context)
    : BaseRepository<Incident>(context), IIncidentRepository
{
    private IQueryable<Incident> WithOwner() =>
        Context.Set<Incident>().Include(i => i.OwnerAdmin);

    public async Task<Incident?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(i => i.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<Incident>> ListByOwnerAdminIdAsync(
        int ownerAdminId,
        CancellationToken cancellationToken = default)
        => await WithOwner()
            .Where(i => i.OwnerAdminId == ownerAdminId)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> AnyIncidentsAsync(CancellationToken cancellationToken = default)
        => await Context.Set<Incident>().AnyAsync(cancellationToken);
}
