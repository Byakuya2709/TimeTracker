using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.Application.UseCases.Tracking;

public class StartTrackingUseCase
{
    // Starts a new session (or resumes paused session) and initializes foreground app target.
    public void Execute(TrackingSessionState state, IActiveAppReader activeAppReader, DateTime now)
    {
        if (state.State == TrackingState.Running)
        {
            return;
        }

        if (state.State == TrackingState.Stopped)
        {
            ResetSession(state);
            state.SessionStartedAt = now;
        }

        string activeAppName = activeAppReader.GetActiveAppName();
        state.LastTrackedAppName = TrackingRules.ResolveTrackedAppName(activeAppName);
        state.LastTickAt = now;

        state.State = TrackingState.Running;
    }

    private static void ResetSession(TrackingSessionState state)
    {
        state.SessionAppDurations.Clear();
        state.RecordedDuration = TimeSpan.Zero;
        state.SessionStartedAt = null;
        state.LastTickAt = null;
        state.LastTrackedAppName = TrackingRules.UnassignedAppName;
    }
}
