namespace TimeTracker.Application.Models;

public class TrackingSnapshot
{
    public string CurrentAppName { get; init; } = "Starting...";

    public TimeSpan Elapsed { get; init; }

    public int FocusScore { get; init; }

    public string FocusSummary { get; init; } = "Settling In";
}
