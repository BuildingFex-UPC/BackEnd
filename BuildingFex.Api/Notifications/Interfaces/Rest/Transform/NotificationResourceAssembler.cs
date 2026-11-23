using BuildingFex.Api.Notifications.Domain.Model.Aggregates;
using BuildingFex.Api.Notifications.Interfaces.Rest.Resources;

namespace BuildingFex.Api.Notifications.Interfaces.Rest.Transform;

public static class NotificationResourceAssembler
{
    public static NotificationResource ToResource(Notification entity) =>
        new(
            Id: entity.ExternalId,
            RecipientId: entity.RecipientExternalId,
            Role: entity.RecipientRole,
            Title: entity.Title,
            Message: entity.Message,
            Type: entity.Type,
            RelatedEntityId: entity.RelatedEntityId,
            IsRead: entity.IsRead,
            CreatedAt: entity.CreatedAt?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
            ReadAt: entity.ReadAt?.ToString("o"));
}
