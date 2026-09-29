namespace BuildingFex.Api.Iam.Infrastructure.Tokens.Jwt.Configuration;

public class TokenSettings
{
    public string Secret { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 24;
}
