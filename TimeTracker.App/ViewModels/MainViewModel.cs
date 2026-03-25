using System.Windows.Threading;
using TimeTracker.Application.Models;
using TimeTracker.Application.Services;
using System.Windows.Input;

namespace TimeTracker.App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ActivityTracker _activityTracker;
    private readonly DispatcherTimer _timer;

    private string _currentAppName = "Starting...";
    private string _elapsedTime = "00:00:00";
    private string _idleTime = "00:00:00";
    private string _suggestionMessage = string.Empty;
    private string _topAppsSummary = "1. -- 00:00\n2. -- 00:00\n3. -- 00:00";
    private int _focusScore;
    private string _focusSummary = "Settling In";
    private bool _isRecording;
    private bool _isPaused;
    private bool _isStopped = true;

    public ICommand StartCommand { get; }

    public ICommand PauseCommand { get; }

    public ICommand StopCommand
    {
        get;
    }
    public MainViewModel(ActivityTracker activityTracker)
    {
        _activityTracker = activityTracker;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += OnTimerTick;
        StartCommand = new RelayCommand(StartTracking, () => !_isRecording);
        PauseCommand = new RelayCommand(PauseTracking, () => _isRecording);
        StopCommand = new RelayCommand(StopTracking, () => !_isStopped);
        _timer.Start();
    }


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

    public string FocusScoreText => $"{FocusScore}%";

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

    public bool IsRecording
    {
        get => _isRecording;
        private set => SetProperty(ref _isRecording, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set => SetProperty(ref _isPaused, value);
    }

    public bool IsStopped
    {
        get => _isStopped;
        private set => SetProperty(ref _isStopped, value);
    }

    private void StartTracking()
    {
        _activityTracker.Start();
        UpdateActionState(TrackingState.Running);
    }

    private void PauseTracking()
    {
        _activityTracker.Pause();
        UpdateActionState(TrackingState.Paused);
    }

    private void StopTracking()
    {
        _activityTracker.Stop();
        UpdateActionState(TrackingState.Stopped);
    }

    private void UpdateActionState(TrackingState state)
    {
        IsRecording = state == TrackingState.Running;
        IsPaused = state == TrackingState.Paused;
        IsStopped = state == TrackingState.Stopped;

        ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PauseCommand).RaiseCanExecuteChanged();
        ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
    }

    private void OnTimerTick(object? sender, EventArgs e)
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

    private static string BuildTopAppsSummary(IReadOnlyList<AppUsage> topApps)
    {
        string[] lines = new string[3];

        for (int i = 0; i < lines.Length; i++)
        {
            if (i < topApps.Count)
            {
                AppUsage usage = topApps[i];
                lines[i] = $"{i + 1}. {usage.AppName} {FormatDuration(usage.Duration)}";
            }
            else
            {
                lines[i] = $"{i + 1}. -- 00:00";
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

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _activityTracker.Stop();
    }
}
