using System.Windows.Input;
using System.Windows.Threading;
using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan TrackingSnapshotRefreshInterval = TimeSpan.FromSeconds(3);

    private readonly ITrackingRuntimeService _activityTracker;
    private readonly ITrackingSessionService _trackingSessionService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly DispatcherTimer _timer;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _stopCommand;
    private readonly RelayCommand _showOverviewCommand;
    private readonly RelayCommand _showSessionsCommand;
    private readonly RelayCommand _showSettingsCommand;

    private DashboardPage _currentPage = DashboardPage.Overview;
    private bool _isRecording;
    private bool _isPaused;
    private bool _isStopped = true;
    private bool _hasLoadedSessions;
    private TimeSpan _elapsedDisplayBase = TimeSpan.Zero;
    private DateTime? _elapsedDisplayRunningSince;
    private DateTime? _lastSnapshotRefreshAt;

    public ICommand StartCommand => _startCommand;

    public ICommand PauseCommand => _pauseCommand;

    public ICommand StopCommand => _stopCommand;

    public ICommand ShowOverviewCommand => _showOverviewCommand;

    public ICommand ShowSessionsCommand => _showSessionsCommand;

    public ICommand ShowSettingsCommand => _showSettingsCommand;

    public MainViewModel(
        ITrackingRuntimeService activityTracker,
        ITrackingSessionService trackingSessionService,
        IUserSettingsService userSettingsService)
    {
        _activityTracker = activityTracker;
        _trackingSessionService = trackingSessionService;
        _userSettingsService = userSettingsService;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += OnTimerTick;
        
        _startCommand = new RelayCommand(StartTracking, () => !_isRecording);
        _pauseCommand = new RelayCommand(PauseTracking, () => _isRecording);
        _stopCommand = new RelayCommand(StopTracking, () => !_isStopped);
        _showOverviewCommand = new RelayCommand(() => SetCurrentPage(DashboardPage.Overview));
        _showSessionsCommand = new RelayCommand(() => SetCurrentPage(DashboardPage.Sessions));
        _showSettingsCommand = new RelayCommand(() => SetCurrentPage(DashboardPage.Settings));

        InitializeSessionsPageCommands();
        InitializeSettingsPageCommands();
        LoadUserSettings();

        _timer.Start();
    }

    public DashboardPage CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsOverviewPage));
                OnPropertyChanged(nameof(IsSessionsPage));
                OnPropertyChanged(nameof(IsSettingsPage));
                OnPropertyChanged(nameof(PageTitle));
            }
        }
    }

    public bool IsOverviewPage => CurrentPage == DashboardPage.Overview;

    public bool IsSessionsPage => CurrentPage == DashboardPage.Sessions;

    public bool IsSettingsPage => CurrentPage == DashboardPage.Settings;

    public string PageTitle => CurrentPage switch
    {
        DashboardPage.Overview => "Tổng quan",
        DashboardPage.Sessions => "Lịch sử",
        DashboardPage.Settings => "Thiết lập",
        _ => "Bảng điều khiển"
    };

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

        if (IsSessionsPage && _hasLoadedSessions)
        {
            LoadTrackingSessions();
        }

        CurrentAppName = "Đang khởi tạo...";
        FocusScore = 0;
        FocusSummary = "Điểm hiệu suất 0/100 - Đang chờ dữ liệu";
        Notification = string.Empty;
        OnPropertyChanged(nameof(HasNotification));
    }

    private void SetCurrentPage(DashboardPage page)
    {
        CurrentPage = page;

        if (page == DashboardPage.Sessions)
        {
            EnsureSessionsLoaded();
        }
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

    private void EnsureSessionsLoaded()
    {
        if (_hasLoadedSessions)
        {
            return;
        }

        LoadTrackingSessions();
        _hasLoadedSessions = true;
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
        OnPropertyChanged(nameof(HasNotification));

        _lastSnapshotRefreshAt = now;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _activityTracker.Stop();
    }

    private partial void InitializeSessionsPageCommands();

    private partial void InitializeSettingsPageCommands();

    private partial void LoadTrackingSessions(string? customStatusMessage = null);

    private partial void OnTimerTick(object? sender, EventArgs e);
}
