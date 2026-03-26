using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Services.Tracking;

public class StopTrackingUseCase
{
    // Stops tracking and persists one summarized session record.
    public void Execute(TrackingSessionState state, ITrackingSessionRepository activityLogStore, DateTime now)
    {
        if (state.State == TrackingState.Running)
        {
            SessionTimeAccumulator.ApplyElapsedTime(state, now);
        }

        state.LastTickAt = null;
        state.State = TrackingState.Stopped;

        PersistSessionIfAny(state, activityLogStore, now);
        state.SessionStartedAt = null;
    }

    private static void PersistSessionIfAny(TrackingSessionState state, ITrackingSessionRepository activityLogStore, DateTime endedAt)
    {
        if (!state.SessionStartedAt.HasValue)
        {
            return;
        }

        DateTime startedAt = state.SessionStartedAt.Value;
        if (endedAt <= startedAt)
        {
            return;
        }

        List<TrackingSessionAppUsage> appUsages = state.SessionAppDurations
            .Where(pair => pair.Value > TimeSpan.Zero)
            .Select(pair => new TrackingSessionAppUsage
            {
                AppName = pair.Key,
                Duration = pair.Value
            })
            .OrderByDescending(usage => usage.Duration)
            .ThenBy(usage => usage.AppName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        TrackingSession session = new()
        {
            SessionDate = DateOnly.FromDateTime(startedAt),
            StartedAt = startedAt,
            EndedAt = endedAt,
            TotalDuration = state.RecordedDuration,
            IdleDuration = state.IdleDuration,
            AppUsages = appUsages
        };

        activityLogStore.AddTrackingSession(session);
    }
}
