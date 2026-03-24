using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.Application.UseCases.Tracking;

public class TickTrackingUseCase
{
    // Executes one timer tick: accumulate elapsed time, switch app bucket if needed, then build snapshot.
    public TrackingSnapshot Execute(TrackingSessionState state, IActiveAppReader activeAppReader, IActivityLogStore activityLogStore, DateTime now)
    {
        if (state.State != TrackingState.Running)
        {
            return BuildSnapshot(state);
        }

        SessionTimeAccumulator.ApplyElapsedTime(state, now);
        TrackForegroundAppTransition(state, activeAppReader, activityLogStore, now);

        return BuildSnapshot(state);
    }

    private static void TrackForegroundAppTransition(TrackingSessionState state, IActiveAppReader activeAppReader, IActivityLogStore activityLogStore, DateTime now)
    {
        string activeAppName = activeAppReader.GetActiveAppName();
        string nextTrackedAppName = TrackingRules.ResolveTrackedAppName(activeAppName);

        bool appChanged = !state.LastTrackedAppName.Equals(nextTrackedAppName, StringComparison.OrdinalIgnoreCase);
        if (appChanged)
        {
            SwitchTrackedApp(state, activityLogStore, now, nextTrackedAppName);
            return;
        }

        // Re-open persistence row when current app is persistable but no open row exists.
        if (state.CurrentActivity == null && TrackingRules.CanPersistActivity(state.LastTrackedAppName))
        {
            TrackingPersistence.StartNewActivity(state, activityLogStore, state.LastTrackedAppName, now);
        }
    }

    private static void SwitchTrackedApp(TrackingSessionState state, IActivityLogStore activityLogStore, DateTime now, string nextTrackedAppName)
    {
        TrackingPersistence.CloseCurrentActivity(state, activityLogStore, now);
        state.LastTrackedAppName = nextTrackedAppName;

        if (TrackingRules.CanPersistActivity(nextTrackedAppName))
        {
            TrackingPersistence.StartNewActivity(state, activityLogStore, nextTrackedAppName, now);
            return;
        }

        state.CurrentActivity = null;
    }

    private static TrackingSnapshot BuildSnapshot(TrackingSessionState state)
    {
        TimeSpan currentAppElapsed = state.SessionAppDurations.TryGetValue(state.LastTrackedAppName, out var trackedDuration)
            ? trackedDuration
            : TimeSpan.Zero;

        int score = TrackingRules.CalculateFocusScore(state.LastTrackedAppName, currentAppElapsed);

        IReadOnlyList<AppUsage> topApps = state.SessionAppDurations
            .Where(pair => !pair.Key.Equals(TrackingRules.UnassignedAppName, StringComparison.OrdinalIgnoreCase))
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
            FocusScore = score,
            FocusSummary = TrackingRules.GetFocusSummary(score),
            SuggestionMessage = TrackingRules.GetSuggestionMessage(state.RecordedDuration, score),
            State = state.State,
            TopApps = topApps
        };
    }
}
