using BuildingFex.Api.Iam.Application.QueryServices;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;

namespace BuildingFex.Api.Iam.Application.Internal.QueryServices;

public class UserQueryService(IUserRepository userRepository) : IUserQueryService
{
    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        => await userRepository.FindByExternalIdAsync(externalId, cancellationToken);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await userRepository.ListAsync(cancellationToken);

    public async Task<IEnumerable<User>> GetResidentsByOwnerExternalIdAsync(
        string ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        var owner = await userRepository.FindByExternalIdAsync(ownerAdminExternalId, cancellationToken);
        if (owner is null)
            return [];

        return await userRepository.ListByOwnerAdminIdAsync(owner.Id, cancellationToken);
    }

    public async Task<IEnumerable<User>> SearchAsync(
        string? email = null,
        string? role = null,
        string? code = null,
        string? ownerAdminExternalId = null,
        CancellationToken cancellationToken = default)
    {
        int? ownerAdminId = null;
        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
        {
            var owner = await userRepository.FindByExternalIdAsync(ownerAdminExternalId, cancellationToken);
            if (owner is null)
                return [];

            ownerAdminId = owner.Id;
        }

        return await userRepository.SearchAsync(email, role, code, ownerAdminId, cancellationToken);
    }
}
