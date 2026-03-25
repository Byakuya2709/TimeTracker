using TimeTracker.Application.Models;

namespace TimeTracker.Application.UseCases.Tracking;

public class PauseTrackingUseCase
{
    // Pauses active tracking and keeps in-memory session data for resume.
    public void Execute(TrackingSessionState state, DateTime now)
    {
        if (state.State != TrackingState.Running)
        {
            return;
        }

        SessionTimeAccumulator.ApplyElapsedTime(state, now);
        state.State = TrackingState.Paused;
    }
}
