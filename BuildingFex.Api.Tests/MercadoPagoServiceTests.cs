using BuildingFex.Api.Finances.Application.Internal;
using BuildingFex.Api.Finances.Application.Internal.MercadoPago;
using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BuildingFex.Api.Tests;

public class MercadoPagoServiceTests
{
    private readonly Mock<IMercadoPagoGateway> _gatewayMock = new();
    private readonly Mock<IReceiptRepository> _receiptRepoMock = new();
    private readonly Mock<IFeeRepository> _feeRepoMock = new();
    private readonly Mock<IPaymentRepository> _paymentRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<MercadoPagoService>> _loggerMock = new();
    private readonly MercadoPagoSettings _settings;
    private readonly FinanceOwnerResolver _ownerResolver;

    public MercadoPagoServiceTests()
    {
        _settings = new MercadoPagoSettings
        {
            AccessToken = "TEST-123456789-access-token",
            PublicKey = "TEST-public-key",
            WebhookSecret = "test-webhook-secret",
            FrontendBaseUrl = "https://buildingfex.web.app",
            NotificationUrl = "https://api.buildingfex.com/api/v1/payments/webhook",
            UseSandbox = true
        };

        _ownerResolver = new FinanceOwnerResolver(_userRepoMock.Object);
    }

    private MercadoPagoService CreateService()
    {
        return new MercadoPagoService(
            Options.Create(_settings),
            _gatewayMock.Object,
            _receiptRepoMock.Object,
            _feeRepoMock.Object,
            _paymentRepoMock.Object,
            _userRepoMock.Object,
            _ownerResolver,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private static User CreateAdminUser(string externalId = "admin-1")
    {
        return User.CreateAdmin(externalId, "Admin Test", "admin@buildingfex.test", "hash");
    }

    [Fact]
    public async Task CreateMaintenanceCheckout_CalculatesAmountOnServer_CreatesPreferenceWithSandbox()
    {
        // Arrange
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var receipt = Receipt.Create("R-1", 1, "res-1", "2026-01-01", "2026-01-15", 150m, "Pending", 10m, 5m, "Mantenimiento Enero");
        var fee = Fee.Create("F-1", 1, "res-1", "2026-01", 100m, "2026-01-20", "Pendiente");

        _receiptRepoMock.Setup(r => r.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt> { receipt });
        _feeRepoMock.Setup(f => f.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fee> { fee });

        _gatewayMock.Setup(g => g.CreatePreferenceAsync(It.IsAny<global::MercadoPago.Client.Preference.PreferenceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreferenceResult("PREF-123", "https://sandbox.mercadopago.com.pe/checkout/v1/redirect?pref_id=PREF-123"));

        var service = CreateService();

        // Act
        var result = await service.CreateMaintenanceCheckoutPreferenceAsync(
            new CreateMaintenanceCheckoutRequest("res-1", "admin-1", "residente@test.com"));

        // Assert
        Assert.Equal("PREF-123", result.PreferenceId);
        Assert.Contains("sandbox.mercadopago", result.InitPoint);

        // Expected amount: (150 + 10 + 5) + 100 = 265
        _gatewayMock.Verify(g => g.CreatePreferenceAsync(
            It.Is<global::MercadoPago.Client.Preference.PreferenceRequest>(p =>
                p.Items.First().UnitPrice == 265m &&
                p.Items.First().CurrencyId == "PEN" &&
                p.ExternalReference == "MAINTENANCE:res-1:admin-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_ApprovedPayment_MarksFeesAndReceiptsPaid_AndRecordsPayment()
    {
        // Arrange (Caso: Pago aprobado)
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var receipt = Receipt.Create("R-1", 1, "res-1", "2026-01-01", "2026-01-15", 150m, "Pending", 0m, 0m, "Mantenimiento Enero");
        var fee = Fee.Create("F-1", 1, "res-1", "2026-01", 100m, "2026-01-20", "Pendiente");

        _receiptRepoMock.Setup(r => r.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt> { receipt });
        _feeRepoMock.Setup(f => f.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fee> { fee });

        _paymentRepoMock.Setup(p => p.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var mpPayment = new MercadoPagoPaymentData(
            Id: 998877,
            Status: "approved",
            StatusDetail: "accredited",
            CurrencyId: "PEN",
            TransactionAmount: 250m,
            ExternalReference: "MAINTENANCE:res-1:admin-1",
            DateApproved: DateTime.UtcNow,
            PaymentMethodId: "visa");

        _gatewayMock.Setup(g => g.GetPaymentAsync(998877, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        var service = CreateService();

        // Act
        var result = await service.ConfirmMaintenancePaymentAsync(
            new ConfirmMaintenancePaymentRequest("res-1", "admin-1", 998877));

        // Assert
        Assert.True(result.Reconciled);
        Assert.Equal(2, result.ItemsPaid);
        Assert.Equal("Paid", receipt.Status);
        Assert.Equal("Pagado", fee.Status);

        _receiptRepoMock.Verify(r => r.Update(receipt), Times.Once);
        _feeRepoMock.Verify(f => f.Update(fee), Times.Once);
        _paymentRepoMock.Verify(p => p.AddAsync(It.Is<Payment>(pm => pm.Reference == "MP-998877"), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_RejectedPayment_ThrowsException_LeavesCuotasPending()
    {
        // Arrange (Caso: Pago rechazado)
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var fee = Fee.Create("F-1", 1, "res-1", "2026-01", 100m, "2026-01-20", "Pendiente");
        _receiptRepoMock.Setup(r => r.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt>());
        _feeRepoMock.Setup(f => f.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fee> { fee });

        var mpPayment = new MercadoPagoPaymentData(
            Id: 112233,
            Status: "rejected",
            StatusDetail: "cc_rejected_insufficient_amount",
            CurrencyId: "PEN",
            TransactionAmount: 100m,
            ExternalReference: "MAINTENANCE:res-1:admin-1",
            DateApproved: null,
            PaymentMethodId: "visa");

        _gatewayMock.Setup(g => g.GetPaymentAsync(112233, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmMaintenancePaymentAsync(new ConfirmMaintenancePaymentRequest("res-1", "admin-1", 112233)));

        Assert.Contains("no está aprobado", ex.Message);
        Assert.Equal("Pendiente", fee.Status);
        _paymentRepoMock.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_PendingPayment_ThrowsException_LeavesCuotasPending()
    {
        // Arrange (Caso: Pago pendiente)
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var fee = Fee.Create("F-1", 1, "res-1", "2026-01", 100m, "2026-01-20", "Pendiente");
        _feeRepoMock.Setup(f => f.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fee> { fee });

        var mpPayment = new MercadoPagoPaymentData(
            Id: 445566,
            Status: "in_process",
            StatusDetail: "pending_review_manual",
            CurrencyId: "PEN",
            TransactionAmount: 100m,
            ExternalReference: "MAINTENANCE:res-1:admin-1",
            DateApproved: null,
            PaymentMethodId: "ticket");

        _gatewayMock.Setup(g => g.GetPaymentAsync(445566, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmMaintenancePaymentAsync(new ConfirmMaintenancePaymentRequest("res-1", "admin-1", 445566)));

        Assert.Contains("no está aprobado", ex.Message);
        Assert.Equal("Pendiente", fee.Status);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_WrongExternalReference_ThrowsException()
    {
        // Arrange (Caso: Referencia incorrecta)
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var mpPayment = new MercadoPagoPaymentData(
            Id: 778899,
            Status: "approved",
            StatusDetail: "accredited",
            CurrencyId: "PEN",
            TransactionAmount: 100m,
            ExternalReference: "MAINTENANCE:other-resident:admin-1",
            DateApproved: DateTime.UtcNow,
            PaymentMethodId: "visa");

        _gatewayMock.Setup(g => g.GetPaymentAsync(778899, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmMaintenancePaymentAsync(new ConfirmMaintenancePaymentRequest("res-1", "admin-1", 778899)));

        Assert.Contains("referencia del pago no coincide", ex.Message);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_IncorrectAmount_ThrowsException()
    {
        // Arrange (Caso: Importe incorrecto - menor a la deuda)
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var fee = Fee.Create("F-1", 1, "res-1", "2026-01", 200m, "2026-01-20", "Pendiente");
        _receiptRepoMock.Setup(r => r.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt>());
        _feeRepoMock.Setup(f => f.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fee> { fee });
        _paymentRepoMock.Setup(p => p.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var mpPayment = new MercadoPagoPaymentData(
            Id: 556677,
            Status: "approved",
            StatusDetail: "accredited",
            CurrencyId: "PEN",
            TransactionAmount: 50m, // Only paid 50 instead of 200
            ExternalReference: "MAINTENANCE:res-1:admin-1",
            DateApproved: DateTime.UtcNow,
            PaymentMethodId: "visa");

        _gatewayMock.Setup(g => g.GetPaymentAsync(556677, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmMaintenancePaymentAsync(new ConfirmMaintenancePaymentRequest("res-1", "admin-1", 556677)));

        Assert.Contains("menor al total adeudado", ex.Message);
        Assert.Equal("Pendiente", fee.Status);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_Idempotent_DoesNotDuplicatePayments()
    {
        // Arrange (Caso: Confirmación repetida / Idempotencia)
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var mpPayment = new MercadoPagoPaymentData(
            Id: 123456,
            Status: "approved",
            StatusDetail: "accredited",
            CurrencyId: "PEN",
            TransactionAmount: 100m,
            ExternalReference: "MAINTENANCE:res-1:admin-1",
            DateApproved: DateTime.UtcNow,
            PaymentMethodId: "visa");

        _gatewayMock.Setup(g => g.GetPaymentAsync(123456, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        // Already exists in repository from previous confirmation or webhook
        var existingPayment = Payment.Create(
            "MP-123456-F-F-1", 1, "res-1", 100m, "F-1", "2026-01", DateTime.UtcNow.ToString("o"), "Mercado Pago", "MP-123456");

        _paymentRepoMock.Setup(p => p.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { existingPayment });

        var service = CreateService();

        // Act
        var result = await service.ConfirmMaintenancePaymentAsync(
            new ConfirmMaintenancePaymentRequest("res-1", "admin-1", 123456));

        // Assert
        Assert.True(result.Reconciled);
        Assert.Equal(1, result.ItemsPaid); // Reports already reconciled items count
        _paymentRepoMock.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_AllowDemoTrue_RejectedWithSecurityException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmMaintenancePaymentAsync(
                new ConfirmMaintenancePaymentRequest("res-1", "admin-1", 12345, AllowDemo: true)));

        Assert.Contains("conciliación demo no está permitida", ex.Message);
    }

    [Fact]
    public async Task HandleWebhook_DuplicateWebhook_ReturnsAlreadyProcessed_DoesNotDuplicate()
    {
        // Arrange (Caso: Webhook repetido)
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var mpPayment = new MercadoPagoPaymentData(
            Id: 888999,
            Status: "approved",
            StatusDetail: "accredited",
            CurrencyId: "PEN",
            TransactionAmount: 150m,
            ExternalReference: "MAINTENANCE:res-1:admin-1",
            DateApproved: DateTime.UtcNow,
            PaymentMethodId: "visa");

        _gatewayMock.Setup(g => g.GetPaymentAsync(888999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        var existingPayment = Payment.Create(
            "MP-888999-R-R-1", 1, "res-1", 150m, null, null, DateTime.UtcNow.ToString("o"), "Mercado Pago", "MP-888999");

        _paymentRepoMock.Setup(p => p.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { existingPayment });

        var service = CreateService();

        // Act
        var result = await service.HandleWebhookAsync(888999);

        // Assert
        Assert.True(result.Processed);
        Assert.Equal("already_processed", result.Status);
        _paymentRepoMock.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_ApprovedMaintenancePayment_ReconcilesSuccessfully()
    {
        // Arrange
        var admin = CreateAdminUser("admin-1");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var receipt = Receipt.Create("R-1", 1, "res-1", "2026-01-01", "2026-01-15", 150m, "Pending", 0m, 0m, "Mantenimiento Enero");

        _receiptRepoMock.Setup(r => r.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt> { receipt });
        _feeRepoMock.Setup(f => f.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fee>());
        _paymentRepoMock.Setup(p => p.ListAsync("admin-1", "res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var mpPayment = new MercadoPagoPaymentData(
            Id: 555444,
            Status: "approved",
            StatusDetail: "accredited",
            CurrencyId: "PEN",
            TransactionAmount: 150m,
            ExternalReference: "MAINTENANCE:res-1:admin-1",
            DateApproved: DateTime.UtcNow,
            PaymentMethodId: "visa");

        _gatewayMock.Setup(g => g.GetPaymentAsync(555444, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mpPayment);

        var service = CreateService();

        // Act
        var result = await service.HandleWebhookAsync(555444);

        // Assert
        Assert.True(result.Processed);
        Assert.Equal("approved", result.Status);
        Assert.Equal("Paid", receipt.Status);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

