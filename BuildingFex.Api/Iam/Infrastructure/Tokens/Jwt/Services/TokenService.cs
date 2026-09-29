using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BuildingFex.Api.Iam.Application.Internal.OutboundServices;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Infrastructure.Tokens.Jwt.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BuildingFex.Api.Iam.Infrastructure.Tokens.Jwt.Services;

public class TokenService(IOptions<TokenSettings> settings) : ITokenService
{
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Value.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.ExternalId),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("name", user.Name),
        };

        if (user.Role == "admin")
            claims.Add(new Claim("ownerAdminId", user.ExternalId));
        else if (user.OwnerAdmin is not null)
            claims.Add(new Claim("ownerAdminId", user.OwnerAdmin.ExternalId));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(settings.Value.ExpirationHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
