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
