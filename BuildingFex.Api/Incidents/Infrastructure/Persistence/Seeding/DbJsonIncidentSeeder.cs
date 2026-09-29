using System.Text.Json;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Incidents.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Incidents.Infrastructure.Persistence.Seeding;

public class DbJsonIncidentSeeder(
    IIncidentRepository incidentRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<DbJsonIncidentSeeder> logger)
{
    public async Task SeedAsync(string dbJsonPath, CancellationToken cancellationToken = default)
    {
        if (await incidentRepository.AnyIncidentsAsync(cancellationToken))
        {
            logger.LogInformation("Incidents table already seeded — skipping.");
            return;
        }

        if (!File.Exists(dbJsonPath))
        {
            logger.LogWarning("Seed file not found at {Path} — skipping incidents seed.", dbJsonPath);
            return;
        }

        await using var stream = File.OpenRead(dbJsonPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("incidents", out var incidentsElement) ||
            incidentsElement.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("No incidents array in {Path} — skipping incidents seed.", dbJsonPath);
            return;
        }

        var entries = incidentsElement.EnumerateArray()
            .Select(e => JsonSerializer.Deserialize<DbJsonIncident>(e.GetRawText()))
            .Where(i => i is not null)
            .Cast<DbJsonIncident>()
            .ToList();

        var seeded = 0;
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.OwnerAdminId))
            {
                logger.LogWarning("Skipping incident {ExternalId}: missing ownerAdminId.", entry.Id);
                continue;
            }

            var owner = await userRepository.FindByExternalIdAsync(entry.OwnerAdminId, cancellationToken);
            if (owner is null)
            {
                logger.LogWarning(
                    "Skipping incident {ExternalId}: owner admin {OwnerAdminId} not found.",
                    entry.Id,
                    entry.OwnerAdminId);
                continue;
            }

            DateTimeOffset? reportedAt = null;
            if (!string.IsNullOrWhiteSpace(entry.CreatedAt) &&
                DateTimeOffset.TryParse(entry.CreatedAt, out var parsed))
                reportedAt = parsed;

            var incident = Incident.Create(
                entry.Id,
                owner.Id,
                entry.Description ?? string.Empty,
                entry.Status ?? "open",
                entry.ResidentId,
                entry.ResidentName,
                entry.Provider,
                reportedAt);

            await incidentRepository.AddAsync(incident, cancellationToken);
            seeded++;
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} incidents from {Path}.", seeded, dbJsonPath);
    }

    private sealed class DbJsonIncident
    {
        public string Id { get; set; } = string.Empty;
        public string? ResidentId { get; set; }
        public string? ResidentName { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? CreatedAt { get; set; }
        public string? Provider { get; set; }
        public string? OwnerAdminId { get; set; }
    }
}
