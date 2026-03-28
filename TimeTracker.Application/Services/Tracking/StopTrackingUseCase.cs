using TimeTracker.Application.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using TimeTracker.Application.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Services.Tracking;

public class StopTrackingUseCase
{
    // Stops tracking and persists one summarized session record.
    public async Task ExecuteAsync(
        TrackingSessionState state,
        ITrackingSessionRepository activityLogStore,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (state.State == TrackingState.Running)
        {
            SessionTimeAccumulator.ApplyElapsedTime(state, now);
        }

        state.LastTickAt = null;
        state.LastAppTransitionCheckAt = null;
        state.State = TrackingState.Stopped;

        await PersistSessionIfAnyAsync(state, activityLogStore, now, cancellationToken);
        state.SessionStartedAt = null;
    }

    public void Execute(TrackingSessionState state, ITrackingSessionRepository activityLogStore, DateTime now)
    {
        ExecuteAsync(state, activityLogStore, now, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static async Task PersistSessionIfAnyAsync(
        TrackingSessionState state,
        ITrackingSessionRepository activityLogStore,
        DateTime endedAt,
        CancellationToken cancellationToken)
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

        await activityLogStore.AddTrackingSessionAsync(session, cancellationToken);
    }
}
