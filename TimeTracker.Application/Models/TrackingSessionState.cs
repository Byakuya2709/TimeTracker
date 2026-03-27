using TimeTracker.Application.Services.Tracking;

namespace TimeTracker.Application.Models;

public class TrackingSessionState
{
    public DateTime? SessionStartedAt { get; set; }

    public Dictionary<string, TimeSpan> SessionAppDurations { get; } = new(StringComparer.OrdinalIgnoreCase);

    public DateTime? LastTickAt { get; set; }

    public DateTime? LastAppTransitionCheckAt { get; set; }

    public string LastTrackedAppName { get; set; } = TrackingRules.UnassignedAppName;

    public TimeSpan RecordedDuration { get; set; } = TimeSpan.Zero;

    public TimeSpan IdleDuration => SessionAppDurations.TryGetValue(TrackingRules.IdleAppName, out TimeSpan value)
        ? value
        : TimeSpan.Zero;

    public TrackingState State { get; set; } = TrackingState.Stopped;
}
