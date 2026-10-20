using AsistentePersonal.Data;
using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

public class AppDbContextModelTests
{
    // Builds the EF model only. No connection to PostgreSQL is opened.
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void HabitLog_HasUniqueIndexOnHabitIdAndDate()
    {
        using var context = CreateContext();
        var logType = context.Model.FindEntityType(typeof(HabitLog))!;

        var index = logType.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "HabitId", "Date" }));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void HabitLog_CascadesDelete_WhenItsHabitIsDeleted()
    {
        using var context = CreateContext();
        var logType = context.Model.FindEntityType(typeof(HabitLog))!;

        var foreignKey = Assert.Single(logType.GetForeignKeys());

        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void User_Id_IsNotGeneratedByTheDatabase()
    {
        // The owner is always inserted with Id = 1, so a second insert fails on the primary key (spec 002, CA-1.7).
        using var context = CreateContext();
        var property = context.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.Id))!;

        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, property.ValueGenerated);
    }

    [Fact]
    public void Habit_StoresFrequencyAsText()
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(typeof(Habit))!.FindProperty(nameof(Habit.Frequency))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
    }
}
