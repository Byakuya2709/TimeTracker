using TimeTracker.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace TimeTracker.Domain.Interfaces;

public interface ITrackingSessionRepository
{
    Task AddTrackingSessionAsync(TrackingSession session, CancellationToken cancellationToken = default);

    IReadOnlyList<TrackingSession> GetTrackingSessions();

    IReadOnlyList<TrackingSession> GetTrackingSessionsByWeeks(DateOnly dateInWeek);

    bool DeleteTrackingSession(Guid sessionId);
}
