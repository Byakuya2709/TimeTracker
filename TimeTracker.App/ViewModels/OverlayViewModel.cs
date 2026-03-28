using System.Windows.Input;
using System.Windows.Threading;
using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.App.ViewModels;

public sealed class OverlayViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan TrackingSnapshotRefreshInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan OverlaySettingsRefreshInterval = TimeSpan.FromSeconds(2);

    private readonly ITrackingRuntimeService _activityTracker;
    private readonly IUserSettingsService _userSettingsService;
    private readonly DispatcherTimer _timer;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _stopCommand;

    private bool _isRecording;
    private bool _isPaused;
    private bool _isStopped = true;
    private TimeSpan _elapsedDisplayBase = TimeSpan.Zero;
    private DateTime? _elapsedDisplayRunningSince;
    private DateTime? _lastSnapshotRefreshAt;
    private DateTime? _lastSettingsRefreshAt;

    private string _currentAppName = "Dang khoi tao...";
    private string _elapsedTime = "00:00:00";
    private string _notification = string.Empty;
    private int _focusScore;
    private string _focusSummary = "Diem hieu suat 0/100 - Dang cho du lieu";
    private int _overlayOpacity = 85;
    private OverlayAnchor _overlayPosition = OverlayAnchor.TopRight;

    public OverlayViewModel(
        ITrackingRuntimeService activityTracker,
        IUserSettingsService userSettingsService)
    {
        _activityTracker = activityTracker;
        _userSettingsService = userSettingsService;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += OnTimerTick;

        _startCommand = new RelayCommand(StartTracking, () => !_isRecording);
        _pauseCommand = new RelayCommand(PauseTracking, () => _isRecording);
        _stopCommand = new RelayCommand(StopTracking, () => !_isStopped);

        LoadUserSettings();
        _timer.Start();
    }

    public ICommand StartCommand => _startCommand;

    public ICommand PauseCommand => _pauseCommand;

    public ICommand StopCommand => _stopCommand;

    public string Notification
    {
        get => _notification;
        private set
        {
            if (SetProperty(ref _notification, value))
            {
                OnPropertyChanged(nameof(HasNotification));
            }
        }
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

    public int OverlayOpacity
    {
        get => _overlayOpacity;
        private set
        {
            int normalized = Math.Clamp(value, 10, 100);
            if (SetProperty(ref _overlayOpacity, normalized))
            {
                OnPropertyChanged(nameof(OverlayOpacityRatio));
            }
        }
    }

    public double OverlayOpacityRatio => OverlayOpacity / 100d;

    public OverlayAnchor OverlayPosition
    {
        get => _overlayPosition;
        private set => SetProperty(ref _overlayPosition, value);
    }

    private void StartTracking()
    {
        _activityTracker.Start();
        UpdateActionState(TrackingState.Running);
        StartElapsedDisplayClock(DateTime.Now);
        RefreshTrackingSnapshot(forceRefresh: true, DateTime.Now);
    }

    private void PauseTracking()
    {
        _activityTracker.Pause();
        PauseElapsedDisplayClock(DateTime.Now);
        UpdateActionState(TrackingState.Paused);
    }

    private void StopTracking()
    {
        _activityTracker.Stop();
        ResetElapsedDisplayClock();
        UpdateActionState(TrackingState.Stopped);

        CurrentAppName = "Dang khoi tao...";
        FocusScore = 0;
        FocusSummary = "Diem hieu suat 0/100 - Dang cho du lieu";
        Notification = string.Empty;
    }

    private void UpdateActionState(TrackingState state)
    {
        IsRecording = state == TrackingState.Running;
        IsPaused = state == TrackingState.Paused;
        IsStopped = state == TrackingState.Stopped;

        _startCommand.RaiseCanExecuteChanged();
        _pauseCommand.RaiseCanExecuteChanged();
        _stopCommand.RaiseCanExecuteChanged();
    }

    private void StartElapsedDisplayClock(DateTime now)
    {
        _elapsedDisplayRunningSince = now;
    }

    private void PauseElapsedDisplayClock(DateTime now)
    {
        if (_elapsedDisplayRunningSince is not null)
        {
            TimeSpan delta = now - _elapsedDisplayRunningSince.Value;
            if (delta > TimeSpan.Zero)
            {
                _elapsedDisplayBase += delta;
            }
        }

        _elapsedDisplayRunningSince = null;
        ElapsedTime = _elapsedDisplayBase.ToString(@"hh\:mm\:ss");
    }

    private void ResetElapsedDisplayClock()
    {
        _elapsedDisplayBase = TimeSpan.Zero;
        _elapsedDisplayRunningSince = null;
        ElapsedTime = _elapsedDisplayBase.ToString(@"hh\:mm\:ss");
    }

    private void UpdateElapsedDisplay(DateTime now)
    {
        TimeSpan elapsed = _elapsedDisplayBase;

        if (_elapsedDisplayRunningSince is not null)
        {
            TimeSpan delta = now - _elapsedDisplayRunningSince.Value;
            if (delta > TimeSpan.Zero)
            {
                elapsed += delta;
            }
        }

        ElapsedTime = elapsed.ToString(@"hh\:mm\:ss");
    }

    private void RefreshTrackingSnapshot(bool forceRefresh, DateTime now)
    {
        if (!forceRefresh
            && _lastSnapshotRefreshAt is not null
            && now - _lastSnapshotRefreshAt.Value < TrackingSnapshotRefreshInterval)
        {
            return;
        }

        TrackingSnapshot snapshot = _activityTracker.Tick();
        CurrentAppName = snapshot.CurrentAppName;
        FocusScore = snapshot.FocusScore;
        FocusSummary = snapshot.FocusSummary;
        Notification = snapshot.Notification;

        _lastSnapshotRefreshAt = now;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        DateTime now = DateTime.Now;

        RefreshOverlaySettingsIfNeeded(now);

        if (!IsRecording)
        {
            return;
        }

        UpdateElapsedDisplay(now);
        RefreshTrackingSnapshot(forceRefresh: false, now);
    }

    private void LoadUserSettings()
    {
        try
        {
            UserSettingsModel settings = _userSettingsService.GetUserSettings();
            ApplyOverlaySettings(settings);
        }
        catch
        {
            // Keep in-memory defaults when persistence is unavailable.
        }
    }

    private void RefreshOverlaySettingsIfNeeded(DateTime now)
    {
        if (_lastSettingsRefreshAt is not null
            && now - _lastSettingsRefreshAt.Value < OverlaySettingsRefreshInterval)
        {
            return;
        }

        _lastSettingsRefreshAt = now;

        try
        {
            UserSettingsModel settings = _userSettingsService.GetUserSettings();
            ApplyOverlaySettings(settings);
        }
        catch
        {
            // Skip refresh errors to keep overlay responsive.
        }
    }

    private void ApplyOverlaySettings(UserSettingsModel settings)
    {
        OverlayOpacity = settings.OverlayOpacity;
        OverlayPosition = ParseOverlayAnchor(settings.OverlayPosition);
    }

    private static OverlayAnchor ParseOverlayAnchor(string? value)
    {
        return Enum.TryParse(value, ignoreCase: true, out OverlayAnchor parsed)
            ? parsed
            : OverlayAnchor.TopRight;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _activityTracker.Stop();
    }
}
