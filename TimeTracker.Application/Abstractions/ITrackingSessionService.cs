using TimeTracker.Application.Models;

namespace TimeTracker.Application.Abstractions;

public interface ITrackingSessionService
{
    IReadOnlyList<TrackingSessionModel> GetTrackingSessionsByWeek(DateOnly dateInWeek);

    bool DeleteTrackingSession(Guid sessionId);
}
