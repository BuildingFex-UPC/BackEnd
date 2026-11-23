using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Notifications.Domain.Model.Aggregates;

public class Notification : IAuditableEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string RecipientExternalId { get; private set; } = string.Empty;
    public string RecipientRole { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = "general";
    public string? RelatedEntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Notification()
    {
    }

    public static Notification Create(
        string externalId,
        string recipientExternalId,
        string recipientRole,
        string title,
        string message,
        string type = "general",
        string? relatedEntityId = null,
        DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(recipientExternalId))
            throw new ArgumentException("recipientExternalId es requerido.", nameof(recipientExternalId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("title es requerido.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("message es requerido.", nameof(message));

        var generatedId = string.IsNullOrWhiteSpace(externalId)
            ? $"notif-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}"[..32]
            : externalId.Trim();

        return new Notification
        {
            ExternalId = generatedId,
            RecipientExternalId = recipientExternalId.Trim(),
            RecipientRole = string.IsNullOrWhiteSpace(recipientRole) ? "general" : recipientRole.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            Message = message.Trim(),
            Type = string.IsNullOrWhiteSpace(type) ? "general" : type.Trim(),
            RelatedEntityId = string.IsNullOrWhiteSpace(relatedEntityId) ? null : relatedEntityId.Trim(),
            IsRead = false,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
