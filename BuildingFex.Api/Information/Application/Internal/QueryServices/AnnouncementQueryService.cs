using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Information.Application.QueryServices;
using BuildingFex.Api.Information.Domain.Model.Aggregates;
using BuildingFex.Api.Information.Domain.Repositories;

namespace BuildingFex.Api.Information.Application.Internal.QueryServices;

public class AnnouncementQueryService(
    IAnnouncementRepository announcementRepository,
    IUserRepository userRepository) : IAnnouncementQueryService
{
    public async Task<IEnumerable<Announcement>> ListByOwnerExternalIdAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerAdminExternalId))
            return await announcementRepository.ListAsync(cancellationToken);

        var owner = await userRepository.FindByExternalIdAsync(ownerAdminExternalId, cancellationToken);
        if (owner is null)
            return [];

        return await announcementRepository.ListByOwnerAdminIdAsync(owner.Id, cancellationToken);
    }
}
