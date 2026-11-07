using System.Security.Claims;
using System.Text.Json;
using BuildingFex.Api.Finances.Application.Internal.MercadoPago;
using BuildingFex.Api.Finances.Interfaces.Rest;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BuildingFex.Api.Tests;

public class MercadoPagoControllerTests
{
    private readonly Mock<IMercadoPagoService> _serviceMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ILogger<MercadoPagoController>> _loggerMock = new();
    private readonly MercadoPagoSettings _settings;

    public MercadoPagoControllerTests()
    {
        _settings = new MercadoPagoSettings
        {
            AccessToken = "TEST-access-token",
            PublicKey = "TEST-public-key",
            WebhookSecret = "test-secret",
            FrontendBaseUrl = "https://buildingfex.web.app",
            NotificationUrl = "https://api.buildingfex.com/api/v1/payments/webhook",
            UseSandbox = true
        };
    }

    private MercadoPagoController CreateController(ClaimsPrincipal? principal = null)
    {
        var controller = new MercadoPagoController(
            _serviceMock.Object,
            _userRepoMock.Object,
            Options.Create(_settings),
            _loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal ?? new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        return controller;
    }

    private static ClaimsPrincipal CreateResidentPrincipal(string externalId, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, externalId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, "resident"),
            new("ownerAdminId", "admin-1")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task CreateMaintenanceCheckout_ResidentMismatch_Returns403Forbidden()
    {
        // Arrange (Caso: Usuario no autorizado no puede pagar cuotas ajenas)
        var principal = CreateResidentPrincipal("resident-A", "a@test.com");
        var residentA = User.CreateResident("resident-A", "Resident A", "a@test.com", "hash", "1", "101", 1);

        _userRepoMock.Setup(u => u.FindByExternalIdAsync("resident-A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(residentA);

        var controller = CreateController(principal);

        var payload = JsonSerializer.Deserialize<JsonElement>("""
        {
            "residentId": "resident-B",
            "ownerAdminId": "admin-1"
        }
        """);

        // Act
        var result = await controller.CreateMaintenanceCheckout(payload, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_ResidentMismatch_Returns403Forbidden()
    {
        // Arrange
        var principal = CreateResidentPrincipal("resident-A", "a@test.com");
        var residentA = User.CreateResident("resident-A", "Resident A", "a@test.com", "hash", "1", "101", 1);

        _userRepoMock.Setup(u => u.FindByExternalIdAsync("resident-A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(residentA);

        var controller = CreateController(principal);

        var payload = JsonSerializer.Deserialize<JsonElement>("""
        {
            "residentId": "resident-B",
            "paymentId": 123456
        }
        """);

        // Act
        var result = await controller.ConfirmMaintenancePayment(payload, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task ConfirmMaintenancePayment_DemoFlag_RejectedWith400BadRequest()
    {
        // Arrange (Caso: demo=true intento de elusión de pago)
        var principal = CreateResidentPrincipal("resident-A", "a@test.com");
        var residentA = User.CreateResident("resident-A", "Resident A", "a@test.com", "hash", "1", "101", 1);

        _userRepoMock.Setup(u => u.FindByExternalIdAsync("resident-A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(residentA);

        var controller = CreateController(principal);

        var payload = JsonSerializer.Deserialize<JsonElement>("""
        {
            "residentId": "resident-A",
            "demo": true
        }
        """);

        // Act
        var result = await controller.ConfirmMaintenancePayment(payload, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task CreateMaintenanceCheckout_UnauthenticatedUser_Returns401Unauthorized()
    {
        // Arrange (Caso: Token inválido / sin autenticación)
        var controller = CreateController(new ClaimsPrincipal(new ClaimsIdentity()));

        var payload = JsonSerializer.Deserialize<JsonElement>("""
        {
            "residentId": "resident-A"
        }
        """);

        // Act
        var result = await controller.CreateMaintenanceCheckout(payload, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_Returns401Unauthorized()
    {
        // Arrange (Caso: Webhook con firma inválida o ausente)
        var controller = CreateController();
        controller.HttpContext.Request.Headers["x-signature"] = "ts=123,v1=invalid_hash";
        controller.HttpContext.Request.Headers["x-request-id"] = "req-1";

        var payload = JsonSerializer.Deserialize<JsonElement>("""
        {
            "data": { "id": "998877" }
        }
        """);

        // Act
        var result = await controller.Webhook(payload, CancellationToken.None);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorized.Value);
    }

    [Fact]
    public async Task Webhook_ValidSignature_ProcessesWebhookSuccessfully()
    {
        // Arrange
        var controller = CreateController();
        var dataId = "998877";
        var requestId = "req-123";
        var ts = "1700000000";

        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var hash = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();

        controller.HttpContext.Request.Headers["x-signature"] = $"ts={ts},v1={hash}";
        controller.HttpContext.Request.Headers["x-request-id"] = requestId;

        _serviceMock.Setup(s => s.HandleWebhookAsync(998877, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookResult(true, "approved"));

        var payload = JsonSerializer.Deserialize<JsonElement>("""
        {
            "data": { "id": "998877" }
        }
        """);

        // Act
        var result = await controller.Webhook(payload, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}

