namespace AsistentePersonal.Services;

// The only code that reads the clock. "Today" is a calendar day in the owner's zone (spec D-1).
public static class OwnerClock
{
    public const string TimeZoneVariable = "OWNER_TIME_ZONE";

    public static DateOnly TodayAt(DateTimeOffset utcNow, TimeZoneInfo ownerZone)
    {
        var local = TimeZoneInfo.ConvertTime(utcNow, ownerZone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    // Turns the configured zone id into a time zone. A missing or unknown zone fails with a clear message,
    // so Program.cs can stop the app at startup instead of failing on every habits request.
    public static TimeZoneInfo ResolveZone(string? zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new InvalidOperationException($"Missing environment variable {TimeZoneVariable}.");
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(zoneId.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException($"Unknown time zone '{zoneId}' in {TimeZoneVariable}.", ex);
        }
    }

    public static DateOnly Today()
    {
        var ownerZone = ResolveZone(Environment.GetEnvironmentVariable(TimeZoneVariable));
        return TodayAt(DateTimeOffset.UtcNow, ownerZone);
    }
}
