using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using Microsoft.Extensions.Options;

namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public class MercadoPagoService(
    IOptions<MercadoPagoSettings> settings,
    IMercadoPagoGateway gateway,
    IReceiptRepository receiptRepository,
    IFeeRepository feeRepository,
    IPaymentRepository paymentRepository,
    IUserRepository userRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork,
    ILogger<MercadoPagoService> logger) : IMercadoPagoService
{
    private readonly MercadoPagoSettings _settings = settings.Value;

    public static string BuildSubscriptionExternalReference(string adminExternalId, string planId) =>
        $"SUBSCRIPTION:{adminExternalId}:{SubscriptionPlans.Normalize(planId)}";

    public static string BuildMaintenanceExternalReference(string residentExternalId, string ownerAdminExternalId) =>
        $"MAINTENANCE:{residentExternalId}:{ownerAdminExternalId}";

    private bool IsMercadoPagoConfigured() =>
        !string.IsNullOrWhiteSpace(_settings.AccessToken) &&
        !_settings.AccessToken.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidMercadoPagoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url.TrimEnd('/'), UriKind.Absolute, out var uri)) return false;
        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) return false;
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return false;
        if (uri.Host.Equals("127.0.0.1")) return false;
        return true;
    }

    private string ResolveFrontendBaseForCheckout(string? requestOverride)
    {
        var fromRequest = string.IsNullOrWhiteSpace(requestOverride)
            ? null
            : requestOverride.TrimEnd('/');

        if (fromRequest != null && IsValidMercadoPagoUrl(fromRequest))
            return fromRequest;

        var fromSettings = _settings.FrontendBaseUrl?.TrimEnd('/');
        if (IsValidMercadoPagoUrl(fromSettings))
            return fromSettings!;

        // Fallback HTTPS return URL suitable for mobile WebView interception
        return "https://buildingfex.web.app";
    }

    private string? ResolveNotificationUrl()
    {
        var url = _settings.NotificationUrl?.Trim();
        return IsValidMercadoPagoUrl(url) ? url : null;
    }

    private static PreferenceBackUrlsRequest BuildBackUrls(string frontendBase, string successPath)
    {
        var baseUrl = frontendBase.TrimEnd('/');
        return new PreferenceBackUrlsRequest
        {
            Success = $"{baseUrl}{successPath}",
            Failure = $"{baseUrl}{successPath.Replace("success", "failure", StringComparison.Ordinal)}",
            Pending = $"{baseUrl}{successPath.Replace("success", "pending", StringComparison.Ordinal)}",
        };
    }

    public async Task<PreferenceResult> CreatePreferenceAsync(
        CreatePreferenceRequest request, CancellationToken ct = default)
    {
        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Agrega AccessToken en las variables de entorno.");

        var receipt = await receiptRepository.FindByExternalIdAsync(request.ReceiptExternalId, ct)
            ?? throw new KeyNotFoundException($"Receipt '{request.ReceiptExternalId}' not found.");

        var totalAmount = receipt.Amount + receipt.LateFee + receipt.ExtraCharges;
        var frontend = ResolveFrontendBaseForCheckout(null);

        var preferenceRequest = new PreferenceRequest
        {
            Items =
            [
                new PreferenceItemRequest
                {
                    Title = $"BuildingFex – Recibo {receipt.ExternalId}",
                    Description = receipt.Concept ?? "Pago de expensas",
                    Quantity = 1,
                    CurrencyId = "PEN",
                    UnitPrice = totalAmount,
                }
            ],
            ExternalReference = receipt.ExternalId,
            BackUrls = BuildBackUrls(frontend, "/app/resident/finance?payment=success"),
            AutoReturn = "approved",
            NotificationUrl = ResolveNotificationUrl(),
        };

        return await gateway.CreatePreferenceAsync(preferenceRequest, ct);
    }

    public async Task<PreferenceResult> CreateMaintenanceCheckoutPreferenceAsync(
        CreateMaintenanceCheckoutRequest request,
        CancellationToken ct = default)
    {
        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Configure MercadoPago__AccessToken.");

        var owner = await ownerResolver.ResolveOwnerAdminAsync(request.OwnerAdminExternalId, ct)
            ?? throw new InvalidOperationException("Valid ownerAdminId is required.");

        var pendingReceipts = (await receiptRepository.ListAsync(
                request.OwnerAdminExternalId, request.ResidentExternalId, ct))
            .Where(r => r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pendingFees = (await feeRepository.ListAsync(
                request.OwnerAdminExternalId, request.ResidentExternalId, ct))
            .Where(f => f.Status.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (pendingReceipts.Count == 0 && pendingFees.Count == 0)
            throw new InvalidOperationException("No hay cuotas pendientes para pagar.");

        var totalAmount = pendingReceipts.Sum(r => r.Amount + r.LateFee + r.ExtraCharges)
            + pendingFees.Sum(f => f.Amount);

        if (totalAmount <= 0)
            throw new InvalidOperationException("El importe total de las cuotas pendientes debe ser mayor a 0.");

        var frontend = ResolveFrontendBaseForCheckout(request.FrontendBaseUrl);
        var itemCount = pendingReceipts.Count + pendingFees.Count;
        var externalRef = BuildMaintenanceExternalReference(
            request.ResidentExternalId, request.OwnerAdminExternalId);

        var preferenceRequest = new PreferenceRequest
        {
            Items =
            [
                new PreferenceItemRequest
                {
                    Title = "BuildingFex – Cuotas de mantenimiento",
                    Description = $"{itemCount} cuota(s) pendiente(s)",
                    Quantity = 1,
                    CurrencyId = "PEN",
                    UnitPrice = totalAmount,
                }
            ],
            ExternalReference = externalRef,
            Payer = string.IsNullOrWhiteSpace(request.PayerEmail)
                ? null
                : new PreferencePayerRequest { Email = request.PayerEmail },
            BackUrls = BuildBackUrls(frontend, "/app/resident/finance?payment=success"),
            AutoReturn = "approved",
            NotificationUrl = ResolveNotificationUrl(),
        };

        return await gateway.CreatePreferenceAsync(preferenceRequest, ct);
    }

    public async Task<MaintenancePaymentResult> ConfirmMaintenancePaymentAsync(
        ConfirmMaintenancePaymentRequest request,
        CancellationToken ct = default)
    {
        if (request.AllowDemo)
            throw new InvalidOperationException("La conciliación demo no está permitida. Se requiere una transacción de prueba verificada por Mercado Pago.");

        if (!request.PaymentId.HasValue || request.PaymentId.Value <= 0)
            throw new InvalidOperationException("paymentId is required to confirm maintenance payment.");

        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Configure MercadoPago__AccessToken.");

        var owner = await ownerResolver.ResolveOwnerAdminAsync(request.OwnerAdminExternalId, ct)
            ?? throw new InvalidOperationException("Valid ownerAdminId is required.");

        var expectedRef = BuildMaintenanceExternalReference(
            request.ResidentExternalId, request.OwnerAdminExternalId);

        // Fetch payment directly from Mercado Pago
        var mpPayment = await gateway.GetPaymentAsync(request.PaymentId.Value, ct);

        // 1. Check approval status
        if (!string.Equals(mpPayment.Status, "approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"El pago de Mercado Pago no está aprobado (estado actual: {mpPayment.Status}).");

        // 2. Check currency
        if (!string.Equals(mpPayment.CurrencyId, "PEN", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"La moneda del pago ({mpPayment.CurrencyId}) no es válida. Se requiere PEN.");

        // 3. Check external reference
        if (!string.Equals(mpPayment.ExternalReference, expectedRef, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La referencia del pago no coincide con el residente o administrador.");

        var mpPaymentIdStr = request.PaymentId.Value.ToString();
        var dateApprovedStr = mpPayment.DateApproved?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o");

        // 4. Check idempotency: Was this transaction already reconciled?
        var existingPayments = (await paymentRepository.ListAsync(
                request.OwnerAdminExternalId, request.ResidentExternalId, ct))
            .Where(p => (p.Reference != null && p.Reference.Equals($"MP-{mpPaymentIdStr}", StringComparison.OrdinalIgnoreCase)) ||
                        p.ExternalId.StartsWith($"MP-{mpPaymentIdStr}-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (existingPayments.Count > 0)
        {
            logger.LogInformation(
                "Maintenance payment MP-{MpId} already reconciled for resident {ResidentId} ({Count} records).",
                mpPaymentIdStr, request.ResidentExternalId, existingPayments.Count);

            return new MaintenancePaymentResult(true, existingPayments.Count, dateApprovedStr);
        }

        // 5. Check expected amount against pending debts
        var pendingReceipts = (await receiptRepository.ListAsync(
                request.OwnerAdminExternalId, request.ResidentExternalId, ct))
            .Where(r => r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pendingFees = (await feeRepository.ListAsync(
                request.OwnerAdminExternalId, request.ResidentExternalId, ct))
            .Where(f => f.Status.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var expectedAmount = pendingReceipts.Sum(r => r.Amount + r.LateFee + r.ExtraCharges)
            + pendingFees.Sum(f => f.Amount);

        if (pendingReceipts.Count == 0 && pendingFees.Count == 0)
            throw new InvalidOperationException("No se encontraron cuotas pendientes para conciliar.");

        if (mpPayment.TransactionAmount < expectedAmount)
            throw new InvalidOperationException(
                $"El importe cobrado en Mercado Pago (S/ {mpPayment.TransactionAmount}) es menor al total adeudado (S/ {expectedAmount}).");

        // 6. Reconcile maintenance items
        var itemsPaid = await ReconcileMaintenanceAsync(
            request.ResidentExternalId,
            request.OwnerAdminExternalId,
            owner.Id,
            mpPaymentIdStr,
            dateApprovedStr,
            ct);

        return new MaintenancePaymentResult(true, itemsPaid, dateApprovedStr);
    }

    public async Task<PreferenceResult> CreateSubscriptionPreferenceAsync(
        CreateSubscriptionPreferenceRequest request,
        CancellationToken ct = default)
    {
        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Configure MercadoPago__AccessToken.");

        var planId = SubscriptionPlans.Normalize(request.PlanId);
        if (!SubscriptionPlans.IsPaid(planId))
            throw new InvalidOperationException("Only paid plans require checkout.");

        var amount = SubscriptionPlans.MonthlyPricePen(planId);
        var admin = await userRepository.FindByExternalIdAsync(request.AdminExternalId, ct)
            ?? throw new KeyNotFoundException($"Admin '{request.AdminExternalId}' not found.");

        var frontend = ResolveFrontendBaseForCheckout(request.FrontendBaseUrl);
        var externalRef = BuildSubscriptionExternalReference(admin.ExternalId, planId);
        var planLabel = char.ToUpper(planId[0]) + planId[1..];

        var preferenceRequest = new PreferenceRequest
        {
            Items =
            [
                new PreferenceItemRequest
                {
                    Title = $"BuildingFex – Plan {planLabel}",
                    Description = $"Suscripción mensual ({planId})",
                    Quantity = 1,
                    CurrencyId = "PEN",
                    UnitPrice = amount,
                }
            ],
            ExternalReference = externalRef,
            Payer = string.IsNullOrWhiteSpace(request.PayerEmail)
                ? null
                : new PreferencePayerRequest { Email = request.PayerEmail },
            BackUrls = BuildBackUrls(frontend, $"/app/settings?subscription=success&plan={planId}"),
            AutoReturn = "approved",
            NotificationUrl = ResolveNotificationUrl(),
        };

        return await gateway.CreatePreferenceAsync(preferenceRequest, ct);
    }

    public async Task<SubscriptionActivationResult> ConfirmSubscriptionPaymentAsync(
        string adminExternalId,
        string planId,
        long? mpPaymentId,
        bool allowDemo,
        CancellationToken ct = default)
    {
        if (allowDemo)
            throw new InvalidOperationException("La activación demo de suscripciones no está permitida.");

        if (!mpPaymentId.HasValue || mpPaymentId.Value <= 0)
            throw new InvalidOperationException("paymentId is required to confirm a paid subscription.");

        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Configure MercadoPago__AccessToken.");

        var normalizedPlan = SubscriptionPlans.Normalize(planId);
        var admin = await userRepository.FindByExternalIdAsync(adminExternalId, ct)
            ?? throw new KeyNotFoundException($"Admin '{adminExternalId}' not found.");

        var mpPayment = await gateway.GetPaymentAsync(mpPaymentId.Value, ct);

        if (!string.Equals(mpPayment.Status, "approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"El pago no está aprobado en Mercado Pago (estado: {mpPayment.Status ?? "unknown"}).");

        if (!string.Equals(mpPayment.CurrencyId, "PEN", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"La moneda del pago ({mpPayment.CurrencyId}) no es válida. Se requiere PEN.");

        var expectedRef = BuildSubscriptionExternalReference(adminExternalId, normalizedPlan);
        if (!string.Equals(mpPayment.ExternalReference, expectedRef, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La referencia del pago no coincide con el plan de suscripción seleccionado.");

        var expectedAmount = SubscriptionPlans.MonthlyPricePen(normalizedPlan);
        if (mpPayment.TransactionAmount < expectedAmount)
            throw new InvalidOperationException($"El importe cobrado (S/ {mpPayment.TransactionAmount}) es menor al plan seleccionado (S/ {expectedAmount}).");

        await ActivateAdminSubscriptionAsync(admin, normalizedPlan, ct);

        return new SubscriptionActivationResult(
            true,
            normalizedPlan,
            admin.SubscriptionPaidUntil?.ToString("o"));
    }

    public async Task<CardPaymentResult> ProcessCardPaymentAsync(
        CardPaymentRequest request, CancellationToken ct = default)
    {
        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Agrega AccessToken en appsettings.Local.json.");

        var owner = await ownerResolver.ResolveOwnerAdminAsync(request.OwnerAdminExternalId, ct)
            ?? throw new InvalidOperationException("Valid ownerAdminId is required.");

        Receipt? receipt = null;
        if (!string.IsNullOrWhiteSpace(request.ReceiptExternalId))
        {
            receipt = await receiptRepository.FindByExternalIdAsync(request.ReceiptExternalId, ct);
        }

        var paymentRequest = new PaymentCreateRequest
        {
            Token = request.Token,
            TransactionAmount = request.TransactionAmount,
            PaymentMethodId = request.PaymentMethodId,
            Installments = request.Installments,
            Payer = new PaymentPayerRequest { Email = request.PayerEmail },
            ExternalReference = receipt?.ExternalId ?? "GENERAL",
            Description = receipt != null ? $"BuildingFex – Recibo {receipt.ExternalId}" : "BuildingFex – Pago de expensas",
            StatementDescriptor = "BUILDINGFEX",
        };

        if (!string.IsNullOrWhiteSpace(request.IssuerId))
            paymentRequest.IssuerId = request.IssuerId;

        var mpPayment = await gateway.CreatePaymentAsync(paymentRequest, ct);

        logger.LogInformation(
            "MercadoPago payment processed: {MpId} status={Status} for receipt {ReceiptId}",
            mpPayment.Id, mpPayment.Status, receipt?.ExternalId ?? "GENERAL");

        if (string.Equals(mpPayment.Status, "approved", StringComparison.OrdinalIgnoreCase) && receipt != null)
        {
            await ReconcilePaymentAsync(
                receipt, owner.Id, request.TransactionAmount,
                mpPayment.Id.ToString(),
                mpPayment.DateApproved?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
                ct);
        }

        return new CardPaymentResult(
            PaymentId: mpPayment.Id.ToString(),
            Status: mpPayment.Status,
            StatusDetail: mpPayment.StatusDetail ?? string.Empty,
            TransactionAmount: mpPayment.TransactionAmount,
            PaymentMethodId: mpPayment.PaymentMethodId,
            DateApproved: mpPayment.DateApproved?.ToString("o"));
    }

    public async Task<WebhookResult> HandleWebhookAsync(long mpPaymentId, CancellationToken ct = default)
    {
        if (!IsMercadoPagoConfigured())
        {
            logger.LogWarning("Webhook ignored: Mercado Pago no está configurado.");
            return new WebhookResult(false, "mp_not_configured");
        }

        MercadoPagoPaymentData mpPayment;
        try
        {
            mpPayment = await gateway.GetPaymentAsync(mpPaymentId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch MercadoPago payment {MpId}", mpPaymentId);
            return new WebhookResult(false, "fetch_error");
        }

        if (!string.Equals(mpPayment.Status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Webhook for MP payment {MpId}: status={Status}, skipping.", mpPaymentId, mpPayment.Status);
            return new WebhookResult(false, mpPayment.Status ?? "not_approved");
        }

        if (!string.Equals(mpPayment.CurrencyId, "PEN", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Webhook for MP payment {MpId}: invalid currency {Currency}.", mpPaymentId, mpPayment.CurrencyId);
            return new WebhookResult(false, "invalid_currency");
        }

        var externalRef = mpPayment.ExternalReference ?? string.Empty;
        if (externalRef.StartsWith("SUBSCRIPTION:", StringComparison.OrdinalIgnoreCase))
            return await HandleSubscriptionWebhookAsync(mpPayment, externalRef, ct);

        if (externalRef.StartsWith("MAINTENANCE:", StringComparison.OrdinalIgnoreCase))
            return await HandleMaintenanceWebhookAsync(mpPayment, externalRef, ct);

        if (string.IsNullOrWhiteSpace(externalRef))
        {
            logger.LogWarning("MercadoPago payment {MpId} has no external_reference.", mpPaymentId);
            return new WebhookResult(false, "no_external_reference");
        }

        var mpReferenceId = $"MP-{mpPaymentId}";
        var existing = await paymentRepository.FindByExternalIdAsync(mpReferenceId, ct);
        if (existing is not null)
        {
            logger.LogInformation("Webhook: payment {MpRef} already recorded, skipping.", mpReferenceId);
            return new WebhookResult(true, "already_processed");
        }

        var receipt = await receiptRepository.FindByExternalIdAsync(externalRef, ct);
        if (receipt is null)
        {
            logger.LogWarning("Webhook: receipt {ExtRef} not found for MP payment {MpId}.", externalRef, mpPaymentId);
            return new WebhookResult(false, "receipt_not_found");
        }

        await ReconcilePaymentAsync(
            receipt, receipt.OwnerAdminId,
            mpPayment.TransactionAmount > 0 ? mpPayment.TransactionAmount : receipt.Amount,
            mpPaymentId.ToString(),
            mpPayment.DateApproved?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
            ct);

        logger.LogInformation("Webhook: reconciled MP payment {MpId} → receipt {ReceiptId}.", mpPaymentId, externalRef);
        return new WebhookResult(true, "approved");
    }

    private async Task<WebhookResult> HandleSubscriptionWebhookAsync(
        MercadoPagoPaymentData mpPayment,
        string externalRef,
        CancellationToken ct)
    {
        var parts = externalRef.Split(':', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return new WebhookResult(false, "invalid_subscription_reference");

        var adminExternalId = parts[1];
        var planId = SubscriptionPlans.Normalize(parts[2]);

        var admin = await userRepository.FindByExternalIdAsync(adminExternalId, ct);
        if (admin is null)
        {
            logger.LogWarning("Webhook subscription: admin {AdminId} not found.", adminExternalId);
            return new WebhookResult(false, "admin_not_found");
        }

        var expectedAmount = SubscriptionPlans.MonthlyPricePen(planId);
        if (mpPayment.TransactionAmount < expectedAmount)
        {
            logger.LogWarning("Webhook subscription: payment {MpId} amount {Amount} < expected {Expected}",
                mpPayment.Id, mpPayment.TransactionAmount, expectedAmount);
            return new WebhookResult(false, "amount_mismatch");
        }

        await ActivateAdminSubscriptionAsync(admin, planId, ct);
        logger.LogInformation(
            "Webhook: activated subscription {PlanId} for admin {AdminId} via MP {MpId}.",
            planId, adminExternalId, mpPayment.Id);

        return new WebhookResult(true, "subscription_activated");
    }

    private async Task<WebhookResult> HandleMaintenanceWebhookAsync(
        MercadoPagoPaymentData mpPayment,
        string externalRef,
        CancellationToken ct)
    {
        var parts = externalRef.Split(':', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return new WebhookResult(false, "invalid_maintenance_reference");

        var residentExternalId = parts[1];
        var ownerAdminExternalId = parts[2];

        var owner = await ownerResolver.ResolveOwnerAdminAsync(ownerAdminExternalId, ct);
        if (owner is null)
            return new WebhookResult(false, "owner_not_found");

        var mpId = mpPayment.Id.ToString();

        // Idempotency check: was this payment already reconciled?
        var existingPayments = (await paymentRepository.ListAsync(ownerAdminExternalId, residentExternalId, ct))
            .Where(p => (p.Reference != null && p.Reference.Equals($"MP-{mpId}", StringComparison.OrdinalIgnoreCase)) ||
                        p.ExternalId.StartsWith($"MP-{mpId}-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (existingPayments.Count > 0)
        {
            logger.LogInformation("Webhook: maintenance MP-{MpId} already reconciled, skipping.", mpId);
            return new WebhookResult(true, "already_processed");
        }

        var itemsPaid = await ReconcileMaintenanceAsync(
            residentExternalId,
            ownerAdminExternalId,
            owner.Id,
            mpId,
            mpPayment.DateApproved?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
            ct);

        logger.LogInformation(
            "Webhook: reconciled maintenance MP {MpId} for resident {ResidentId} ({Count} items).",
            mpPayment.Id, residentExternalId, itemsPaid);

        return new WebhookResult(itemsPaid > 0, itemsPaid > 0 ? "approved" : "nothing_to_reconcile");
    }

    private async Task<int> ReconcileMaintenanceAsync(
        string residentExternalId,
        string ownerAdminExternalId,
        int ownerAdminId,
        string mpPaymentId,
        string paidAt,
        CancellationToken ct)
    {
        var pendingReceipts = (await receiptRepository.ListAsync(ownerAdminExternalId, residentExternalId, ct))
            .Where(r => r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pendingFees = (await feeRepository.ListAsync(ownerAdminExternalId, residentExternalId, ct))
            .Where(f => f.Status.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var itemsPaid = 0;

        foreach (var receipt in pendingReceipts)
        {
            receipt.MarkAsPaid();
            receiptRepository.Update(receipt);

            var paymentExternalId = $"MP-{mpPaymentId}-R-{receipt.ExternalId}";
            if (await paymentRepository.FindByExternalIdAsync(paymentExternalId, ct) is null)
            {
                var amount = receipt.Amount + receipt.LateFee + receipt.ExtraCharges;
                await paymentRepository.AddAsync(
                    Domain.Model.Aggregates.Payment.Create(
                        paymentExternalId,
                        ownerAdminId,
                        residentExternalId,
                        amount,
                        feeExternalId: null,
                        feeMonth: null,
                        paidAt: paidAt,
                        method: "Mercado Pago",
                        reference: $"MP-{mpPaymentId}"),
                    ct);
            }
            itemsPaid++;
        }

        foreach (var fee in pendingFees)
        {
            fee.UpdateStatus("Pagado");
            feeRepository.Update(fee);

            var paymentExternalId = $"MP-{mpPaymentId}-F-{fee.ExternalId}";
            if (await paymentRepository.FindByExternalIdAsync(paymentExternalId, ct) is null)
            {
                await paymentRepository.AddAsync(
                    Domain.Model.Aggregates.Payment.Create(
                        paymentExternalId,
                        ownerAdminId,
                        residentExternalId,
                        fee.Amount,
                        feeExternalId: fee.ExternalId,
                        feeMonth: fee.Month,
                        paidAt: paidAt,
                        method: "Mercado Pago",
                        reference: $"MP-{mpPaymentId}"),
                    ct);
            }
            itemsPaid++;
        }

        if (itemsPaid > 0)
            await unitOfWork.CompleteAsync(ct);

        return itemsPaid;
    }

    private async Task ActivateAdminSubscriptionAsync(
        BuildingFex.Api.Iam.Domain.Model.Aggregates.User admin,
        string planId,
        CancellationToken ct)
    {
        var paidUntil = SubscriptionPlans.IsPaid(planId)
            ? DateTimeOffset.UtcNow.AddDays(31)
            : (DateTimeOffset?)null;

        admin.UpdateSubscription(planId, paidUntil);
        userRepository.Update(admin);
        await unitOfWork.CompleteAsync(ct);
    }

    private async Task ReconcilePaymentAsync(
        Receipt receipt, int ownerAdminId, decimal amount,
        string mpPaymentId, string paidAt, CancellationToken ct)
    {
        receipt.MarkAsPaid();
        receiptRepository.Update(receipt);

        var paymentExternalId = $"MP-{mpPaymentId}";

        var existingPayment = await paymentRepository.FindByExternalIdAsync(paymentExternalId, ct);
        if (existingPayment is null)
        {
            var payment = Domain.Model.Aggregates.Payment.Create(
                paymentExternalId,
                ownerAdminId,
                receipt.ResidentExternalId,
                amount,
                feeExternalId: null,
                feeMonth: null,
                paidAt: paidAt,
                method: "Mercado Pago",
                reference: $"MP-{mpPaymentId}");

            await paymentRepository.AddAsync(payment, ct);
        }

        await unitOfWork.CompleteAsync(ct);
    }
}
