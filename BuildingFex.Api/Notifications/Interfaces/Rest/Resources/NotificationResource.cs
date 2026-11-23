namespace BuildingFex.Api.Notifications.Interfaces.Rest.Resources;

public record NotificationResource(
    string Id,
    string RecipientId,
    string Role,
    string Title,
    string Message,
    string Type,
    string? RelatedEntityId,
    bool IsRead,
    string CreatedAt,
    string? ReadAt);

