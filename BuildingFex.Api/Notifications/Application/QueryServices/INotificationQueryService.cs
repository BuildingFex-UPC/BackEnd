using BuildingFex.Api.Notifications.Domain.Model.Aggregates;

namespace BuildingFex.Api.Notifications.Application.QueryServices;

public interface INotificationQueryService
{
    Task<List<Notification>> ListByRecipientAsync(
        string recipientExternalId,
        string? role = null,
        bool? unreadOnly = null,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(
        string recipientExternalId,
        string? role = null,
        CancellationToken ct = default);
}

