using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using Microsoft.Extensions.Options;

namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public class MercadoPagoService(
    IOptions<MercadoPagoSettings> settings,
    IReceiptRepository receiptRepository,
    IFeeRepository feeRepository,
    IPaymentRepository paymentRepository,
    IUserRepository userRepository,
    FinanceOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork,
    IHostEnvironment hostEnvironment,
    ILogger<MercadoPagoService> logger) : IMercadoPagoService
{
    private readonly MercadoPagoSettings _settings = settings.Value;

    public static string BuildSubscriptionExternalReference(string adminExternalId, string planId) =>
        $"SUBSCRIPTION:{adminExternalId}:{SubscriptionPlans.Normalize(planId)}";

    public static string BuildMaintenanceExternalReference(string residentExternalId, string ownerAdminExternalId) =>
        $"MAINTENANCE:{residentExternalId}:{ownerAdminExternalId}";

    private void EnsureSdkConfigured()
    {
        if (string.IsNullOrWhiteSpace(MercadoPagoConfig.AccessToken))
            MercadoPagoConfig.AccessToken = _settings.AccessToken;
    }

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

        var fromSettings = _settings.FrontendBaseUrl.TrimEnd('/');
        if (IsValidMercadoPagoUrl(fromSettings))
            return fromSettings;

        return fromRequest ?? fromSettings;
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

    private async Task<PreferenceResult> CreatePreferenceSafeAsync(
        PreferenceRequest request, string logContext, CancellationToken ct)
    {
        try
        {
            var client = new PreferenceClient();
            var preference = await client.CreateAsync(request, cancellationToken: ct);
            var initPoint = preference.InitPoint ?? preference.SandboxInitPoint;
            logger.LogInformation("MercadoPago preference created: {PreferenceId} ({Context})", preference.Id, logContext);
            return new PreferenceResult(preference.Id!, initPoint);
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            logger.LogError(ex, "MercadoPago preference failed ({Context}): {Detail}", logContext, detail);
            throw new InvalidOperationException($"Mercado Pago rechazó la preferencia: {detail}", ex);
        }
    }

    public async Task<PreferenceResult> CreatePreferenceAsync(
        CreatePreferenceRequest request, CancellationToken ct = default)
    {
        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Agrega AccessToken en appsettings.Local.json.");

        EnsureSdkConfigured();

        var receipt = await receiptRepository.FindByExternalIdAsync(request.ReceiptExternalId, ct)
            ?? throw new KeyNotFoundException($"Receipt '{request.ReceiptExternalId}' not found.");

        var totalAmount = receipt.Amount + receipt.LateFee + receipt.ExtraCharges;
        var frontend = ResolveFrontendBaseForCheckout(null);

        if (!IsValidMercadoPagoUrl(frontend))
            throw new InvalidOperationException(
                "MercadoPago__FrontendBaseUrl debe ser HTTPS público (ej. https://tu-app.vercel.app). localhost no es válido.");

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

        return await CreatePreferenceSafeAsync(
            preferenceRequest, $"receipt {receipt.ExternalId}", ct);
    }

    public async Task<PreferenceResult> CreateMaintenanceCheckoutPreferenceAsync(
        CreateMaintenanceCheckoutRequest request,
        CancellationToken ct = default)
    {
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

        if (!IsMercadoPagoConfigured())
        {
            logger.LogWarning("MercadoPago not configured – maintenance checkout unavailable for demo.");
            return new PreferenceResult("DEMO", null);
        }

        EnsureSdkConfigured();

        var frontend = ResolveFrontendBaseForCheckout(request.FrontendBaseUrl);
        if (!IsValidMercadoPagoUrl(frontend))
        {
            logger.LogWarning(
                "No HTTPS frontend URL for maintenance checkout (got '{Frontend}').",
                frontend);
            return new PreferenceResult("DEMO", null);
        }

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

        return await CreatePreferenceSafeAsync(
            preferenceRequest,
            $"maintenance resident {request.ResidentExternalId} ({itemCount} items)",
            ct);
    }

    public async Task<MaintenancePaymentResult> ConfirmMaintenancePaymentAsync(
        ConfirmMaintenancePaymentRequest request,
        CancellationToken ct = default)
    {
        var owner = await ownerResolver.ResolveOwnerAdminAsync(request.OwnerAdminExternalId, ct)
            ?? throw new InvalidOperationException("Valid ownerAdminId is required.");

        var expectedRef = BuildMaintenanceExternalReference(
            request.ResidentExternalId, request.OwnerAdminExternalId);

        if (request.AllowDemo)
        {
            var demoItems = await ReconcileMaintenanceAsync(
                request.ResidentExternalId,
                request.OwnerAdminExternalId,
                owner.Id,
                $"DEMO-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow.ToString("o"),
                ct);
            return new MaintenancePaymentResult(true, demoItems, DateTimeOffset.UtcNow.ToString("o"));
        }

        if (!request.PaymentId.HasValue || request.PaymentId.Value <= 0)
            throw new InvalidOperationException("paymentId is required to confirm maintenance payment.");

        EnsureSdkConfigured();

        var client = new PaymentClient();
        var mpPayment = await client.GetAsync(request.PaymentId.Value, cancellationToken: ct);

        if (!string.Equals(mpPayment.Status, "approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Payment status is {mpPayment.Status ?? "unknown"}.");

        if (!string.Equals(mpPayment.ExternalReference, expectedRef, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Payment does not match the resident maintenance checkout.");

        var itemsPaid = await ReconcileMaintenanceAsync(
            request.ResidentExternalId,
            request.OwnerAdminExternalId,
            owner.Id,
            request.PaymentId.Value.ToString(),
            mpPayment.DateApproved?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
            ct);

        return new MaintenancePaymentResult(true, itemsPaid, mpPayment.DateApproved?.ToString("o"));
    }

    public async Task<PreferenceResult> CreateSubscriptionPreferenceAsync(
        CreateSubscriptionPreferenceRequest request,
        CancellationToken ct = default)
    {
        var planId = SubscriptionPlans.Normalize(request.PlanId);
        if (!SubscriptionPlans.IsPaid(planId))
            throw new InvalidOperationException("Only paid plans require checkout.");

        var amount = SubscriptionPlans.MonthlyPricePen(planId);
        var admin = await userRepository.FindByExternalIdAsync(request.AdminExternalId, ct)
            ?? throw new KeyNotFoundException($"Admin '{request.AdminExternalId}' not found.");

        if (!IsMercadoPagoConfigured())
        {
            logger.LogWarning("MercadoPago not configured – subscription checkout unavailable for demo.");
            return new PreferenceResult("DEMO", null);
        }

        EnsureSdkConfigured();

        var frontend = ResolveFrontendBaseForCheckout(request.FrontendBaseUrl);
        if (!IsValidMercadoPagoUrl(frontend))
        {
            logger.LogWarning(
                "No HTTPS frontend URL for subscription checkout (got '{Frontend}'). " +
                "Set MercadoPago__FrontendBaseUrl=https://tu-app.vercel.app in Railway.",
                frontend);
            return new PreferenceResult("DEMO", null);
        }

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

        return await CreatePreferenceSafeAsync(
            preferenceRequest, $"subscription admin {admin.ExternalId} plan {planId}", ct);
    }

    public async Task<SubscriptionActivationResult> ConfirmSubscriptionPaymentAsync(
        string adminExternalId,
        string planId,
        long? mpPaymentId,
        bool allowDemo,
        CancellationToken ct = default)
    {
        var normalizedPlan = SubscriptionPlans.Normalize(planId);
        var admin = await userRepository.FindByExternalIdAsync(adminExternalId, ct)
            ?? throw new KeyNotFoundException($"Admin '{adminExternalId}' not found.");

        if (allowDemo && (hostEnvironment.IsDevelopment() || !IsMercadoPagoConfigured()))
        {
            await ActivateAdminSubscriptionAsync(admin, normalizedPlan, ct);
            return new SubscriptionActivationResult(
                true,
                normalizedPlan,
                admin.SubscriptionPaidUntil?.ToString("o"));
        }

        if (!mpPaymentId.HasValue || mpPaymentId.Value <= 0)
            throw new InvalidOperationException("paymentId is required to confirm a paid subscription.");

        EnsureSdkConfigured();

        var client = new PaymentClient();
        var mpPayment = await client.GetAsync(mpPaymentId.Value, cancellationToken: ct);

        if (!string.Equals(mpPayment.Status, "approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Payment status is {mpPayment.Status ?? "unknown"}.");

        var expectedRef = BuildSubscriptionExternalReference(adminExternalId, normalizedPlan);
        if (!string.Equals(mpPayment.ExternalReference, expectedRef, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Payment does not match the selected subscription plan.");

        await ActivateAdminSubscriptionAsync(admin, normalizedPlan, ct);

        return new SubscriptionActivationResult(
            true,
            normalizedPlan,
            admin.SubscriptionPaidUntil?.ToString("o"));
    }

    public async Task<CardPaymentResult> ProcessCardPaymentAsync(
        CardPaymentRequest request, CancellationToken ct = default)
    {
        var owner = await ownerResolver.ResolveOwnerAdminAsync(request.OwnerAdminExternalId, ct)
            ?? throw new InvalidOperationException("Valid ownerAdminId is required.");

        Receipt? receipt = null;
        if (!string.IsNullOrWhiteSpace(request.ReceiptExternalId))
        {
            receipt = await receiptRepository.FindByExternalIdAsync(request.ReceiptExternalId, ct);
        }

        if (request.Token == "DEMO-TOKEN")
        {
            logger.LogInformation("Demo payment processed for {Email} amount {Amount}", request.PayerEmail, request.TransactionAmount);

            if (receipt != null)
            {
                await ReconcilePaymentAsync(
                    receipt, owner.Id, request.TransactionAmount,
                    $"DEMO-{Guid.NewGuid():N}",
                    DateTimeOffset.UtcNow.ToString("o"),
                    ct);
            }

            return new CardPaymentResult(
                PaymentId: $"DEMO-{Guid.NewGuid().ToString()[..8]}",
                Status: "approved",
                StatusDetail: "accredited",
                TransactionAmount: request.TransactionAmount,
                PaymentMethodId: request.PaymentMethodId,
                DateApproved: DateTimeOffset.UtcNow.ToString("o"));
        }

        if (!IsMercadoPagoConfigured())
            throw new InvalidOperationException("Mercado Pago no está configurado. Agrega AccessToken en appsettings.Local.json.");

        EnsureSdkConfigured();

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

        var client = new PaymentClient();
        var mpPayment = await client.CreateAsync(paymentRequest, cancellationToken: ct);

        logger.LogInformation(
            "MercadoPago payment processed: {MpId} status={Status} for receipt {ReceiptId}",
            mpPayment.Id, mpPayment.Status, receipt?.ExternalId ?? "GENERAL");

        if (string.Equals(mpPayment.Status, "approved", StringComparison.OrdinalIgnoreCase) && receipt != null)
        {
            await ReconcilePaymentAsync(
                receipt, owner.Id, request.TransactionAmount,
                mpPayment.Id?.ToString() ?? string.Empty,
                mpPayment.DateApproved?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
                ct);
        }

        return new CardPaymentResult(
            PaymentId: mpPayment.Id?.ToString() ?? string.Empty,
            Status: mpPayment.Status ?? "unknown",
            StatusDetail: mpPayment.StatusDetail ?? string.Empty,
            TransactionAmount: mpPayment.TransactionAmount ?? request.TransactionAmount,
            PaymentMethodId: mpPayment.PaymentMethodId,
            DateApproved: mpPayment.DateApproved?.ToString("o"));
    }

    public async Task<WebhookResult> HandleWebhookAsync(long mpPaymentId, CancellationToken ct = default)
    {
        EnsureSdkConfigured();

        var client = new PaymentClient();
        global::MercadoPago.Resource.Payment.Payment mpPayment;

        try
        {
            mpPayment = await client.GetAsync(mpPaymentId, cancellationToken: ct);
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
            mpPayment.TransactionAmount ?? receipt.Amount,
            mpPaymentId.ToString(),
            mpPayment.DateApproved?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
            ct);

        logger.LogInformation("Webhook: reconciled MP payment {MpId} → receipt {ReceiptId}.", mpPaymentId, externalRef);
        return new WebhookResult(true, "approved");
    }

    private async Task<WebhookResult> HandleSubscriptionWebhookAsync(
        global::MercadoPago.Resource.Payment.Payment mpPayment,
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

        await ActivateAdminSubscriptionAsync(admin, planId, ct);
        logger.LogInformation(
            "Webhook: activated subscription {PlanId} for admin {AdminId} via MP {MpId}.",
            planId, adminExternalId, mpPayment.Id);

        return new WebhookResult(true, "subscription_activated");
    }

    private async Task<WebhookResult> HandleMaintenanceWebhookAsync(
        global::MercadoPago.Resource.Payment.Payment mpPayment,
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

        var mpId = mpPayment.Id?.ToString() ?? string.Empty;
        var bundleKey = $"MP-{mpId}-bundle";
        if (await paymentRepository.FindByExternalIdAsync(bundleKey, ct) is not null)
            return new WebhookResult(true, "already_processed");

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
