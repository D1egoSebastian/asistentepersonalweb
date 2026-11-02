using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class OwnerClockTests
{
    [Fact]
    public void TodayAt_UsesOwnerZone_NotUtcDate()
    {
        var bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        var utcNow = new DateTimeOffset(2026, 1, 1, 3, 0, 0, TimeSpan.Zero);

        var today = OwnerClock.TodayAt(utcNow, bogota);

        Assert.Equal(new DateOnly(2025, 12, 31), today);
    }

    [Fact]
    public void LocalTimeAt_LaPaz_IsFourHoursBehindUtc()
    {
        var laPaz = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        var utcNow = new DateTimeOffset(2026, 10, 16, 12, 30, 0, TimeSpan.Zero);

        Assert.Equal(new TimeOnly(8, 30), OwnerClock.LocalTimeAt(utcNow, laPaz));
    }

    [Fact]
    public void LocalTimeAt_Santiago_FollowsTheDaylightSavingChange()
    {
        var santiago = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
        var beforeChange = new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);
        var afterChange = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

        // Guard the dates of this test: Chile moves its clocks forward on the night of 5 to 6 September 2026.
        Assert.Equal(TimeSpan.FromHours(-4), santiago.GetUtcOffset(beforeChange));
        Assert.Equal(TimeSpan.FromHours(-3), santiago.GetUtcOffset(afterChange));

        Assert.Equal(new TimeOnly(8, 0), OwnerClock.LocalTimeAt(beforeChange, santiago));
        Assert.Equal(new TimeOnly(9, 0), OwnerClock.LocalTimeAt(afterChange, santiago));
    }

    [Fact]
    public void ResolveZone_RejectsAMissingZone()
    {
        Assert.Throws<InvalidOperationException>(() => OwnerClock.ResolveZone(null));
        Assert.Throws<InvalidOperationException>(() => OwnerClock.ResolveZone("  "));
    }

    [Fact]
    public void ResolveZone_RejectsAnUnknownZone()
    {
        Assert.Throws<InvalidOperationException>(() => OwnerClock.ResolveZone("Zona/Inventada"));
    }

    [Fact]
    public void ResolveZone_AcceptsAValidZone()
    {
        Assert.NotNull(OwnerClock.ResolveZone("America/La_Paz"));
    }
}
