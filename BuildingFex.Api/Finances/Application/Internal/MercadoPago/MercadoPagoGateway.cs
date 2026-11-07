using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.Extensions.Options;

namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public class MercadoPagoGateway(
    IOptions<MercadoPagoSettings> settings,
    ILogger<MercadoPagoGateway> logger) : IMercadoPagoGateway
{
    private readonly MercadoPagoSettings _settings = settings.Value;

    private void EnsureSdkConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken) ||
            _settings.AccessToken.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Mercado Pago no está configurado con un AccessToken válido.");
        }

        if (!string.Equals(MercadoPagoConfig.AccessToken, _settings.AccessToken, StringComparison.Ordinal))
        {
            MercadoPagoConfig.AccessToken = _settings.AccessToken;
        }
    }

    public async Task<PreferenceResult> CreatePreferenceAsync(PreferenceRequest request, CancellationToken ct = default)
    {
        EnsureSdkConfigured();

        try
        {
            var client = new PreferenceClient();
            var preference = await client.CreateAsync(request, cancellationToken: ct);

            var isSandbox = _settings.UseSandbox ||
                            _settings.AccessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);

            var initPoint = isSandbox
                ? (preference.SandboxInitPoint ?? preference.InitPoint)
                : (preference.InitPoint ?? preference.SandboxInitPoint);

            logger.LogInformation(
                "MercadoPago preference created: {PreferenceId} (isSandbox={IsSandbox})",
                preference.Id, isSandbox);

            return new PreferenceResult(preference.Id!, initPoint);
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            logger.LogError(ex, "Error creating preference in Mercado Pago SDK: {Detail}", detail);
            throw new InvalidOperationException($"Error en Mercado Pago al crear preferencia: {detail}", ex);
        }
    }

    public async Task<MercadoPagoPaymentData> GetPaymentAsync(long paymentId, CancellationToken ct = default)
    {
        EnsureSdkConfigured();

        try
        {
            var client = new PaymentClient();
            var payment = await client.GetAsync(paymentId, cancellationToken: ct);

            return new MercadoPagoPaymentData(
                payment.Id ?? paymentId,
                payment.Status ?? string.Empty,
                payment.StatusDetail,
                payment.CurrencyId ?? "PEN",
                payment.TransactionAmount ?? 0m,
                payment.ExternalReference,
                payment.DateApproved,
                payment.PaymentMethodId);
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            logger.LogError(ex, "Error fetching payment {PaymentId} from Mercado Pago SDK: {Detail}", paymentId, detail);
            throw new InvalidOperationException($"Error consultando pago en Mercado Pago: {detail}", ex);
        }
    }

    public async Task<MercadoPagoPaymentData> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken ct = default)
    {
        EnsureSdkConfigured();

        try
        {
            var client = new PaymentClient();
            var payment = await client.CreateAsync(request, cancellationToken: ct);

            return new MercadoPagoPaymentData(
                payment.Id ?? 0,
                payment.Status ?? string.Empty,
                payment.StatusDetail,
                payment.CurrencyId ?? "PEN",
                payment.TransactionAmount ?? 0m,
                payment.ExternalReference,
                payment.DateApproved,
                payment.PaymentMethodId);
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            logger.LogError(ex, "Error creating card payment in Mercado Pago SDK: {Detail}", detail);
            throw new InvalidOperationException($"Error procesando cobro en Mercado Pago: {detail}", ex);
        }
    }
}

