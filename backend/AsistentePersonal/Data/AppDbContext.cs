using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
}
