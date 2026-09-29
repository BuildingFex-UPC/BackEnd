using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;
using BuildingFex.Api.SocialSpaces.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.SocialSpaces.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class ReservationRepository(AppDbContext context)
    : BaseRepository<Reservation>(context), IReservationRepository
{
    private IQueryable<Reservation> WithOwner() =>
        Context.Set<Reservation>().Include(r => r.OwnerAdmin);

    public async Task<Reservation?> FindByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<Reservation>> ListAsync(
        string? ownerAdminExternalId,
        string? spaceExternalId,
        string? residentExternalId,
        string? date,
        string? guestInviteToken,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(guestInviteToken))
            query = query.Where(r => r.GuestInviteToken == guestInviteToken.Trim());
        else
        {
            if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
                query = query.Where(r => r.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

            if (!string.IsNullOrWhiteSpace(spaceExternalId))
                query = query.Where(r => r.SpaceExternalId == spaceExternalId.Trim());

            if (!string.IsNullOrWhiteSpace(residentExternalId))
                query = query.Where(r => r.ResidentExternalId == residentExternalId.Trim());

            if (!string.IsNullOrWhiteSpace(date))
                query = query.Where(r => r.Date == date.Trim());
        }

        return await query.OrderBy(r => r.Date).ThenBy(r => r.StartTime).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<Reservation>().AnyAsync(cancellationToken);
}
