using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;

namespace BuildingFex.Api.Finances.Application.Internal;

public class FinanceOwnerResolver(IUserRepository userRepository)
{
    public async Task<User?> ResolveOwnerAdminAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerAdminExternalId))
            return null;

        var owner = await userRepository.FindByExternalIdAsync(ownerAdminExternalId.Trim(), cancellationToken);
        if (owner is null || owner.Role != "admin")
            return null;

        return owner;
    }
}
