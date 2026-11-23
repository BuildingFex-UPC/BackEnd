using BuildingFex.Api.Notifications.Domain.Model.Aggregates;

namespace BuildingFex.Api.Notifications.Domain.Repositories;

public interface INotificationRepository
{
    Task<Notification?> FindByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<List<Notification>> ListByRecipientAsync(string recipientExternalId, string? role = null, bool? unreadOnly = null, CancellationToken ct = default);
    Task<int> CountUnreadByRecipientAsync(string recipientExternalId, string? role = null, CancellationToken ct = default);
    Task AddAsync(Notification entity, CancellationToken ct = default);
    void Update(Notification entity);
}

