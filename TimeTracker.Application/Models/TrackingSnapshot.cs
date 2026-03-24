namespace TimeTracker.Application.Models;

public enum TrackingState
{
    Stopped,
    Running,
    Paused
}

public class AppUsage
{
    public string AppName { get; init; } = "Unknown App";

    public TimeSpan Duration { get; init; }
}

public class TrackingSnapshot
{
    public string CurrentAppName { get; init; } = "Starting...";

    public TimeSpan TotalRecorded { get; init; }

    public int FocusScore { get; init; }

    public string FocusSummary { get; init; } = "Settling In";

    public string SuggestionMessage { get; init; } = string.Empty;

    public TrackingState State { get; init; }

    public IReadOnlyList<AppUsage> TopApps { get; init; } = [];
}
