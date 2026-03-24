using TimeTracker.Application.UseCases.Tracking;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Models;

public class TrackingSessionState
{
    public ActivityLog? CurrentActivity { get; set; }

    public Dictionary<string, TimeSpan> SessionAppDurations { get; } = new(StringComparer.OrdinalIgnoreCase);

    public DateTime? LastTickAt { get; set; }

    public string LastTrackedAppName { get; set; } = TrackingRules.UnassignedAppName;

    public TimeSpan RecordedDuration { get; set; } = TimeSpan.Zero;

    public TrackingState State { get; set; } = TrackingState.Stopped;
}
