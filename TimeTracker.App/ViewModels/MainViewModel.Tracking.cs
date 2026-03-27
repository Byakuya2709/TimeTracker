using TimeTracker.Application.Models;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel
{
    private string _currentAppName = "Đang khởi tạo...";
    private string _elapsedTime = "00:00:00";
    private string _suggestionMessage = string.Empty;
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

    private partial void OnTimerTick(object? sender, EventArgs e)
    {
        DateTime now = DateTime.Now;
        UpdateElapsedDisplay(now);
        RefreshTrackingSnapshot(forceRefresh: false, now);
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
