using System.Security.Cryptography;
using System.Text;
using BuildingFex.Api.Finances.Application.Internal.MercadoPago;
using Xunit;

namespace BuildingFex.Api.Tests;

public class MercadoPagoWebhookValidatorTests
{
    private static string GenerateValidHeader(string dataId, string requestId, string ts, string secret)
    {
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        return $"ts={ts},v1={hash}";
    }

    [Fact]
    public void TryValidateSignature_ValidSignature_ReturnsTrue()
    {
        var secret = "test_webhook_secret_key_12345";
        var dataId = "99887766";
        var requestId = "req-abc-123";
        var ts = "1700000000";
        var header = GenerateValidHeader(dataId, requestId, ts, secret);

        var isValid = MercadoPagoWebhookValidator.TryValidateSignature(
            header, requestId, dataId, secret, out var failureReason);

        Assert.True(isValid);
        Assert.Null(failureReason);
    }

    [Fact]
    public void TryValidateSignature_MissingSecret_ReturnsFalse()
    {
        var isValid = MercadoPagoWebhookValidator.TryValidateSignature(
            "ts=123,v1=abc", "req-1", "9988", "", out var failureReason);

        Assert.False(isValid);
        Assert.Equal("webhook_secret_not_configured", failureReason);
    }

    [Fact]
    public void TryValidateSignature_MissingSignatureHeader_ReturnsFalse()
    {
        var isValid = MercadoPagoWebhookValidator.TryValidateSignature(
            null, "req-1", "9988", "my-secret", out var failureReason);

        Assert.False(isValid);
        Assert.Equal("missing_x_signature", failureReason);
    }

    [Fact]
    public void TryValidateSignature_MalformedSignatureHeader_ReturnsFalse()
    {
        var isValid = MercadoPagoWebhookValidator.TryValidateSignature(
            "malformed_header", "req-1", "9988", "my-secret", out var failureReason);

        Assert.False(isValid);
        Assert.Equal("invalid_x_signature_format", failureReason);
    }

    [Fact]
    public void TryValidateSignature_SignatureMismatch_ReturnsFalse()
    {
        var secret = "test_webhook_secret_key_12345";
        var dataId = "99887766";
        var requestId = "req-abc-123";
        var ts = "1700000000";
        var header = GenerateValidHeader(dataId, requestId, ts, "different_secret");

        var isValid = MercadoPagoWebhookValidator.TryValidateSignature(
            header, requestId, dataId, secret, out var failureReason);

        Assert.False(isValid);
        Assert.Equal("signature_mismatch", failureReason);
    }

    [Fact]
    public void TryValidateSignature_TamperedDataId_ReturnsFalse()
    {
        var secret = "test_webhook_secret_key_12345";
        var requestId = "req-abc-123";
        var ts = "1700000000";
        var header = GenerateValidHeader("original_id", requestId, ts, secret);

        var isValid = MercadoPagoWebhookValidator.TryValidateSignature(
            header, requestId, "tampered_id", secret, out var failureReason);

        Assert.False(isValid);
        Assert.Equal("signature_mismatch", failureReason);
    }
}

