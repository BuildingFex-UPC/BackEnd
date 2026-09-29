using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;

namespace BuildingFex.Api.SocialSpaces.Application.Internal;

public static class ReservationOverlapHelper
{
    public static bool Overlaps(Reservation existing, string date, string startTime, string endTime)
    {
        if (!string.Equals(existing.Date, date, StringComparison.Ordinal))
            return false;

        var aStart = TimeToMinutes(existing.StartTime);
        var aEnd = TimeToMinutes(existing.EndTime);
        var bStart = TimeToMinutes(startTime);
        var bEnd = TimeToMinutes(endTime);

        if (aStart is null || aEnd is null || bStart is null || bEnd is null)
            return false;

        return aStart < bEnd && bStart < aEnd;
    }

    private static int? TimeToMinutes(string time)
    {
        var parts = time.Split(':');
        if (parts.Length < 2)
            return null;

        if (!int.TryParse(parts[0], out var hours) || !int.TryParse(parts[1], out var minutes))
            return null;

        return hours * 60 + minutes;
    }
}
