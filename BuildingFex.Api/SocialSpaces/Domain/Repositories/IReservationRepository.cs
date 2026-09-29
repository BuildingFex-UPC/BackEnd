using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;

namespace BuildingFex.Api.SocialSpaces.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> ListAsync(
        string? ownerAdminExternalId,
        string? spaceExternalId,
        string? residentExternalId,
        string? date,
        string? guestInviteToken,
        CancellationToken cancellationToken = default);
    Task AddAsync(Reservation entity, CancellationToken cancellationToken = default);
    void Update(Reservation entity);
    void Remove(Reservation entity);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
