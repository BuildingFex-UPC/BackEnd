using BuildingFex.Api.Iam.Domain.Model.Aggregates;

namespace BuildingFex.Api.Iam.Application.Internal.OutboundServices;

public interface ITokenService
{
    string GenerateToken(User user);
}
