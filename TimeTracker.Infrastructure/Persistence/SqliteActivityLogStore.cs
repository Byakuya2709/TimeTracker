using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence;

public class SqliteActivityLogStore : ITrackingSessionRepository
{
    private readonly IDbContextFactory<TimeTrackerDbContext> _dbContextFactory;

    public SqliteActivityLogStore(IDbContextFactory<TimeTrackerDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddTrackingSessionAsync(TrackingSession session, CancellationToken cancellationToken = default)
    {
        using TimeTrackerDbContext dbContext = _dbContextFactory.CreateDbContext();

        foreach (TrackingSessionAppUsage appUsage in session.AppUsages.Where(item => item.Duration > TimeSpan.Zero))
        {
            if (appUsage.Id == Guid.Empty)
            {
                appUsage.Id = Guid.NewGuid();
            }

            appUsage.SessionId = session.Id;
        }

        dbContext.TrackingSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public IReadOnlyList<TrackingSession> GetTrackingSessions()
    {
        using TimeTrackerDbContext dbContext = _dbContextFactory.CreateDbContext();

        return dbContext
            .TrackingSessions
            .Include(item => item.AppUsages)
            .OrderByDescending(item => item.StartedAt)
            .ToList();
    }

    public IReadOnlyList<TrackingSession> GetTrackingSessionsByWeeks(DateOnly dateInWeek)
    {
        using TimeTrackerDbContext dbContext = _dbContextFactory.CreateDbContext();

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
        using TimeTrackerDbContext dbContext = _dbContextFactory.CreateDbContext();

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

}
