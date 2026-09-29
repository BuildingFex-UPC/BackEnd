namespace BuildingFex.Api.Incidents.Domain.Model;

public static class IncidentStatuses
{
    public const string Open = "open";
    public const string InProgress = "in-progress";
    public const string Resolved = "resolved";

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Open;

        return status.Trim().ToLowerInvariant() switch
        {
            "open" => Open,
            "in-progress" or "in_progress" or "inprogress" => InProgress,
            "resolved" or "closed" => Resolved,
            _ => status.Trim(),
        };
    }

    public static bool IsValid(string? status)
    {
        var normalized = Normalize(status);
        return normalized is Open or InProgress or Resolved;
    }
}
