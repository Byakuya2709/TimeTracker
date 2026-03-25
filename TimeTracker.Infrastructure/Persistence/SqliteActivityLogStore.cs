using Microsoft.EntityFrameworkCore;
using TimeTracker.Application.Abstractions;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence;

public class SqliteActivityLogStore : IActivityLogStore
{
    private readonly DbContextOptions<TimeTrackerDbContext> _dbContextOptions;

    public SqliteActivityLogStore(string databasePath)
    {
        DbContextOptionsBuilder<TimeTrackerDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
        _dbContextOptions = optionsBuilder.Options;
        
        EnsureDatabase();
    }

    public void AddTrackingSession(TrackingSession session)
    {
        using TimeTrackerDbContext dbContext = new(_dbContextOptions);

        foreach (TrackingSessionAppUsage appUsage in session.AppUsages.Where(item => item.Duration > TimeSpan.Zero))
        {
            if (appUsage.Id == Guid.Empty)
            {
                appUsage.Id = Guid.NewGuid();
            }

            appUsage.SessionId = session.Id;
        }

        dbContext.TrackingSessions.Add(session);
        dbContext.SaveChanges();
    }

    private void EnsureDatabase()
    {
        using TimeTrackerDbContext dbContext = new(_dbContextOptions);
        dbContext.Database.EnsureCreated();
    }
}
