using BuildingFex.Api.Notifications.Application.QueryServices;
using BuildingFex.Api.Notifications.Domain.Model.Aggregates;
using BuildingFex.Api.Notifications.Domain.Repositories;

namespace BuildingFex.Api.Notifications.Application.Internal.QueryServices;

public class NotificationQueryService(INotificationRepository notificationRepository) : INotificationQueryService
{
    public async Task<List<Notification>> ListByRecipientAsync(
        string recipientExternalId,
        string? role = null,
        bool? unreadOnly = null,
        CancellationToken ct = default)
    {
        return await notificationRepository.ListByRecipientAsync(recipientExternalId, role, unreadOnly, ct);
    }

    public async Task<int> GetUnreadCountAsync(
        string recipientExternalId,
        string? role = null,
        CancellationToken ct = default)
    {
        return await notificationRepository.CountUnreadByRecipientAsync(recipientExternalId, role, ct);
    }
}

