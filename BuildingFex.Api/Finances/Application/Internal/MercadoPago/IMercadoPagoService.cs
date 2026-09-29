namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public interface IMercadoPagoService
{
    Task<PreferenceResult> CreatePreferenceAsync(CreatePreferenceRequest request, CancellationToken ct = default);
    Task<PreferenceResult> CreateMaintenanceCheckoutPreferenceAsync(
        CreateMaintenanceCheckoutRequest request,
        CancellationToken ct = default);
    Task<MaintenancePaymentResult> ConfirmMaintenancePaymentAsync(
        ConfirmMaintenancePaymentRequest request,
        CancellationToken ct = default);
    Task<PreferenceResult> CreateSubscriptionPreferenceAsync(
        CreateSubscriptionPreferenceRequest request,
        CancellationToken ct = default);
    Task<SubscriptionActivationResult> ConfirmSubscriptionPaymentAsync(
        string adminExternalId,
        string planId,
        long? mpPaymentId,
        bool allowDemo,
        CancellationToken ct = default);
    Task<CardPaymentResult> ProcessCardPaymentAsync(CardPaymentRequest request, CancellationToken ct = default);
    Task<WebhookResult> HandleWebhookAsync(long mpPaymentId, CancellationToken ct = default);
}
