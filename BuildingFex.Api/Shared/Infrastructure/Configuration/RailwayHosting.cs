
namespace BuildingFex.Api.Shared.Infrastructure.Configuration;

public static class RailwayHosting
{
    public static void ConfigureKestrelPort(WebApplicationBuilder builder)
    {
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
            builder.WebHost.UseUrls($"http://+:{port}");
    }

    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var railwayConnection = TryBuildFromRailwayEnv();
        if (railwayConnection is not null)
            return railwayConnection;

        var configured = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        throw new InvalidOperationException(
            "Database connection is not configured. Set ConnectionStrings__DefaultConnection or Railway MySQL variables.");
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
                "Missing JWT secret for production. In Railway → your API service → Variables, add " +
                "TokenSettings__Secret (or JWT_SECRET) with a random string of at least 32 characters, then redeploy.");
        }
    }

    public static bool IsRailwayDeployment() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT"))
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORT"));

    private static string? TryBuildFromRailwayEnv()
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
