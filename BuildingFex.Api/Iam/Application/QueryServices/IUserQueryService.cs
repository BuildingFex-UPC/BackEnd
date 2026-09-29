using BuildingFex.Api.Iam.Domain.Model.Aggregates;

namespace BuildingFex.Api.Iam.Application.QueryServices;

public interface IUserQueryService
{
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetResidentsByOwnerExternalIdAsync(
        string ownerAdminExternalId,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> SearchAsync(
        string? email = null,
        string? role = null,
        string? code = null,
        string? ownerAdminExternalId = null,
        CancellationToken cancellationToken = default);
}
