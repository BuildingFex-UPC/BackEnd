using BuildingFex.Api.Iam.Application.Internal.OutboundServices;

namespace BuildingFex.Api.Iam.Infrastructure.Hashing.BCrypt.Services;

public class HashingService : IHashingService
{
    public string HashPassword(string password) => global::BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
            return string.IsNullOrEmpty(password);

        return global::BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
