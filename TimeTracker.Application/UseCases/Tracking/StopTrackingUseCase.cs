using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.Application.UseCases.Tracking;

public class StopTrackingUseCase
{
    // Stops tracking and closes the current persisted activity if needed.
    public void Execute(TrackingSessionState state, IActivityLogStore activityLogStore, DateTime now)
    {
        if (state.State == TrackingState.Running)
        {
            SessionTimeAccumulator.ApplyElapsedTime(state, now);
            TrackingPersistence.CloseCurrentActivity(state, activityLogStore, now);
        }

        state.LastTickAt = null;
        state.CurrentActivity = null;
        state.State = TrackingState.Stopped;
    }
}
