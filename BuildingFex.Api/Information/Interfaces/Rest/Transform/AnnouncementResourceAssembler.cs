using BuildingFex.Api.Information.Domain.Model.Aggregates;
using BuildingFex.Api.Information.Interfaces.Rest.Resources;

namespace BuildingFex.Api.Information.Interfaces.Rest.Transform;

public static class AnnouncementResourceAssembler
{
    public static AnnouncementResource ToResource(Announcement announcement) =>
        new(
            announcement.ExternalId,
            announcement.Title,
            announcement.Body,
            announcement.Priority,
            announcement.Duration,
            announcement.OwnerAdmin?.ExternalId ?? string.Empty,
            announcement.AuthorName,
            announcement.CreatedAt?.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? string.Empty,
            announcement.ExpiresAt?.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? string.Empty,
            announcement.OwnerAdmin?.ExternalId ?? string.Empty);
}
