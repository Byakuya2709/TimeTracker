using TimeTracker.Application.Services;
using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;
using System.Windows.Input;
using System.Windows.Threading;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ActivityTracker _activityTracker;
    private readonly IActivityLogStore _activityLogStore;
    private readonly DispatcherTimer _timer;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _stopCommand;
    private readonly RelayCommand _showOverviewCommand;
    private readonly RelayCommand _showSessionsCommand;

    private DashboardPage _currentPage = DashboardPage.Overview;
    private bool _isRecording;
    private bool _isPaused;
    private bool _isStopped = true;

    public ICommand StartCommand => _startCommand;

    public ICommand PauseCommand => _pauseCommand;

    public ICommand StopCommand => _stopCommand;

    public ICommand ShowOverviewCommand => _showOverviewCommand;

    public ICommand ShowSessionsCommand => _showSessionsCommand;

    public MainViewModel(ActivityTracker activityTracker, IActivityLogStore activityLogStore)
    {
        _activityTracker = activityTracker;
        _activityLogStore = activityLogStore;
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

        InitializeSessionsPageCommands();

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
            }
        }
    }

    public bool IsOverviewPage => CurrentPage == DashboardPage.Overview;

    public bool IsSessionsPage => CurrentPage == DashboardPage.Sessions;

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

    private partial void LoadTrackingSessions(string? customStatusMessage = null);

    private partial void OnTimerTick(object? sender, EventArgs e);
}
