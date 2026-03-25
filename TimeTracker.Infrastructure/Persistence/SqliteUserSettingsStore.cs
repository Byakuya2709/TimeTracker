using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TimeTracker.Application.Abstractions;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence;

public class SqliteUserSettingsStore : IUserSettingsStore
{
    private const int SingletonSettingsId = 1;

    private readonly IDbContextFactory<TimeTrackerDbContext> _dbContextFactory;

    public SqliteUserSettingsStore(IDbContextFactory<TimeTrackerDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        EnsureSettingsTable();
    }

    public UserSettings GetUserSettings()
    {
        using TimeTrackerDbContext dbContext = _dbContextFactory.CreateDbContext();

        UserSettings? settings = dbContext
            .UserSettings
            .AsNoTracking()
            .FirstOrDefault(item => EF.Property<int>(item, "Id") == SingletonSettingsId);

        return settings is null
            ? new UserSettings()
            : new UserSettings
            {
                IdleDetectionMinutes = NormalizeIdleDetectionMinutes(settings.IdleDetectionMinutes),
                AutoStartOnBoot = settings.AutoStartOnBoot,
                OverlayOpacity = Math.Clamp(settings.OverlayOpacity, 10, 100),
                OverlayPosition = string.IsNullOrWhiteSpace(settings.OverlayPosition)
                    ? "TopRight"
                    : settings.OverlayPosition
            };
    }

    public void SaveUserSettings(UserSettings settings)
    {
        using TimeTrackerDbContext dbContext = _dbContextFactory.CreateDbContext();

        UserSettings normalized = new()
        {
            IdleDetectionMinutes = NormalizeIdleDetectionMinutes(settings.IdleDetectionMinutes),
            AutoStartOnBoot = settings.AutoStartOnBoot,
            OverlayOpacity = Math.Clamp(settings.OverlayOpacity, 10, 100),
            OverlayPosition = string.IsNullOrWhiteSpace(settings.OverlayPosition)
                ? "TopRight"
                : settings.OverlayPosition
        };

        UserSettings? existing = dbContext
            .UserSettings
            .FirstOrDefault(item => EF.Property<int>(item, "Id") == SingletonSettingsId);

        if (existing is not null)
        {
            existing.IdleDetectionMinutes = normalized.IdleDetectionMinutes;
            existing.AutoStartOnBoot = normalized.AutoStartOnBoot;
            existing.OverlayOpacity = normalized.OverlayOpacity;
            existing.OverlayPosition = normalized.OverlayPosition;
        }
        else
        {
            EntityEntry<UserSettings> entry = dbContext.UserSettings.Add(normalized);
            entry.Property("Id").CurrentValue = SingletonSettingsId;
        }

        dbContext.SaveChanges();
    }

    private static int NormalizeIdleDetectionMinutes(int value)
    {
        return value <= 5 ? 5 : 10;
    }

    private void EnsureSettingsTable()
    {
        using TimeTrackerDbContext dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS UserSettings (
                Id INTEGER NOT NULL PRIMARY KEY,
                IdleDetectionMinutes INTEGER NOT NULL,
                AutoStartOnBoot INTEGER NOT NULL,
                OverlayOpacity INTEGER NOT NULL,
                OverlayPosition TEXT NOT NULL
            );
            """);
    }
}
