namespace TimeTracker.Domain.Entities;

public class TrackingSessionAppUsage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessionId { get; set; }

    public string AppName { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public TrackingSession? Session { get; set; }
}
