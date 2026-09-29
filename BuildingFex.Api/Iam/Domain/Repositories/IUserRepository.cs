using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Iam.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> ListByOwnerAdminIdAsync(int ownerAdminId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> SearchAsync(
        string? email = null,
        string? role = null,
        string? code = null,
        int? ownerAdminId = null,
        CancellationToken cancellationToken = default);
    Task<bool> AnyUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsResidentByCodeAsync(
        string code,
        int? ownerAdminId = null,
        CancellationToken cancellationToken = default);
    Task<int> CountResidentsByOwnerAdminIdAsync(int ownerAdminId, CancellationToken cancellationToken = default);
}
