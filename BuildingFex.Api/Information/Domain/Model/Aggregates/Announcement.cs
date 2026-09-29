using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Information.Domain.Model.Aggregates;

public class Announcement : IAuditableEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Priority { get; private set; } = "normal";
    public int Duration { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Announcement()
    {
    }

    public static Announcement Create(
        string externalId,
        int ownerAdminId,
        string title,
        string body,
        string priority = "normal",
        int duration = 30,
        string? authorName = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? createdAt = null)
    {
        return new Announcement
        {
            ExternalId = externalId.Trim(),
            OwnerAdminId = ownerAdminId,
            Title = title.Trim(),
            Body = body?.Trim() ?? string.Empty,
            Priority = string.IsNullOrWhiteSpace(priority) ? "normal" : priority.Trim(),
            Duration = duration,
            AuthorName = authorName?.Trim() ?? string.Empty,
            ExpiresAt = expiresAt,
        };
    }

    public void Update(
        string title,
        string body,
        string priority,
        int duration)
    {
        Title = title.Trim();
        Body = body?.Trim() ?? string.Empty;
        Priority = string.IsNullOrWhiteSpace(priority) ? Priority : priority.Trim();
        Duration = duration;
    }
}
