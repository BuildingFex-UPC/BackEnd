namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public class MercadoPagoSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = "https://buildingfex.web.app";
    public string NotificationUrl { get; set; } = string.Empty;

    /// <summary>
    /// When true (default), preference URLs will return the Mercado Pago SandboxInitPoint for test transactions.
    /// Also automatically true if AccessToken begins with "TEST-".
    /// </summary>
    public bool UseSandbox { get; set; } = true;
}
