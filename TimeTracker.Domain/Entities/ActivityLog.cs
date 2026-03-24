namespace TimeTracker.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AppName { get; set; } = "Unknown App";

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
}
