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
            return BuildSnapshot(state);
        }

        SessionTimeAccumulator.ApplyElapsedTime(state, now);
        TrackForegroundAppTransition(state, activeAppReader);

        return BuildSnapshot(state);
    }

    private static void TrackForegroundAppTransition(TrackingSessionState state, IActiveAppReader activeAppReader)
    {
        string activeAppName = activeAppReader.GetActiveAppName();
        string nextTrackedAppName = TrackingRules.ResolveTrackedAppName(activeAppName);

        bool appChanged = !state.LastTrackedAppName.Equals(nextTrackedAppName, StringComparison.OrdinalIgnoreCase);
        if (appChanged)
        {
            state.LastTrackedAppName = nextTrackedAppName;
        }
    }

    private static TrackingSnapshot BuildSnapshot(TrackingSessionState state)
    {
        TimeSpan currentAppElapsed = state.SessionAppDurations.TryGetValue(state.LastTrackedAppName, out var trackedDuration)
            ? trackedDuration
            : TimeSpan.Zero;

        int score = TrackingRules.CalculateFocusScore(state.LastTrackedAppName, currentAppElapsed);

        IReadOnlyList<AppUsage> topApps = state.SessionAppDurations
            .Where(pair => !pair.Key.Equals(TrackingRules.UnassignedAppName, StringComparison.OrdinalIgnoreCase))
            .Where(pair => !pair.Key.Equals(TrackingRules.IdleAppName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(pair => new AppUsage
            {
                AppName = pair.Key,
                Duration = pair.Value
            })
            .ToList();

        return new TrackingSnapshot
        {
            CurrentAppName = TrackingRules.GetDisplayAppName(state),
            TotalRecorded = state.RecordedDuration,
            IdleDuration = state.IdleDuration,
            FocusScore = score,
            FocusSummary = TrackingRules.GetFocusSummary(score, state.RecordedDuration, state.IdleDuration),
            SuggestionMessage = TrackingRules.GetSuggestionMessage(state.RecordedDuration, score),
            State = state.State,
            TopApps = topApps
        };
    }
}
