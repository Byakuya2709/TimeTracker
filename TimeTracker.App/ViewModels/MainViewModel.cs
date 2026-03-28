using System.Windows.Input;
using TimeTracker.Application.Abstractions;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ITrackingRuntimeService _activityTracker;
    private readonly ITrackingSessionService _trackingSessionService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly RelayCommand _showOverviewCommand;
    private readonly RelayCommand _showSessionsCommand;
    private readonly RelayCommand _showSettingsCommand;

    private DashboardPage _currentPage = DashboardPage.Overview;
    private bool _hasLoadedSessions;

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
        _showOverviewCommand = new RelayCommand(() => SetCurrentPage(DashboardPage.Overview));
        _showSessionsCommand = new RelayCommand(() => SetCurrentPage(DashboardPage.Sessions));
        _showSettingsCommand = new RelayCommand(() => SetCurrentPage(DashboardPage.Settings));

        InitializeSessionsPageCommands();
        InitializeSettingsPageCommands();
        LoadUserSettings();
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
        DashboardPage.Sessions => "Lịch sử Phiên theo dõi",
        DashboardPage.Settings => "Thiết lập",
        _ => "Bảng điều khiển"
    };

    private void SetCurrentPage(DashboardPage page)
    {
        CurrentPage = page;

        if (page == DashboardPage.Sessions)
        {
            EnsureSessionsLoaded();
        }
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
    public void Dispose()
    {
        SelectedTrackingSession = null;
        SessionDayGroups.Clear();
        _weekSessions.Clear();
        _hasLoadedSessions = false;
    }

    private partial void InitializeSessionsPageCommands();

    private partial void InitializeSettingsPageCommands();

    private partial void LoadTrackingSessions(string? customStatusMessage = null);
}
