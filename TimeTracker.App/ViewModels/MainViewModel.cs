using System.Windows.Input;
using System.Windows.Threading;
using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
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

        LoadTrackingSessions();
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
        DashboardPage.Sessions => "Lịch Sử",
        DashboardPage.Settings => "Thiết Lập",
        _ => "Dashboard"
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
        LoadTrackingSessions();
    }

    private void SetCurrentPage(DashboardPage page)
    {
        CurrentPage = page;

        if (page == DashboardPage.Sessions)
        {
            LoadTrackingSessions();
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
