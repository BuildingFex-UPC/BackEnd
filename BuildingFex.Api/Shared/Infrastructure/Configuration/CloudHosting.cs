
namespace BuildingFex.Api.Shared.Infrastructure.Configuration;

public static class CloudHosting
{
    public static void ConfigureKestrelPort(WebApplicationBuilder builder)
    {
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
            builder.WebHost.UseUrls($"http://+:{port}");
    }

    public static string ResolveConnectionString(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var platformConnection = TryBuildFromPlatformEnv();
        if (platformConnection is not null)
            return platformConnection;

        var configured = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException(
                "Database connection is not configured. Set ConnectionStrings__DefaultConnection " +
                "in your host's environment variables.");

        if (!environment.IsDevelopment() && IsLoopback(configured))
        {
            throw new InvalidOperationException(
                "The configured database connection points at localhost, which cannot work once the API " +
                "runs in a container or on a remote host. Set ConnectionStrings__DefaultConnection in your " +
                "host's environment variables to the connection string of your database.");
        }

        return configured;
    }

    public static void ApplySecretsFromEnvironment(WebApplicationBuilder builder)
    {
        var secret = FirstEnv(JwtSecretKeys);

        if (!string.IsNullOrWhiteSpace(secret))
            builder.Configuration["TokenSettings:Secret"] = secret;

        var mpAccessToken = FirstEnv("MercadoPago__AccessToken", "MP_ACCESS_TOKEN");
        if (!string.IsNullOrWhiteSpace(mpAccessToken))
            builder.Configuration["MercadoPago:AccessToken"] = mpAccessToken;

        var mpPublicKey = FirstEnv("MercadoPago__PublicKey", "MP_PUBLIC_KEY");
        if (!string.IsNullOrWhiteSpace(mpPublicKey))
            builder.Configuration["MercadoPago:PublicKey"] = mpPublicKey;

        var mpWebhookSecret = FirstEnv("MercadoPago__WebhookSecret", "MP_WEBHOOK_SECRET");
        if (!string.IsNullOrWhiteSpace(mpWebhookSecret))
            builder.Configuration["MercadoPago:WebhookSecret"] = mpWebhookSecret;

        var mpFrontendUrl = FirstEnv("MercadoPago__FrontendBaseUrl", "MP_FRONTEND_BASE_URL");
        if (!string.IsNullOrWhiteSpace(mpFrontendUrl))
            builder.Configuration["MercadoPago:FrontendBaseUrl"] = mpFrontendUrl;

        var mpNotificationUrl = FirstEnv("MercadoPago__NotificationUrl", "MP_NOTIFICATION_URL");
        if (!string.IsNullOrWhiteSpace(mpNotificationUrl))
            builder.Configuration["MercadoPago:NotificationUrl"] = mpNotificationUrl;

        var connectionString = FirstEnv(
            "ConnectionStrings__DefaultConnection",
            "DATABASE_CONNECTION",
            "PG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(connectionString))
            builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }

    public static void ValidateProductionSecrets(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        var secret = configuration["TokenSettings:Secret"];
        if (!string.IsNullOrWhiteSpace(secret) &&
            !secret.Contains("CHANGE-ME", StringComparison.OrdinalIgnoreCase) &&
            secret.Length >= 32)
        {
            return;
        }

        var reason = string.IsNullOrWhiteSpace(secret)
            ? "no secret was found"
            : secret.Contains("CHANGE-ME", StringComparison.OrdinalIgnoreCase)
                ? "the value is the appsettings.json placeholder (the real secret was not set)"
                : $"the value is only {secret.Length} characters long, and it needs at least 32";

        throw new InvalidOperationException(
            $"Missing JWT secret for production: {reason}. {DescribeSecretSources()} " +
            "Set TokenSettings__Secret (or JWT_SECRET) in your host's environment variables to a " +
            "random string of at least 32 characters, then redeploy. " +
            "Generate one with: openssl rand -base64 48");
    }

    public static bool IsPlatformDeployment() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORT"));

    private static string DescribeSecretSources()
    {
        var found = JwtSecretKeys
            .Select(key =>
            {
                var value = Environment.GetEnvironmentVariable(key);
                return string.IsNullOrWhiteSpace(value)
                    ? $"{key}=not set"
                    : $"{key}=set ({value.Length} chars)";
            });

        return $"Environment: {string.Join(", ", found)}.";
    }

    private static readonly string[] JwtSecretKeys =
    [
        "TokenSettings__Secret",
        "JWT_SECRET",
        "TOKEN_SETTINGS_SECRET",
    ];

    private static string? TryBuildFromPlatformEnv()
    {
        var databaseUrl = FirstEnv("DATABASE_URL", "POSTGRES_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl) &&
            databaseUrl.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePostgresUrl(databaseUrl);
        }

        var host = FirstEnv("PGHOST", "PG_HOST");
        var port = FirstEnv("PGPORT", "PG_PORT") ?? "5432";
        var user = FirstEnv("PGUSER", "PG_USER");
        var password = FirstEnv("PGPASSWORD", "PG_PASSWORD");
        var database = FirstEnv("PGDATABASE", "PG_DB");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        return BuildConnectionString(host, port, user, password, database, null);
    }

    private static bool IsLoopback(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = part[..separator].Trim();
            if (!key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("Server", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("server", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("data source", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = Unquote(part[(separator + 1)..].Trim());
            if (value.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("127.0.0.1", StringComparison.Ordinal) ||
                value.Equals("::1", StringComparison.Ordinal) ||
                value.Equals("0.0.0.0", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string ParsePostgresUrl(string url)
    {
        var normalized = url;
        if (normalized.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            normalized = "http://" + normalized["postgresql://".Length..];
        else if (normalized.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            normalized = "http://" + normalized["postgres://".Length..];

        Uri uri;
        try
        {
            uri = new Uri(normalized);
        }
        catch (UriFormatException ex)
        {
            throw new InvalidOperationException(
                "DATABASE_URL could not be parsed as a PostgreSQL connection string. " +
                "It must look like postgresql://user:password@host:5432/database, with the " +
                "password percent-encoded if it contains '@', '/', or ':'.", ex);
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.Trim('/');

        // The scheme was rewritten to http:// so Uri.Port falls back to 80 when the
        // URL omits its port. PostgreSQL connection strings commonly omit it, so
        // detect the omission from the authority instead of trusting uri.Port.
        var host = uri.Host;
        var port = HasExplicitPort(uri.Authority) ? uri.Port : 5432;
        var sslMode = QueryValue(uri.Query, "sslmode");

        return BuildConnectionString(host, port.ToString(), user, password, database, sslMode);
    }

    private static bool HasExplicitPort(string authority)
    {
        if (string.IsNullOrEmpty(authority))
            return false;

        if (authority.StartsWith('['))
        {
            var closingBracket = authority.IndexOf(']');
            return closingBracket >= 0 &&
                   closingBracket + 1 < authority.Length &&
                   authority[closingBracket + 1] == ':';
        }

        return authority.Contains(':');
    }

    private static string BuildConnectionString(
        string host,
        string port,
        string user,
        string password,
        string database,
        string? sslMode)
    {
        var parts = new List<string>
        {
            $"Host={Quote(host)}",
            $"Port={Quote(port)}",
            $"Database={Quote(database)}",
            $"Username={Quote(user)}",
            $"Password={Quote(password)}",
        };

        var ssl = MapSslMode(sslMode);
        if (ssl is not null)
            parts.Add($"SSL Mode={ssl}");

        return string.Join(";", parts);
    }

    private static string? MapSslMode(string? sslMode)
    {
        if (string.IsNullOrWhiteSpace(sslMode))
            return null;

        return sslMode.ToLowerInvariant() switch
        {
            "require" or "verify-ca" or "verify-full" => "Require",
            "prefer" => "Prefer",
            "allow" => "Allow",
            "disable" => "Disable",
            _ => null,
        };
    }

    private static string? QueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2)
                continue;

            if (parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(parts[1]);
        }

        return null;
    }

    private static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "''";

        if (value.Contains('\''))
            return "'" + value.Replace("'", "''") + "'";

        if (value.Any(c => c is ';' or '=' or ' ' or '"'))
            return "'" + value + "'";

        return value;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
            return value[1..^1].Replace("''", "'");

        return value;
    }

    private static string? FirstEnv(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
