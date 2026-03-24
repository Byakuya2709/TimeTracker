using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.Application.UseCases.Tracking;

public class PauseTrackingUseCase
{
    // Pauses active tracking and safely closes any open persisted activity row.
    public void Execute(TrackingSessionState state, IActivityLogStore activityLogStore, DateTime now)
    {
        if (state.State != TrackingState.Running)
        {
            return;
        }

        SessionTimeAccumulator.ApplyElapsedTime(state, now);
        TrackingPersistence.CloseCurrentActivity(state, activityLogStore, now);
        state.State = TrackingState.Paused;
    }
}
