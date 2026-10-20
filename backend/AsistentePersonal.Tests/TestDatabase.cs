using AsistentePersonal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

// Database tests share one database, so they must not run in parallel.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace AsistentePersonal.Tests;

// Gives database tests a migrated PostgreSQL database. The connection string comes from ConnectionStrings__Test.
public static class TestDatabase
{
    public const string ConnectionStringVariable = "ConnectionStrings__Test";

    // Opens the test database with the schema applied. Keeps existing rows.
    // Interceptors let a test simulate another request acting right before a save.
    public static async Task<AppDbContext> OpenAsync(params IInterceptor[] interceptors)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? throw new InvalidOperationException($"Missing environment variable {ConnectionStringVariable} (test database connection string).");
        EnsureIsTestDatabase(connectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(interceptors)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    // Opens the test database and removes all rows, so each test starts from an empty state.
    public static async Task<AppDbContext> CreateEmptyContextAsync()
    {
        var context = await OpenAsync();
        await context.HabitLogs.ExecuteDeleteAsync();
        await context.Habits.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();
        return context;
    }

    // The tests delete every row, so they refuse to run against a database that is not clearly a test one.
    public static void EnsureIsTestDatabase(string connectionString)
    {
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (database is null || !database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringVariable} must point to a database whose name ends with '_test' (got '{database}'). The tests delete all rows.");
        }
    }
}
