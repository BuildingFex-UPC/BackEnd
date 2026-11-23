using BuildingFex.Api.Notifications.Application.CommandServices;
using BuildingFex.Api.Notifications.Domain.Model.Aggregates;
using BuildingFex.Api.Notifications.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Notifications.Application.Internal.CommandServices;

public class NotificationCommandService(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork) : INotificationCommandService
{
    public async Task<Notification> CreateNotificationAsync(
        string recipientExternalId,
        string recipientRole,
        string title,
        string message,
        string type = "general",
        string? relatedEntityId = null,
        CancellationToken ct = default)
    {
        var notification = Notification.Create(
            string.Empty,
            recipientExternalId,
            recipientRole,
            title,
            message,
            type,
            relatedEntityId);

        await notificationRepository.AddAsync(notification, ct);
        await unitOfWork.CompleteAsync(ct);
        return notification;
    }

    public async Task<bool> MarkAsReadAsync(string externalId, CancellationToken ct = default)
    {
        var notification = await notificationRepository.FindByExternalIdAsync(externalId, ct);
        if (notification is null) return false;

        notification.MarkAsRead();
        notificationRepository.Update(notification);
        await unitOfWork.CompleteAsync(ct);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(string recipientExternalId, string? role = null, CancellationToken ct = default)
    {
        var notifications = await notificationRepository.ListByRecipientAsync(recipientExternalId, role, unreadOnly: true, ct);
        if (notifications.Count == 0) return 0;

        foreach (var notif in notifications)
        {
            notif.MarkAsRead();
            notificationRepository.Update(notif);
        }

        await unitOfWork.CompleteAsync(ct);
        return notifications.Count;
    }
}

