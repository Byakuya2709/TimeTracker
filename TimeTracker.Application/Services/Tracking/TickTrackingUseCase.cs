using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.Application.Services.Tracking;

public class TickTrackingUseCase
{
    // Executes one timer tick: accumulate elapsed time, switch app bucket if needed, then build snapshot.
    public TrackingSnapshot Execute(TrackingSessionState state, IActiveAppReader activeAppReader, DateTime now)
    {
        if (state.State != TrackingState.Running)
        {
            return BuildSnapshot(state, now);
        }

        SessionTimeAccumulator.ApplyElapsedTime(state, now);
        TrackForegroundAppTransition(state, activeAppReader, now);

        return BuildSnapshot(state, now);
    }

    private static void TrackForegroundAppTransition(TrackingSessionState state, IActiveAppReader activeAppReader, DateTime now)
    {
        string activeAppName = activeAppReader.GetActiveAppName();
        string nextTrackedAppName = TrackingRules.ResolveTrackedAppName(activeAppName);

        bool appChanged = !state.LastTrackedAppName.Equals(nextTrackedAppName, StringComparison.OrdinalIgnoreCase);
        if (appChanged)
        {
            state.LastTrackedAppName = nextTrackedAppName;
            state.LastAppChangedAt = now;
            // Only count meaningful switches, not idle
            if (!TrackingRules.IsIdleAppName(nextTrackedAppName) && !TrackingRules.IsUnassignedAppName(nextTrackedAppName))
            {
                state.AppSwitchCount++;
            }
        }
    }

    private static TrackingSnapshot BuildSnapshot(TrackingSessionState state, DateTime now)
    {
        int score = TrackingRules.CalculateFocusScore(state, now);

        return new TrackingSnapshot
        {
            CurrentAppName = TrackingRules.GetDisplayAppName(state),
            FocusScore = score,
            FocusSummary = TrackingRules.GetFocusSummary(score, state.RecordedDuration, state.IdleDuration),
            Notification = TrackingRules.GetSuggestionMessage(state.RecordedDuration, score, state.LastTrackedAppName)
        };
    }
}
