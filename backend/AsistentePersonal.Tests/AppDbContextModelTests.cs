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

    [Fact]
    public void Idea_StoresTagsAsTextArray()
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(typeof(Idea))!.FindProperty(nameof(Idea.Tags))!;

        Assert.Equal("text[]", property.GetColumnType());
    }

    [Fact]
    public void Idea_HasIndexOnUserArchivedAndCreation()
    {
        // Lists filter by owner and section, and sort by creation (plan §3.2).
        using var context = CreateContext();
        var ideaType = context.Model.FindEntityType(typeof(Idea))!;

        var index = ideaType.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "UserId", "ArchivedAt", "CreatedAt" }));

        Assert.False(index.IsUnique);
    }

    [Fact]
    public void Idea_VersionIsPostgresRowVersion_ThatChangesOnEveryWrite()
    {
        // Version maps to xmin (plan D-T12): no column is created, and EF reads it after each write.
        using var context = CreateContext();
        var property = context.Model.FindEntityType(typeof(Idea))!.FindProperty(nameof(Idea.Version))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }
}
