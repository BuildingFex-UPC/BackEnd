namespace BuildingFex.Api.Iam.Domain.Model;

public static class SubscriptionPlanIds
{
    public const string Free = "free";
    public const string Essential = "essential";
    public const string Standard = "standard";
    public const string Scale = "scale";
}

public static class SubscriptionPlans
{
    private static readonly Dictionary<string, int> ResidentLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        [SubscriptionPlanIds.Free] = 9,
        [SubscriptionPlanIds.Essential] = 15,
        [SubscriptionPlanIds.Standard] = 40,
        [SubscriptionPlanIds.Scale] = 80,
    };

    private static readonly Dictionary<string, decimal> MonthlyPricesPen = new(StringComparer.OrdinalIgnoreCase)
    {
        [SubscriptionPlanIds.Free] = 0m,
        [SubscriptionPlanIds.Essential] = 40m,
        [SubscriptionPlanIds.Standard] = 80m,
        [SubscriptionPlanIds.Scale] = 120m,
    };

    public static bool IsValid(string? planId) =>
        !string.IsNullOrWhiteSpace(planId) &&
        ResidentLimits.ContainsKey(planId.Trim());

    public static string Normalize(string? planId) =>
        IsValid(planId) ? planId!.Trim().ToLowerInvariant() : SubscriptionPlanIds.Free;

    public static int MaxResidents(string? planId) =>
        ResidentLimits.TryGetValue(Normalize(planId), out var limit)
            ? limit
            : ResidentLimits[SubscriptionPlanIds.Free];

    public static decimal MonthlyPricePen(string? planId) =>
        MonthlyPricesPen.TryGetValue(Normalize(planId), out var price)
            ? price
            : 0m;

    public static bool IsPaid(string? planId) => MonthlyPricePen(planId) > 0m;
}
