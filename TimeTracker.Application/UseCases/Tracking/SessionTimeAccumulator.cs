using TimeTracker.Application.Models;

namespace TimeTracker.Application.UseCases.Tracking;

public static class SessionTimeAccumulator
{
    // Adds elapsed wall-clock time into total session time and current app bucket.
    public static void ApplyElapsedTime(TrackingSessionState state, DateTime now)
    {
        if (state.LastTickAt is null)
        {
            state.LastTickAt = now;
            return;
        }

        TimeSpan delta = now - state.LastTickAt.Value;
        if (delta <= TimeSpan.Zero)
        {
            return;
        }

        state.RecordedDuration += delta;

        if (state.SessionAppDurations.TryGetValue(state.LastTrackedAppName, out var previousDuration))
        {
            state.SessionAppDurations[state.LastTrackedAppName] = previousDuration + delta;
        }
        else
        {
            state.SessionAppDurations[state.LastTrackedAppName] = delta;
        }

        state.LastTickAt = now;
    }
}
