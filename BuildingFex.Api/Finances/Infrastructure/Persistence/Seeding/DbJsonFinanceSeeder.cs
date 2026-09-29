using System.Text.Json;
using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Finances.Infrastructure.Persistence.Seeding;

public class DbJsonFinanceSeeder(
    IFeeRepository feeRepository,
    IPaymentRepository paymentRepository,
    IReceiptRepository receiptRepository,
    IFinanceSettingRepository financeSettingRepository,
    IKpiRepository kpiRepository,
    IAdminManagementExpenseRepository adminExpenseRepository,
    ISharedUtilityServiceRepository sharedUtilityServiceRepository,
    IFixedPayoutRecipientRepository fixedPayoutRecipientRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<DbJsonFinanceSeeder> logger)
{
    private const int MaxPhotoLength = 2000;

    public async Task SeedAsync(string dbJsonPath, CancellationToken cancellationToken = default)
    {
        if (await financeSettingRepository.AnyAsync(cancellationToken) ||
            await feeRepository.AnyAsync(cancellationToken) ||
            await receiptRepository.AnyAsync(cancellationToken) ||
            await paymentRepository.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Finances tables already seeded — skipping.");
            return;
        }

        if (!File.Exists(dbJsonPath))
        {
            logger.LogWarning("Seed file not found at {Path} — skipping finances seed.", dbJsonPath);
            return;
        }

        await using var stream = File.OpenRead(dbJsonPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var seeded = 0;
        seeded += await SeedFeesAsync(document, cancellationToken);
        seeded += await SeedPaymentsAsync(document, cancellationToken);
        seeded += await SeedReceiptsAsync(document, cancellationToken);
        seeded += await SeedFinanceSettingsAsync(document, cancellationToken);
        seeded += await SeedKpiAsync(document, cancellationToken);
        seeded += await SeedAdminExpensesAsync(document, cancellationToken);
        seeded += await SeedSharedServicesAsync(document, cancellationToken);
        seeded += await SeedFixedPayoutsAsync(document, cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} finance records from {Path}.", seeded, dbJsonPath);
    }

    private async Task<int> ResolveOwnerAndSkip(
        string? ownerExternalId,
        string entityLabel,
        string entityId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ownerExternalId))
        {
            logger.LogWarning("Skipping {Label} {Id}: missing ownerAdminId.", entityLabel, entityId);
            return 0;
        }

        var owner = await userRepository.FindByExternalIdAsync(ownerExternalId, ct);
        if (owner is null)
        {
            logger.LogWarning(
                "Skipping {Label} {Id}: owner admin {OwnerAdminId} not found.",
                entityLabel,
                entityId,
                ownerExternalId);
            return 0;
        }

        return owner.Id;
    }

    private async Task<int> SeedFeesAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("fees", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = FinanceCompatSeedId(entry, "id");
            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "fee",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            var fee = Fee.Create(
                externalId,
                ownerId,
                entry.GetProperty("residentId").GetString() ?? string.Empty,
                entry.GetProperty("month").GetString() ?? string.Empty,
                entry.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
                entry.GetProperty("dueDate").GetString() ?? string.Empty,
                entry.GetProperty("status").GetString() ?? "Pendiente");

            await feeRepository.AddAsync(fee, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedPaymentsAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("payments", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.TryGetProperty("id", out var idProp)
                ? FinanceCompatSeedId(idProp)
                : $"MP-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "payment",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            string? feeId = entry.TryGetProperty("feeId", out var feeProp)
                ? FinanceCompatSeedId(feeProp)
                : null;

            var payment = Payment.Create(
                externalId,
                ownerId,
                entry.GetProperty("residentId").GetString() ?? string.Empty,
                entry.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
                feeId,
                entry.TryGetProperty("feeMonth", out var monthProp) ? monthProp.GetString() : null,
                entry.TryGetProperty("paidAt", out var paidProp) ? paidProp.GetString() : null,
                entry.TryGetProperty("method", out var methodProp) ? methodProp.GetString() : null,
                entry.TryGetProperty("reference", out var refProp) ? refProp.GetString() : null);

            await paymentRepository.AddAsync(payment, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedReceiptsAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("receipts", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = FinanceCompatSeedId(entry, "id");
            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "receipt",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            var receipt = Receipt.Create(
                externalId,
                ownerId,
                entry.GetProperty("residentId").GetString() ?? string.Empty,
                entry.TryGetProperty("issueDate", out var issueProp) ? issueProp.GetString() : null,
                entry.TryGetProperty("dueDate", out var dueProp) ? dueProp.GetString() : null,
                entry.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
                entry.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? "Pending" : "Pending",
                entry.TryGetProperty("lateFee", out var lateProp) ? lateProp.GetDecimal() : 0,
                entry.TryGetProperty("extraCharges", out var extraProp) ? extraProp.GetDecimal() : 0,
                entry.TryGetProperty("concept", out var conceptProp) ? conceptProp.GetString() : null);

            await receiptRepository.AddAsync(receipt, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedFinanceSettingsAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("financeSettings", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.GetProperty("id").GetString() ?? string.Empty;
            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "financeSetting",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            var setting = FinanceSetting.Create(
                externalId,
                ownerId,
                entry.TryGetProperty("baseMonthlyExpense", out var baseProp) ? baseProp.GetDecimal() : 150,
                entry.TryGetProperty("lateFeeRate", out var rateProp) ? rateProp.GetDecimal() : 0.05m);

            await financeSettingRepository.AddAsync(setting, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedKpiAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("kpi", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.GetProperty("id").GetString() ?? string.Empty;
            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "kpi",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            var kpi = KpiRecord.Create(
                externalId,
                ownerId,
                entry.TryGetProperty("totalResidents", out var totalProp) ? totalProp.GetInt32() : 0,
                entry.TryGetProperty("occupiedUnits", out var occupiedProp) ? occupiedProp.GetInt32() : 0,
                entry.TryGetProperty("emptyUnits", out var emptyProp) ? emptyProp.GetInt32() : 0,
                entry.TryGetProperty("totalDebt", out var debtProp) ? debtProp.GetDecimal() : 0);

            await kpiRepository.AddAsync(kpi, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedAdminExpensesAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("adminManagementExpenses", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.GetProperty("id").GetString() ?? string.Empty;
            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "adminManagementExpense",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            var photo = entry.TryGetProperty("invoicePhotoUrl", out var photoProp)
                ? photoProp.GetString() ?? string.Empty
                : string.Empty;
            if (photo.Length > MaxPhotoLength)
                photo = string.Empty;

            var expense = AdminManagementExpense.Create(
                externalId,
                ownerId,
                entry.GetProperty("name").GetString() ?? string.Empty,
                entry.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
                entry.GetProperty("purchaseDate").GetString() ?? string.Empty,
                photo);

            await adminExpenseRepository.AddAsync(expense, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedSharedServicesAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("sharedUtilityServices", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.GetProperty("id").GetString() ?? string.Empty;
            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "sharedUtilityService",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            int? residentCount = entry.TryGetProperty("residentCount", out var countProp)
                ? countProp.GetInt32()
                : null;
            decimal? perShare = entry.TryGetProperty("perResidentShare", out var shareProp)
                ? shareProp.GetDecimal()
                : null;

            var service = SharedUtilityService.Create(
                externalId,
                ownerId,
                entry.GetProperty("type").GetString() ?? string.Empty,
                entry.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0,
                entry.TryGetProperty("month", out var monthProp) ? monthProp.GetString() : null,
                residentCount,
                perShare);

            await sharedUtilityServiceRepository.AddAsync(service, ct);
            count++;
        }

        return count;
    }

    private async Task<int> SeedFixedPayoutsAsync(JsonDocument document, CancellationToken ct)
    {
        if (!document.RootElement.TryGetProperty("fixedPayoutRecipients", out var element) ||
            element.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var entry in element.EnumerateArray())
        {
            var externalId = entry.GetProperty("id").GetString() ?? string.Empty;
            var ownerId = await ResolveOwnerAndSkip(
                entry.TryGetProperty("ownerAdminId", out var ownerProp) ? ownerProp.GetString() : null,
                "fixedPayoutRecipient",
                externalId,
                ct);
            if (ownerId == 0)
                continue;

            var photo = entry.TryGetProperty("photoUrl", out var photoProp)
                ? photoProp.GetString() ?? string.Empty
                : string.Empty;
            if (photo.Length > MaxPhotoLength)
                photo = string.Empty;

            var paymentHistoryJson = entry.TryGetProperty("paymentHistory", out var histProp)
                ? histProp.GetRawText()
                : "[]";

            var recipient = FixedPayoutRecipient.Create(
                externalId,
                ownerId,
                entry.GetProperty("name").GetString() ?? string.Empty,
                entry.GetProperty("dni").GetString() ?? string.Empty,
                entry.GetProperty("phone").GetString() ?? string.Empty,
                entry.TryGetProperty("salary", out var salaryProp) ? salaryProp.GetDecimal() : 0,
                entry.TryGetProperty("intervalDays", out var intervalProp) ? intervalProp.GetInt32() : 30,
                entry.GetProperty("nextPaymentDate").GetString() ?? string.Empty,
                photo,
                paymentHistoryJson,
                entry.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetString() : null);

            await fixedPayoutRecipientRepository.AddAsync(recipient, ct);
            count++;
        }

        return count;
    }

    private static string FinanceCompatSeedId(JsonElement entry, string propertyName)
        => FinanceCompatSeedId(entry.GetProperty(propertyName));

    private static string FinanceCompatSeedId(JsonElement idElement) =>
        idElement.ValueKind switch
        {
            JsonValueKind.Number => idElement.GetRawText(),
            JsonValueKind.String => idElement.GetString() ?? string.Empty,
            _ => idElement.ToString(),
        };
}
