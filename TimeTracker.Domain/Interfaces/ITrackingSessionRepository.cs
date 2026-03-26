using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface ITrackingSessionRepository
{
    void AddTrackingSession(TrackingSession session);

    IReadOnlyList<TrackingSession> GetTrackingSessions();

    IReadOnlyList<TrackingSession> GetTrackingSessionsByWeeks(DateOnly dateInWeek);

    bool DeleteTrackingSession(Guid sessionId);
}
