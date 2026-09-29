namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public record CreatePreferenceRequest(string ReceiptExternalId, string OwnerAdminExternalId);

public record CreateMaintenanceCheckoutRequest(
    string ResidentExternalId,
    string OwnerAdminExternalId,
    string? PayerEmail = null,
    string? FrontendBaseUrl = null);

public record ConfirmMaintenancePaymentRequest(
    string ResidentExternalId,
    string OwnerAdminExternalId,
    long? PaymentId,
    bool AllowDemo = false);

public record MaintenancePaymentResult(bool Reconciled, int ItemsPaid, string? PaidAt);

public record CreateSubscriptionPreferenceRequest(
    string AdminExternalId,
    string PlanId,
    string? PayerEmail = null,
    string? FrontendBaseUrl = null);

public record SubscriptionActivationResult(bool Activated, string PlanId, string? PaidUntil);

public record PreferenceResult(string PreferenceId, string? InitPoint);

public record CardPaymentRequest(
    string Token,
    string PaymentMethodId,
    decimal TransactionAmount,
    int Installments,
    string PayerEmail,
    string ReceiptExternalId,
    string OwnerAdminExternalId,
    string? IssuerId = null);

public record CardPaymentResult(
    string PaymentId,
    string Status,
    string StatusDetail,
    decimal TransactionAmount,
    string? PaymentMethodId,
    string? DateApproved);

public record WebhookResult(bool Processed, string Status);
