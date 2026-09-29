using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;

namespace BuildingFex.Api.SocialSpaces.Application.Internal;

public class SocialSpacesOwnerResolver(IUserRepository userRepository)
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
