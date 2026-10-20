using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

public class DatabaseSmokeTests
{
    [Fact]
    public async Task Habit_CanBeSavedAndReadBack()
    {
        await using (var writer = await TestDatabase.CreateEmptyContextAsync())
        {
            writer.Habits.Add(new Habit
            {
                UserId = 1,
                Name = "Leer",
                Frequency = Frequency.Daily,
                CreatedOn = new DateOnly(2026, 10, 16),
            });
            await writer.SaveChangesAsync();
        }

        await using var reader = await TestDatabase.OpenAsync();
        var saved = await reader.Habits.SingleAsync();

        Assert.Equal("Leer", saved.Name);
        Assert.Equal(Frequency.Daily, saved.Frequency);
        Assert.Equal(new DateOnly(2026, 10, 16), saved.CreatedOn);
    }
}
