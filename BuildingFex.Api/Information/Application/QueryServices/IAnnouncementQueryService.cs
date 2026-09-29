using BuildingFex.Api.Information.Domain.Model.Aggregates;

namespace BuildingFex.Api.Information.Application.QueryServices;

public interface IAnnouncementQueryService
{
    Task<IEnumerable<Announcement>> ListByOwnerExternalIdAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default);
}
