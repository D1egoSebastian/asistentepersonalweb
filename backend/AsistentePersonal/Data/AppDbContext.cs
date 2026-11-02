using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<DailyDigestLog> DailyDigestLogs => Set<DailyDigestLog>();
    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureIdea(modelBuilder);
        ConfigureReminders(modelBuilder);

        // The owner is always inserted with Id = 1: the database must not generate it (spec 002, CA-1.7).
        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedNever();

        // Stored as text ("Daily", "Weekly") so the database is readable without the C# enum.
        modelBuilder.Entity<Habit>()
            .Property(h => h.Frequency)
            .HasConversion<string>();

        // One record per habit per day, and deleting a habit deletes its history.
        modelBuilder.Entity<HabitLog>()
            .HasIndex(l => new { l.HabitId, l.Date })
            .IsUnique();

        modelBuilder.Entity<HabitLog>()
            .HasOne<Habit>()
            .WithMany()
            .HasForeignKey(l => l.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureReminders(ModelBuilder modelBuilder)
    {
        // The date is the key: one digest per day, and the database enforces it (spec 005, RNF-3).
        modelBuilder.Entity<DailyDigestLog>()
            .HasKey(d => d.ForDate);

        modelBuilder.Entity<DailyDigestLog>()
            .Property(d => d.ForDate)
            .ValueGeneratedNever();

        // One reminder per idea per day, and deleting an idea deletes its reminders.
        modelBuilder.Entity<ReminderLog>()
            .HasIndex(r => new { r.IdeaId, r.SentOn })
            .IsUnique();

        modelBuilder.Entity<ReminderLog>()
            .HasOne<Idea>()
            .WithMany()
            .HasForeignKey(r => r.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIdea(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(typeof(IdeaFunctions).GetMethod(nameof(IdeaFunctions.Unaccent))!);

        modelBuilder.Entity<Idea>(entity =>
        {
            // Tags live inside the idea as a PostgreSQL text array (plan D-T1).
            entity.Property(i => i.Tags).HasColumnType("text[]");

            // xmin is PostgreSQL's row version: no column is created, EF reads it after each write (plan D-T12).
            entity.Property(i => i.Version).IsRowVersion();

            // Lists filter by owner and section, and sort by creation.
            entity.HasIndex(i => new { i.UserId, i.ArchivedAt, i.CreatedAt });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
