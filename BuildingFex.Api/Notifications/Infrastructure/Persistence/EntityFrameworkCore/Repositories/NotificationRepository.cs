using BuildingFex.Api.Notifications.Domain.Model.Aggregates;
using BuildingFex.Api.Notifications.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Notifications.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class NotificationRepository(AppDbContext context)
    : BaseRepository<Notification>(context), INotificationRepository
{
    public async Task<Notification?> FindByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId)) return null;
        return await Context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.ExternalId == externalId.Trim(), ct);
    }

    public async Task<List<Notification>> ListByRecipientAsync(
        string recipientExternalId,
        string? role = null,
        bool? unreadOnly = null,
        CancellationToken ct = default)
    {
        var query = Context.Set<Notification>()
            .Where(n => n.RecipientExternalId == recipientExternalId.Trim());

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(n => n.RecipientRole == normalizedRole);
        }

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> CountUnreadByRecipientAsync(
        string recipientExternalId,
        string? role = null,
        CancellationToken ct = default)
    {
        var query = Context.Set<Notification>()
            .Where(n => n.RecipientExternalId == recipientExternalId.Trim() && !n.IsRead);

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(n => n.RecipientRole == normalizedRole);
        }

        return await query.CountAsync(ct);
    }
}

