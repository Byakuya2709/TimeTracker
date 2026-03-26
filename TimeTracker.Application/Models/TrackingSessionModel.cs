namespace TimeTracker.Application.Models;

public sealed class TrackingSessionAppUsageModel
{
    public string AppName { get; init; } = string.Empty;

    public TimeSpan Duration { get; init; }
}

public sealed class TrackingSessionModel
{
    public Guid Id { get; init; }

    public DateOnly SessionDate { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime EndedAt { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public TimeSpan IdleDuration { get; init; }

    public IReadOnlyList<TrackingSessionAppUsageModel> AppUsages { get; init; } = [];
}
