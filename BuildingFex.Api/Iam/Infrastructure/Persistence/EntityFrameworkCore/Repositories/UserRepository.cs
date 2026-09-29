using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class UserRepository(AppDbContext context)
    : BaseRepository<User>(context), IUserRepository
{
    private IQueryable<User> WithOwner() =>
        Context.Set<User>().Include(u => u.OwnerAdmin);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<User?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
            return false;

        return await Context.Set<User>().AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<IEnumerable<User>> ListByOwnerAdminIdAsync(
        int ownerAdminId,
        CancellationToken cancellationToken = default)
        => await WithOwner()
            .Where(u => u.OwnerAdminId == ownerAdminId && u.Role == "resident")
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<User>> SearchAsync(
        string? email = null,
        string? role = null,
        string? code = null,
        int? ownerAdminId = null,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email == normalized);
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role.Trim());

        if (!string.IsNullOrWhiteSpace(code))
            query = query.Where(u => u.Code == code.Trim());

        if (ownerAdminId.HasValue)
            query = query.Where(u => u.OwnerAdminId == ownerAdminId.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyUsersAsync(CancellationToken cancellationToken = default)
        => await Context.Set<User>().AnyAsync(cancellationToken);

    public async Task<bool> ExistsResidentByCodeAsync(
        string code,
        int? ownerAdminId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var normalized = code.Trim();
        var query = Context.Set<User>()
            .Where(u => u.Role == "resident" && u.Code == normalized);

        if (ownerAdminId.HasValue)
            query = query.Where(u => u.OwnerAdminId == ownerAdminId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> CountResidentsByOwnerAdminIdAsync(
        int ownerAdminId,
        CancellationToken cancellationToken = default)
        => await Context.Set<User>()
            .CountAsync(u => u.OwnerAdminId == ownerAdminId && u.Role == "resident", cancellationToken);
}
