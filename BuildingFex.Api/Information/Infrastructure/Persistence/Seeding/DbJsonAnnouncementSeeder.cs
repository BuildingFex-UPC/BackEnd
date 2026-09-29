using System.Text.Json;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Information.Domain.Model.Aggregates;
using BuildingFex.Api.Information.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Information.Infrastructure.Persistence.Seeding;

public class DbJsonAnnouncementSeeder(
    IAnnouncementRepository announcementRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<DbJsonAnnouncementSeeder> logger)
{
    public async Task SeedAsync(string dbJsonPath, CancellationToken cancellationToken = default)
    {
        if (await announcementRepository.AnyAnnouncementsAsync(cancellationToken))
        {
            logger.LogInformation("Announcements table already seeded — skipping.");
            return;
        }

        if (!File.Exists(dbJsonPath))
        {
            logger.LogWarning("Seed file not found at {Path} — skipping announcements seed.", dbJsonPath);
            return;
        }

        await using var stream = File.OpenRead(dbJsonPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("announcements", out var announcementsElement) ||
            announcementsElement.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("No announcements array in {Path} — skipping announcements seed.", dbJsonPath);
            return;
        }

        var entries = announcementsElement.EnumerateArray()
            .Select(e => JsonSerializer.Deserialize<DbJsonAnnouncement>(e.GetRawText()))
            .Where(a => a is not null)
            .Cast<DbJsonAnnouncement>()
            .ToList();

        var seeded = 0;
        foreach (var entry in entries)
        {
            var ownerExternalId = entry.OwnerAdminId ?? entry.AuthorId;
            if (string.IsNullOrWhiteSpace(ownerExternalId))
            {
                logger.LogWarning("Skipping announcement {ExternalId}: missing ownerAdminId.", entry.Id);
                continue;
            }

            var owner = await userRepository.FindByExternalIdAsync(ownerExternalId, cancellationToken);
            if (owner is null)
            {
                logger.LogWarning(
                    "Skipping announcement {ExternalId}: owner admin {OwnerAdminId} not found.",
                    entry.Id,
                    ownerExternalId);
                continue;
            }

            DateTimeOffset? expiresAt = null;
            if (!string.IsNullOrWhiteSpace(entry.ExpiresAt) &&
                DateTimeOffset.TryParse(entry.ExpiresAt, out var parsedExpires))
                expiresAt = parsedExpires;

            DateTimeOffset? createdAt = null;
            if (!string.IsNullOrWhiteSpace(entry.CreatedAt) &&
                DateTimeOffset.TryParse(entry.CreatedAt, out var parsedCreated))
                createdAt = parsedCreated;

            var announcement = Announcement.Create(
                entry.Id,
                owner.Id,
                entry.Title ?? string.Empty,
                entry.Body ?? string.Empty,
                entry.Priority ?? "normal",
                entry.Duration,
                entry.AuthorName,
                expiresAt,
                createdAt);

            await announcementRepository.AddAsync(announcement, cancellationToken);
            seeded++;
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} announcements from {Path}.", seeded, dbJsonPath);
    }

    private sealed class DbJsonAnnouncement
    {
        public string Id { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Priority { get; set; }
        public int Duration { get; set; }
        public string? AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? CreatedAt { get; set; }
        public string? ExpiresAt { get; set; }
        public string? OwnerAdminId { get; set; }
    }
}
