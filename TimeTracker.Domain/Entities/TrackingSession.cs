namespace TimeTracker.Domain.Entities;

public class TrackingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateOnly SessionDate { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime EndedAt { get; set; }

    public TimeSpan TotalDuration { get; set; }

    public TimeSpan IdleDuration { get; set; }

    public List<TrackingSessionAppUsage> AppUsages { get; set; } = [];
}
