using System.Text.Json;
using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;

namespace BuildingFex.Api.Finances.Interfaces.Rest.Transform;

public static class FinanceCompatSerializer
{
    public static object ToCompatId(string externalId)
    {
        if (long.TryParse(externalId, out var numeric))
            return numeric;
        return externalId;
    }

    public static string OwnerExternalId(User? owner) => owner?.ExternalId ?? string.Empty;

    public static object FeeToJson(Fee fee) => new
    {
        id = ToCompatId(fee.ExternalId),
        residentId = fee.ResidentExternalId,
        month = fee.Month,
        amount = fee.Amount,
        dueDate = fee.DueDate,
        status = fee.Status,
        ownerAdminId = OwnerExternalId(fee.OwnerAdmin),
    };

    public static object PaymentToJson(Payment payment) => new
    {
        id = payment.ExternalId,
        residentId = payment.ResidentExternalId,
        feeId = string.IsNullOrWhiteSpace(payment.FeeExternalId)
            ? null
            : ToCompatId(payment.FeeExternalId),
        feeMonth = payment.FeeMonth,
        amount = payment.Amount,
        paidAt = payment.PaidAt,
        method = payment.Method,
        reference = payment.Reference,
        ownerAdminId = OwnerExternalId(payment.OwnerAdmin),
    };

    public static object ReceiptToJson(Receipt receipt) => new
    {
        id = ToCompatId(receipt.ExternalId),
        residentId = receipt.ResidentExternalId,
        issueDate = receipt.IssueDate,
        dueDate = receipt.DueDate,
        amount = receipt.Amount,
        status = receipt.Status,
        lateFee = receipt.LateFee,
        extraCharges = receipt.ExtraCharges,
        concept = receipt.Concept,
        ownerAdminId = OwnerExternalId(receipt.OwnerAdmin),
    };

    public static object FinanceSettingToJson(FinanceSetting setting) => new
    {
        id = setting.ExternalId,
        ownerAdminId = OwnerExternalId(setting.OwnerAdmin),
        baseMonthlyExpense = setting.BaseMonthlyExpense,
        lateFeeRate = setting.LateFeeRate,
    };

    public static object KpiToJson(KpiRecord kpi) => new
    {
        id = kpi.ExternalId,
        ownerAdminId = OwnerExternalId(kpi.OwnerAdmin),
        totalResidents = kpi.TotalResidents,
        occupiedUnits = kpi.OccupiedUnits,
        emptyUnits = kpi.EmptyUnits,
        totalDebt = kpi.TotalDebt,
    };

    public static object AdminExpenseToJson(AdminManagementExpense expense) => new
    {
        id = expense.ExternalId,
        name = expense.Name,
        amount = expense.Amount,
        purchaseDate = expense.PurchaseDate,
        invoicePhotoUrl = expense.InvoicePhotoUrl,
        ownerAdminId = OwnerExternalId(expense.OwnerAdmin),
    };

    public static object SharedServiceToJson(SharedUtilityService service) => new
    {
        id = service.ExternalId,
        type = service.Type,
        amount = service.Amount,
        month = service.Month,
        residentCount = service.ResidentCount,
        perResidentShare = service.PerResidentShare,
        ownerAdminId = OwnerExternalId(service.OwnerAdmin),
    };

    public static object FixedPayoutToJson(FixedPayoutRecipient recipient)
    {
        object[] paymentHistory;
        try
        {
            paymentHistory = JsonSerializer.Deserialize<object[]>(
                string.IsNullOrWhiteSpace(recipient.PaymentHistoryJson)
                    ? "[]"
                    : recipient.PaymentHistoryJson) ?? [];
        }
        catch
        {
            paymentHistory = [];
        }

        return new
        {
            id = recipient.ExternalId,
            name = recipient.Name,
            dni = recipient.Dni,
            phone = recipient.Phone,
            salary = recipient.Salary,
            intervalDays = recipient.IntervalDays,
            nextPaymentDate = recipient.NextPaymentDate,
            photoUrl = recipient.PhotoUrl,
            paymentHistory,
            createdAt = recipient.CreatedAtIso,
            ownerAdminId = OwnerExternalId(recipient.OwnerAdmin),
        };
    }

    public static string NormalizeExternalId(object? id, string? fallbackPrefix = null)
    {
        if (id is null)
            return $"{fallbackPrefix}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        return id switch
        {
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetRawText(),
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString() ?? string.Empty,
            _ => id.ToString() ?? string.Empty,
        };
    }

    public static string? NormalizeOptionalExternalId(object? value)
    {
        if (value is null)
            return null;

        return value switch
        {
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetRawText(),
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            _ => value.ToString(),
        };
    }
}
