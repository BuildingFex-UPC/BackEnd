
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
                "in your host's environment variables, or provide the platform MySQL variables.");

        if (!environment.IsDevelopment() && IsLoopback(configured))
        {
            throw new InvalidOperationException(
                "The configured database connection points at localhost, which cannot work once the API " +
                "runs in a container or on a remote host. Set ConnectionStrings__DefaultConnection in your " +
                "host's environment variables to the public connection string of your MySQL database.");
        }

        return configured;
    }

    public static void ApplySecretsFromEnvironment(WebApplicationBuilder builder)
    {
        var secret = FirstEnv(
            "TokenSettings__Secret",
            "JWT_SECRET",
            "TOKEN_SETTINGS_SECRET");

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
            "MYSQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(connectionString))
            builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }

    public static void ValidateProductionSecrets(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        var secret = configuration["TokenSettings:Secret"];
        if (string.IsNullOrWhiteSpace(secret) ||
            secret.Contains("CHANGE-ME", StringComparison.OrdinalIgnoreCase) ||
            secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Missing JWT secret. In your host's environment variables, add TokenSettings__Secret " +
                "(or JWT_SECRET) with a random string of at least 32 characters, then redeploy. " +
                "You can generate one with: openssl rand -base64 48");
        }
    }

    public static bool IsPlatformDeployment() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORT"));

    private static string? TryBuildFromPlatformEnv()
    {
        var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(mysqlUrl) && mysqlUrl.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
            return ParseMySqlUrl(mysqlUrl);

        var host = FirstEnv("MYSQLHOST", "MYSQL_HOST");
        var port = FirstEnv("MYSQLPORT", "MYSQL_PORT") ?? "3306";
        var user = FirstEnv("MYSQLUSER", "MYSQL_USER");
        var password = FirstEnv("MYSQLPASSWORD", "MYSQL_PASSWORD", "MYSQL_ROOT_PASSWORD");
        var database = FirstEnv("MYSQLDATABASE", "MYSQL_DATABASE");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        return $"server={host};port={port};user={user};password={password};database={database}";
    }

    private static bool IsLoopback(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = part[..separator].Trim();
            if (!key.Equals("server", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("host", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("data source", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = part[(separator + 1)..].Trim().Trim('"', '\'');
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

    private static string ParseMySqlUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 3306;

        return $"server={host};port={port};user={user};password={password};database={database}";
    }
}
