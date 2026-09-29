using System.Text.Json;
using BuildingFex.Api.Iam.Application.Internal.OutboundServices;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;
namespace BuildingFex.Api.Iam.Infrastructure.Persistence.Seeding;

public class DbJsonUserSeeder(
    IUserRepository userRepository,
    IHashingService hashingService,
    IUnitOfWork unitOfWork,
    ILogger<DbJsonUserSeeder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task SeedAsync(string dbJsonPath, CancellationToken cancellationToken = default)
    {
        if (await userRepository.AnyUsersAsync(cancellationToken))
        {
            logger.LogInformation("Users table already seeded — skipping.");
            return;
        }

        if (!File.Exists(dbJsonPath))
        {
            logger.LogWarning("Seed file not found at {Path} — skipping user seed.", dbJsonPath);
            return;
        }

        await using var stream = File.OpenRead(dbJsonPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("users", out var usersElement) ||
            usersElement.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("No users array in {Path} — skipping user seed.", dbJsonPath);
            return;
        }

        var entries = usersElement.EnumerateArray()
            .Select(e => JsonSerializer.Deserialize<DbJsonUser>(e.GetRawText(), JsonOptions))
            .Where(u => u is not null)
            .Cast<DbJsonUser>()
            .ToList();

        var externalIdToInternalId = new Dictionary<string, int>(StringComparer.Ordinal);
        var seededCount = 0;

        foreach (var entry in entries.Where(u => u.Role == "admin"))
        {
            var user = User.CreateAdmin(
                entry.Id,
                entry.Name ?? string.Empty,
                entry.Email ?? string.Empty,
                HashOrEmpty(entry.Password),
                entry.Dni,
                entry.Address,
                entry.Company,
                entry.Ruc);

            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            externalIdToInternalId[entry.Id] = user.Id;
            seededCount++;
        }

        foreach (var entry in entries.Where(u => u.Role == "resident"))
        {
            if (string.IsNullOrWhiteSpace(entry.OwnerAdminId) ||
                !externalIdToInternalId.TryGetValue(entry.OwnerAdminId, out var ownerId))
            {
                logger.LogWarning(
                    "Skipping resident {ExternalId}: owner admin {OwnerAdminId} not found.",
                    entry.Id,
                    entry.OwnerAdminId);
                continue;
            }

            DateOnly? admissionDate = null;
            if (!string.IsNullOrWhiteSpace(entry.AdmissionDate) &&
                DateOnly.TryParse(entry.AdmissionDate, out var parsed))
                admissionDate = parsed;

            var user = User.CreateResident(
                entry.Id,
                entry.Name ?? string.Empty,
                entry.Email,
                HashOrEmpty(entry.Password),
                entry.Floor ?? string.Empty,
                entry.Code ?? string.Empty,
                ownerId,
                admissionDate);

            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            seededCount++;
        }

        logger.LogInformation("Seeded {Count} users from {Path}.", seededCount, dbJsonPath);
    }

    private string HashOrEmpty(string? password) =>
        string.IsNullOrWhiteSpace(password)
            ? string.Empty
            : hashingService.HashPassword(password);

    private sealed class DbJsonUser
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string Role { get; set; } = "resident";
        public string? Dni { get; set; }
        public string? Address { get; set; }
        public string? Company { get; set; }
        public string? Ruc { get; set; }
        public string? Floor { get; set; }
        public string? Code { get; set; }
        public string? AdmissionDate { get; set; }
        public string? OwnerAdminId { get; set; }
    }
}
