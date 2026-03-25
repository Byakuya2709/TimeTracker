using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence;

public class TimeTrackerDbContext : DbContext
{
    public TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<TrackingSession> TrackingSessions => Set<TrackingSession>();

    public DbSet<TrackingSessionAppUsage> TrackingSessionAppUsages => Set<TrackingSessionAppUsage>();

    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingSession>(entity =>
        {
            entity.ToTable("TrackingSessions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("Id");
            entity
                .Property(item => item.SessionDate)
                .HasColumnName("SessionDate")
                .HasConversion(
                    item => item.ToString("yyyy-MM-dd"),
                    item => DateOnly.Parse(item))
                .IsRequired();
            entity.Property(item => item.StartedAt).HasColumnName("StartedAt").IsRequired();
            entity.Property(item => item.EndedAt).HasColumnName("EndedAt").IsRequired();
            entity
                .Property(item => item.TotalDuration)
                .HasColumnName("TotalDurationSeconds")
                .HasConversion(
                    item => item.TotalSeconds,
                    item => TimeSpan.FromSeconds(item))
                .IsRequired();
            entity
                .Property(item => item.IdleDuration)
                .HasColumnName("IdleDurationSeconds")
                .HasConversion(
                    item => item.TotalSeconds,
                    item => TimeSpan.FromSeconds(item))
                .IsRequired();

            entity
                .Property(item => item.ProductivityScore)
                .HasColumnName("ProductivityScore")
                .IsRequired();
            entity
                .HasMany(item => item.AppUsages)
                .WithOne(item => item.Session)
                .HasForeignKey(item => item.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrackingSessionAppUsage>(entity =>
        {
            entity.ToTable("TrackingSessionAppUsages");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("Id");
            entity.Property(item => item.SessionId).HasColumnName("SessionId").IsRequired();
            entity.Property(item => item.AppName).HasColumnName("AppName").IsRequired();
            entity
                .Property(item => item.Duration)
                .HasColumnName("DurationSeconds")
                .HasConversion(
                    item => item.TotalSeconds,
                    item => TimeSpan.FromSeconds(item))
                .IsRequired();
        });

        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("UserSettings");
            entity.Property<int>("Id").HasColumnName("Id").ValueGeneratedNever();
            entity.HasKey("Id");
            entity.Property(item => item.IdleDetectionMinutes).HasColumnName("IdleDetectionMinutes").IsRequired();
            entity.Property(item => item.AutoStartOnBoot).HasColumnName("AutoStartOnBoot").IsRequired();
            entity.Property(item => item.OverlayOpacity).HasColumnName("OverlayOpacity").IsRequired();
            entity.Property(item => item.OverlayPosition).HasColumnName("OverlayPosition").IsRequired();
        });
    }
}
