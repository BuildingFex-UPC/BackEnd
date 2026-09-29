using System.Security.Cryptography;
using System.Text;

namespace BuildingFex.Api.Finances.Application.Internal.MercadoPago;

public static class MercadoPagoWebhookValidator
{
    public static bool TryValidateSignature(
        string? signatureHeader,
        string? requestId,
        string? dataId,
        string webhookSecret,
        out string? failureReason)
    {
        failureReason = null;

        if (string.IsNullOrWhiteSpace(webhookSecret))
            return true;

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            failureReason = "missing_x_signature";
            return false;
        }

        string? ts = null;
        string? receivedHash = null;

        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;

            if (kv[0] == "ts") ts = kv[1];
            if (kv[0] == "v1") receivedHash = kv[1];
        }

        if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(receivedHash))
        {
            failureReason = "invalid_x_signature_format";
            return false;
        }

        var manifest = $"id:{dataId ?? string.Empty};request-id:{requestId ?? string.Empty};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();

        if (!string.Equals(computed, receivedHash, StringComparison.OrdinalIgnoreCase))
        {
            failureReason = "signature_mismatch";
            return false;
        }

        return true;
    }
}
