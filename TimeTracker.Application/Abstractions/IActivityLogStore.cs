using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Abstractions;

public interface IActivityLogStore
{
    void AddTrackingSession(TrackingSession session);
}
