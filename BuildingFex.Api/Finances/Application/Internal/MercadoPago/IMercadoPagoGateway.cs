using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;

namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public record MercadoPagoPaymentData(
    long Id,
    string Status,
    string? StatusDetail,
    string CurrencyId,
    decimal TransactionAmount,
    string? ExternalReference,
    DateTime? DateApproved,
    string? PaymentMethodId);

public interface IMercadoPagoGateway
{
    Task<PreferenceResult> CreatePreferenceAsync(PreferenceRequest request, CancellationToken ct = default);
    Task<MercadoPagoPaymentData> GetPaymentAsync(long paymentId, CancellationToken ct = default);
    Task<MercadoPagoPaymentData> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken ct = default);
}

