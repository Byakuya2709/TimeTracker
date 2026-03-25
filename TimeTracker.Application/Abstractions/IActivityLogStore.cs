using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Abstractions;

public interface IActivityLogStore
{
    void AddTrackingSession(TrackingSession session);

    IReadOnlyList<TrackingSession> GetTrackingSessions();

    IReadOnlyList<TrackingSession> GetTrackingSessionsByWeeks(DateOnly dateInWeek);

    bool DeleteTrackingSession(Guid sessionId);
}
