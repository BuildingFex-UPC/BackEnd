using System.Text.Json;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;
using BuildingFex.Api.SocialSpaces.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.SocialSpaces.Infrastructure.Persistence.Seeding;

public class DbJsonSocialSpacesSeeder(
    ISocialSpaceRepository socialSpaceRepository,
    IReservationRepository reservationRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<DbJsonSocialSpacesSeeder> logger)
{
    private const int MaxImageLength = 2000;

    public async Task SeedAsync(string dbJsonPath, CancellationToken cancellationToken = default)
    {
        if (await socialSpaceRepository.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Social spaces table already seeded — skipping.");
            return;
        }

        if (!File.Exists(dbJsonPath))
        {
            logger.LogWarning("Seed file not found at {Path} — skipping social spaces seed.", dbJsonPath);
            return;
        }

        await using var stream = File.OpenRead(dbJsonPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var seeded = 0;
        seeded += await SeedSpacesAsync(document, cancellationToken);
        seeded += await SeedReservationsAsync(document, cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} social space records from {Path}.", seeded, dbJsonPath);
    }

    private async Task<int?> ResolveOwnerIdAsync(
        string? ownerExternalId,
        string label,
        string entityId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ownerExternalId))
        {
            logger.LogWarning("Skipping {Label} {Id}: missing ownerAdminId.", label, entityId);
            return null;
        }

        var owner = await userRepository.FindByExternalIdAsync(ownerExternalId, ct);
        if (owner is null)
        {
            logger.LogWarning(
                "Skipping {Label} {Id}: owner admin {OwnerAdminId} not found.",
                label,
                entityId,
                ownerExternalId);
            return null;
        }

        return owner.Id;
    }

    private async Task<int> SeedSpacesAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("socialSpaces", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.GetProperty("id").GetString() ?? string.Empty;
            var ownerId = await ResolveOwnerIdAsync(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "socialSpace",
                externalId,
                ct);
            if (ownerId is null)
                continue;

            var imageUrl = entry.TryGetProperty("imageUrl", out var imgProp)
                ? imgProp.GetString() ?? string.Empty
                : string.Empty;
            if (imageUrl.Length > MaxImageLength)
                imageUrl = string.Empty;

            int? capacity = entry.TryGetProperty("capacity", out var capProp) &&
                            capProp.ValueKind == JsonValueKind.Number
                ? capProp.GetInt32()
                : null;

            var space = SocialSpace.Create(
                externalId,
                ownerId.Value,
                entry.GetProperty("name").GetString() ?? string.Empty,
                entry.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty,
                capacity,
                imageUrl);

            await socialSpaceRepository.AddAsync(space, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedReservationsAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("reservations", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.GetProperty("id").GetString() ?? string.Empty;
            var ownerId = await ResolveOwnerIdAsync(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "reservation",
                externalId,
                ct);
            if (ownerId is null)
                continue;

            var guestsJson = entry.TryGetProperty("guests", out var guestsProp)
                ? guestsProp.GetRawText()
                : "[]";

            string? guestInviteToken = entry.TryGetProperty("guestInviteToken", out var tokenProp) &&
                                       tokenProp.ValueKind == JsonValueKind.String
                ? tokenProp.GetString()
                : null;

            var reservation = Reservation.Create(
                externalId,
                ownerId.Value,
                entry.GetProperty("spaceId").GetString() ?? string.Empty,
                entry.GetProperty("residentId").GetString() ?? string.Empty,
                entry.TryGetProperty("residentName", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
                entry.TryGetProperty("residentCode", out var codeProp) ? codeProp.GetString() ?? string.Empty : string.Empty,
                entry.GetProperty("date").GetString() ?? string.Empty,
                entry.GetProperty("startTime").GetString() ?? string.Empty,
                entry.GetProperty("endTime").GetString() ?? string.Empty,
                guestsJson,
                guestInviteToken);

            await reservationRepository.AddAsync(reservation, ct);
            count++;
        }

        return count;
    }
}
