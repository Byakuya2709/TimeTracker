namespace TimeTracker.Application.Models;

public enum TrackingState
{
    Stopped,
    Running,
    Paused
}

public class AppUsage
{
    public string AppName { get; init; } = "Ứng dụng không xác định";

    public TimeSpan Duration { get; init; }
}

public class TrackingSnapshot
{
    public string CurrentAppName { get; init; } = "Đang khởi tạo...";

    public TimeSpan TotalRecorded { get; init; }

    public TimeSpan IdleDuration { get; init; }

    public int FocusScore { get; init; }

    public string FocusSummary { get; init; } = "Điểm hiệu suất 0/100 - Đang chờ dữ liệu";

    public string SuggestionMessage { get; init; } = string.Empty;

    public TrackingState State { get; init; }

    public IReadOnlyList<AppUsage> TopApps { get; init; } = [];
}
