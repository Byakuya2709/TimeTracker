using TimeTracker.Application.Models;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel
{
    private string _currentAppName = "Đang khởi tạo...";
    private string _elapsedTime = "00:00:00";
    private string _idleTime = "00:00:00";
    private string _suggestionMessage = string.Empty;
    private string _topAppsSummary = "1. -- 00:00\n2. -- 00:00\n3. -- 00:00";
    private int _focusScore;
    private string _focusSummary = "Điểm hiệu suất 0/100 - Đang chờ dữ liệu";

    public string Notification
    {
        get => _suggestionMessage;
        private set => SetProperty(ref _suggestionMessage, value);
    }

    public bool HasNotification => !string.IsNullOrWhiteSpace(Notification);

    public string CurrentAppName
    {
        get => _currentAppName;
        private set => SetProperty(ref _currentAppName, value);
    }

    public string ElapsedTime
    {
        get => _elapsedTime;
        private set => SetProperty(ref _elapsedTime, value);
    }

    public string IdleTime
    {
        get => _idleTime;
        private set => SetProperty(ref _idleTime, value);
    }

    public int FocusScore
    {
        get => _focusScore;
        private set
        {
            if (SetProperty(ref _focusScore, value))
            {
                OnPropertyChanged(nameof(FocusScoreText));
            }
        }
    }

    public string FocusScoreText => $"{FocusScore}/100";

    public string FocusSummary
    {
        get => _focusSummary;
        private set => SetProperty(ref _focusSummary, value);
    }

    public string TopAppsSummary
    {
        get => _topAppsSummary;
        private set => SetProperty(ref _topAppsSummary, value);
    }

    private partial void OnTimerTick(object? sender, EventArgs e)
    {
        TrackingSnapshot snapshot = _activityTracker.Tick();

        UpdateActionState(snapshot.State);
        CurrentAppName = snapshot.CurrentAppName;
        ElapsedTime = snapshot.TotalRecorded.ToString(@"hh\:mm\:ss");
        IdleTime = snapshot.IdleDuration.ToString(@"hh\:mm\:ss");
        FocusScore = snapshot.FocusScore;
        FocusSummary = snapshot.FocusSummary;
        Notification = snapshot.SuggestionMessage;
        OnPropertyChanged(nameof(HasNotification));
        TopAppsSummary = BuildTopAppsSummary(snapshot.TopApps);
    }

    private static string FormatDuration2(TimeSpan duration)
    {
        // Nếu giờ = 0 thì chỉ hiện phút
        if (duration.TotalHours < 1)
            return $"{duration.Minutes}m";

        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }

    private static string BuildTopAppsSummary(IReadOnlyList<AppUsage> topApps)
    {
        string[] lines = new string[3];

        for (int i = 0; i < lines.Length; i++)
        {
            if (i < topApps.Count)
            {
                AppUsage usage = topApps[i];
                lines[i] = $"{i + 1}. {usage.AppName} : {FormatDuration2(usage.Duration)}";
            }
            else
            {
                lines[i] = $"{i + 1}. -- 0m"; // 0h thì bỏ, chỉ hiện 0 phút
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return duration.ToString(@"hh\:mm\:ss");
        }

        return duration.ToString(@"mm\:ss");
    }
}
