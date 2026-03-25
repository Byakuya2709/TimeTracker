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

    public IReadOnlyList<TrackingSession> GetTrackingSessions()
    {
        using TimeTrackerDbContext dbContext = new(_dbContextOptions);

        return dbContext
            .TrackingSessions
            .Include(item => item.AppUsages)
            .OrderByDescending(item => item.StartedAt)
            .ToList();
    }

    public IReadOnlyList<TrackingSession> GetTrackingSessionsByWeeks(DateOnly dateInWeek)
    {
        using TimeTrackerDbContext dbContext = new(_dbContextOptions);

        int mondayOffset = ((int)dateInWeek.DayOfWeek + 6) % 7;
        DateOnly weekStart = dateInWeek.AddDays(-mondayOffset);
        DateOnly weekEnd = weekStart.AddDays(6);

        return dbContext
            .TrackingSessions
            .Include(item => item.AppUsages)
            .Where(item => item.SessionDate >= weekStart && item.SessionDate <= weekEnd)
            .OrderByDescending(item => item.StartedAt)
            .ToList();
    }

    public bool DeleteTrackingSession(Guid sessionId)
    {
        using TimeTrackerDbContext dbContext = new(_dbContextOptions);

        TrackingSession? session = dbContext.TrackingSessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null)
        {
            return false;
        }

        List<TrackingSessionAppUsage> appUsages = dbContext
            .TrackingSessionAppUsages
            .Where(item => item.SessionId == sessionId)
            .ToList();

        if (appUsages.Count > 0)
        {
            dbContext.TrackingSessionAppUsages.RemoveRange(appUsages);
        }

        dbContext.TrackingSessions.Remove(session);
        dbContext.SaveChanges();
        return true;
    }

    private void EnsureDatabase()
    {
        using TimeTrackerDbContext dbContext = new(_dbContextOptions);
        dbContext.Database.EnsureCreated();
    }
}
