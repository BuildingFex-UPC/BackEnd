using System.Text.Json;
using BuildingFex.Api.Finances.Application.Internal.MercadoPago;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BuildingFex.Api.Finances.Interfaces.Rest;

[ApiController]
[Route("api/v1/payments")]
[Tags("Mercado Pago")]
public class MercadoPagoController(
    IMercadoPagoService mercadoPagoService,
    IOptions<MercadoPagoSettings> mpSettings,
    ILogger<MercadoPagoController> logger) : ControllerBase
{
    private readonly MercadoPagoSettings _settings = mpSettings.Value;

    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetConfig()
    {
        var configured = !string.IsNullOrWhiteSpace(_settings.AccessToken) &&
                         !_settings.AccessToken.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);

        return Ok(new
        {
            publicKey = string.IsNullOrWhiteSpace(_settings.PublicKey) ? null : _settings.PublicKey,
            configured,
            country = "PE",
            currency = "PEN",
        });
    }
    [HttpPost("preference")]
    [Authorize]
    public async Task<IActionResult> CreatePreference(
        [FromBody] JsonElement body, CancellationToken ct)
    {
        if (!body.TryGetProperty("receiptId", out var receiptIdProp))
            return BadRequest(new { code = "RECEIPT_ID_REQUIRED", message = "receiptId is required." });

        var receiptId = receiptIdProp.ToString();

        var ownerAdminId = body.TryGetProperty("ownerAdminId", out var ownerProp)
            ? ownerProp.GetString() ?? string.Empty
            : string.Empty;

        try
        {
            var result = await mercadoPagoService.CreatePreferenceAsync(
                new CreatePreferenceRequest(receiptId, ownerAdminId), ct);

            return Ok(new
            {
                preferenceId = result.PreferenceId,
                initPoint = result.InitPoint,
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { code = "RECEIPT_NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = "MP_NOT_CONFIGURED", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating MercadoPago preference for receipt {ReceiptId}", receiptId);
            return StatusCode(500, new { code = "MP_ERROR", message = "Could not create payment preference." });
        }
    }

    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> CreateMaintenanceCheckout(
        [FromBody] JsonElement body, CancellationToken ct)
    {
        if (!body.TryGetProperty("residentId", out var residentProp))
            return BadRequest(new { code = "RESIDENT_ID_REQUIRED", message = "residentId is required." });

        var residentId = residentProp.GetString() ?? string.Empty;
        var ownerAdminId = body.TryGetProperty("ownerAdminId", out var ownerProp)
            ? ownerProp.GetString() ?? string.Empty
            : string.Empty;

        var payerEmail = body.TryGetProperty("payerEmail", out var emailProp)
            ? emailProp.GetString()
            : null;

        var frontendBaseUrl = body.TryGetProperty("frontendBaseUrl", out var frontendProp)
            ? frontendProp.GetString()
            : Request.Headers.Origin.FirstOrDefault();

        try
        {
            var result = await mercadoPagoService.CreateMaintenanceCheckoutPreferenceAsync(
                new CreateMaintenanceCheckoutRequest(
                    residentId,
                    ownerAdminId,
                    payerEmail,
                    frontendBaseUrl),
                ct);

            var demo = string.Equals(result.PreferenceId, "DEMO", StringComparison.OrdinalIgnoreCase);
            return Ok(new
            {
                preferenceId = result.PreferenceId,
                initPoint = result.InitPoint,
                demo,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = "CHECKOUT_ERROR", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Maintenance checkout failed for resident {ResidentId}", residentId);
            var detail = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { code = "CHECKOUT_ERROR", message = detail });
        }
    }

    [HttpPost("confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmMaintenancePayment(
        [FromBody] JsonElement body, CancellationToken ct)
    {
        if (!body.TryGetProperty("residentId", out var residentProp))
            return BadRequest(new { code = "RESIDENT_ID_REQUIRED", message = "residentId is required." });

        var residentId = residentProp.GetString() ?? string.Empty;
        var ownerAdminId = body.TryGetProperty("ownerAdminId", out var ownerProp)
            ? ownerProp.GetString() ?? string.Empty
            : string.Empty;

        long? paymentId = null;
        if (body.TryGetProperty("paymentId", out var paymentProp))
        {
            if (paymentProp.ValueKind == JsonValueKind.Number)
                paymentId = paymentProp.GetInt64();
            else if (long.TryParse(paymentProp.GetString(), out var parsed))
                paymentId = parsed;
        }

        var demo = body.TryGetProperty("demo", out var demoProp) && demoProp.GetBoolean();

        try
        {
            var result = await mercadoPagoService.ConfirmMaintenancePaymentAsync(
                new ConfirmMaintenancePaymentRequest(residentId, ownerAdminId, paymentId, demo),
                ct);

            return Ok(new
            {
                reconciled = result.Reconciled,
                itemsPaid = result.ItemsPaid,
                paidAt = result.PaidAt,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = "CONFIRM_FAILED", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Maintenance confirm failed for resident {ResidentId}", residentId);
            return BadRequest(new { code = "CONFIRM_FAILED", message = ex.Message });
        }
    }

    [HttpPost("process")]
    [Authorize]
    public async Task<IActionResult> ProcessCardPayment(
        [FromBody] JsonElement body, CancellationToken ct)
    {
        if (!body.TryGetProperty("token", out var tokenProp) ||
            !body.TryGetProperty("transaction_amount", out var amountProp))
        {
            return BadRequest(new
            {
                code = "INVALID_PAYLOAD",
                message = "token and transaction_amount are required."
            });
        }

        var receiptId = body.TryGetProperty("receiptId", out var rProp)
            ? rProp.ToString()
            : body.TryGetProperty("external_reference", out var refProp)
                ? refProp.GetString() ?? string.Empty
                : string.Empty;

        var ownerAdminId = body.TryGetProperty("ownerAdminId", out var ownerProp)
            ? ownerProp.GetString() ?? string.Empty
            : string.Empty;

        var request = new CardPaymentRequest(
            Token: tokenProp.GetString() ?? string.Empty,
            PaymentMethodId: body.TryGetProperty("payment_method_id", out var pmProp)
                ? pmProp.GetString() ?? "visa" : "visa",
            TransactionAmount: amountProp.GetDecimal(),
            Installments: body.TryGetProperty("installments", out var instProp)
                ? instProp.GetInt32() : 1,
            PayerEmail: body.TryGetProperty("payer", out var payerProp) &&
                        payerProp.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString() ?? "guest@buildingfex.com"
                : "guest@buildingfex.com",
            ReceiptExternalId: receiptId,
            OwnerAdminExternalId: ownerAdminId,
            IssuerId: body.TryGetProperty("issuer_id", out var issProp)
                ? issProp.ToString() : null);

        try
        {
            var result = await mercadoPagoService.ProcessCardPaymentAsync(request, ct);

            return Ok(new
            {
                id = result.PaymentId,
                status = result.Status,
                status_detail = result.StatusDetail,
                transaction_amount = result.TransactionAmount,
                payment_method_id = result.PaymentMethodId,
                date_approved = result.DateApproved,
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { code = "RECEIPT_NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = "MP_PAYMENT_FAILED", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing card payment for receipt {ReceiptId}", receiptId);
            var detail = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { code = "MP_ERROR", message = detail });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] JsonElement body, CancellationToken ct)
    {
        logger.LogInformation("MercadoPago webhook received: {Body}", body.GetRawText());

        long mpPaymentId = 0;
        string? dataId = null;

        if (body.TryGetProperty("data", out var dataProp) &&
            dataProp.TryGetProperty("id", out var dataIdProp))
        {
            dataId = dataIdProp.ToString();
            mpPaymentId = dataIdProp.ValueKind == JsonValueKind.Number
                ? dataIdProp.GetInt64()
                : long.TryParse(dataIdProp.GetString(), out var parsed) ? parsed : 0;
        }
        else if (body.TryGetProperty("id", out var idProp) &&
                 body.TryGetProperty("topic", out var topicProp) &&
                 topicProp.GetString() == "payment")
        {
            dataId = idProp.ToString();
            mpPaymentId = idProp.ValueKind == JsonValueKind.Number
                ? idProp.GetInt64()
                : long.TryParse(idProp.GetString(), out var parsed) ? parsed : 0;
        }

        if (mpPaymentId == 0)
        {
            var qId = HttpContext.Request.Query["id"].FirstOrDefault();
            var qTopic = HttpContext.Request.Query["topic"].FirstOrDefault();
            if (qTopic == "payment" && long.TryParse(qId, out var qParsed))
            {
                mpPaymentId = qParsed;
                dataId = qId;
            }
        }

        if (mpPaymentId == 0)
        {
            return Ok(new { status = "ignored" });
        }

        var signature = Request.Headers["x-signature"].FirstOrDefault();
        var requestId = Request.Headers["x-request-id"].FirstOrDefault();

        if (!MercadoPagoWebhookValidator.TryValidateSignature(
                signature, requestId, dataId, _settings.WebhookSecret, out var sigFailure))
        {
            logger.LogWarning("MercadoPago webhook signature rejected: {Reason}", sigFailure);
            return Unauthorized(new { processed = false, status = sigFailure });
        }

        try
        {
            var result = await mercadoPagoService.HandleWebhookAsync(mpPaymentId, ct);
            return Ok(new { processed = result.Processed, status = result.Status });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook for MP payment {MpId}", mpPaymentId);
            return Ok(new { processed = false, status = "internal_error" });
        }
    }
}
