using System.Windows.Threading;
using TimeTracker.Application.Models;
using TimeTracker.Application.Services;

namespace TimeTracker.App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ActivityTracker _activityTracker;
    private readonly DispatcherTimer _timer;

    private string _currentAppName = "Starting...";
    private string _elapsedTime = "00:00:00";
    private int _focusScore;
    private string _focusSummary = "Settling In";

    public MainViewModel(ActivityTracker activityTracker)
    {
        _activityTracker = activityTracker;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

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

    public string FocusScoreText => $"{FocusScore}%";

    public string FocusSummary
    {
        get => _focusSummary;
        private set => SetProperty(ref _focusSummary, value);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        TrackingSnapshot snapshot = _activityTracker.Tick();
        CurrentAppName = snapshot.CurrentAppName;
        ElapsedTime = snapshot.Elapsed.ToString(@"hh\:mm\:ss");
        FocusScore = snapshot.FocusScore;
        FocusSummary = snapshot.FocusSummary;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _activityTracker.Stop();
    }
}
