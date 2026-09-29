namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public class MercadoPagoSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    public string NotificationUrl { get; set; } = string.Empty;
}
