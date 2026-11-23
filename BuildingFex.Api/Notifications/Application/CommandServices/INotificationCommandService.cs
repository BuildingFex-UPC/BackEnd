using BuildingFex.Api.Notifications.Domain.Model.Aggregates;

namespace BuildingFex.Api.Notifications.Application.CommandServices;

public interface INotificationCommandService
{
    Task<Notification> CreateNotificationAsync(
        string recipientExternalId,
        string recipientRole,
        string title,
        string message,
        string type = "general",
        string? relatedEntityId = null,
        CancellationToken ct = default);

    Task<bool> MarkAsReadAsync(string externalId, CancellationToken ct = default);

    Task<int> MarkAllAsReadAsync(string recipientExternalId, string? role = null, CancellationToken ct = default);
}

