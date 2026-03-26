using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TimeTracker.Application.Models;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel
{
    private RelayCommand _refreshSessionsCommand = null!;
    private RelayCommand _deleteSelectedSessionCommand = null!;
    private RelayCommand<TrackingSessionListItem> _selectTrackingSessionCommand = null!;
    private readonly List<TrackingSessionListItem> _weekSessions = [];

    private string _sessionsStatusMessage = "No tracking sessions in this week.";
    private DateTime? _selectedWeekDate = DateTime.Today;
    private TrackingSessionListItem? _selectedTrackingSession;

    public ObservableCollection<TrackingSessionDayGroup> SessionDayGroups { get; } = [];

    public ICommand RefreshSessionsCommand => _refreshSessionsCommand;

    public ICommand DeleteSelectedSessionCommand => _deleteSelectedSessionCommand;

    public ICommand SelectTrackingSessionCommand => _selectTrackingSessionCommand;

    public string SessionsStatusMessage
    {
        get => _sessionsStatusMessage;
        private set => SetProperty(ref _sessionsStatusMessage, value);
    }

    public DateTime? SelectedWeekDate
    {
        get => _selectedWeekDate;
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            DateTime normalizedDate = value.Value.Date;
            if (SetProperty(ref _selectedWeekDate, normalizedDate))
            {
                OnPropertyChanged(nameof(WeekRangeTitle));
                LoadTrackingSessions();
            }
        }
    }

    public string WeekRangeTitle
    {
        get
        {
            DateOnly dateInWeek = DateOnly.FromDateTime((_selectedWeekDate ?? DateTime.Today).Date);
            int mondayOffset = ((int)dateInWeek.DayOfWeek + 6) % 7;
            DateOnly weekStart = dateInWeek.AddDays(-mondayOffset);
            DateOnly weekEnd = weekStart.AddDays(6);

            return $"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}";
        }
    }

    public TrackingSessionListItem? SelectedTrackingSession
    {
        get => _selectedTrackingSession;
        set
        {
            if (SetProperty(ref _selectedTrackingSession, value))
            {
                UpdateSessionSelectionState(value);
                _deleteSelectedSessionCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(HasSelectedTrackingSession));
                OnPropertyChanged(nameof(NoSelectedTrackingSession));
            }
        }
    }

    public bool HasSelectedTrackingSession => SelectedTrackingSession is not null;

    public bool NoSelectedTrackingSession => !HasSelectedTrackingSession;

    private partial void InitializeSessionsPageCommands()
    {
        _deleteSelectedSessionCommand = new RelayCommand(DeleteSelectedSession, () => SelectedTrackingSession is not null);
        _selectTrackingSessionCommand = new RelayCommand<TrackingSessionListItem>(SelectTrackingSession);
        _refreshSessionsCommand = new RelayCommand(() => LoadTrackingSessions());
    }

    private partial void LoadTrackingSessions(string? customStatusMessage)
    {
        DateOnly dateInWeek = DateOnly.FromDateTime((_selectedWeekDate ?? DateTime.Today).Date);
        IReadOnlyList<TrackingSessionModel> sessions = _trackingSessionService.GetTrackingSessionsByWeek(dateInWeek);

        _weekSessions.Clear();
        SessionDayGroups.Clear();

        foreach (TrackingSessionModel session in sessions)
        {
            _weekSessions.Add(MapToListItem(session));
        }

        IEnumerable<IGrouping<DateOnly, TrackingSessionListItem>> groupedSessions = _weekSessions
            .GroupBy(item => DateOnly.ParseExact(item.SessionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture))
            .OrderByDescending(group => group.Key);

        foreach (IGrouping<DateOnly, TrackingSessionListItem> group in groupedSessions)
        {
            SessionDayGroups.Add(new TrackingSessionDayGroup
            {
                SessionDate = group.Key,
                DateTitle = $"{group.Key:dddd, dd MMM yyyy}",
                Sessions = new ObservableCollection<TrackingSessionListItem>(group.OrderByDescending(item => item.StartedAt))
            });
        }

        SessionsStatusMessage = customStatusMessage ?? (_weekSessions.Count == 0
            ? "No tracking sessions in this week."
            : $"{_weekSessions.Count} session(s) found in selected week.");

        if (SelectedTrackingSession is not null)
        {
            SelectedTrackingSession = _weekSessions.FirstOrDefault(item => item.Id == SelectedTrackingSession.Id);
        }

        UpdateSessionSelectionState(SelectedTrackingSession);
    }

    private void DeleteSelectedSession()
    {
        if (SelectedTrackingSession is null)
        {
            return;
        }

        Guid sessionId = SelectedTrackingSession.Id;
        bool isDeleted;

        try
        {
            isDeleted = _trackingSessionService.DeleteTrackingSession(sessionId);
        }
        catch
        {
            LoadTrackingSessions("Could not delete session because of a database error.");
            return;
        }

        SelectedTrackingSession = null;
        LoadTrackingSessions(isDeleted
            ? "Tracking session deleted."
            : "Could not delete session. It may have been removed already.");
    }

    private void SelectTrackingSession(TrackingSessionListItem? session)
    {
        if (session is null)
        {
            return;
        }

        SelectedTrackingSession = session;
    }

    private void UpdateSessionSelectionState(TrackingSessionListItem? selectedSession)
    {
        Guid? selectedId = selectedSession?.Id;

        foreach (TrackingSessionListItem item in _weekSessions)
        {
            item.IsSelected = selectedId.HasValue && item.Id == selectedId.Value;
        }
    }

    private static TrackingSessionListItem MapToListItem(TrackingSessionModel session)
    {
        List<TrackingSessionAppUsageModel> topApps = session
            .AppUsages
            .OrderByDescending(item => item.Duration)
            .Take(3)
            .ToList();

        List<TrackingSessionAppUsageModel> allApps = session
            .AppUsages
            .OrderByDescending(item => item.Duration)
            .ToList();

        double totalAppSeconds = allApps.Sum(item => item.Duration.TotalSeconds);
        IReadOnlyList<AppUsageProgressItem> appUsageProgressItems = allApps
            .Select(item => new AppUsageProgressItem
            {
                AppName = item.AppName,
                Duration = FormatDuration(item.Duration),
                Percent = totalAppSeconds <= 0
                    ? 0
                    : Math.Round((item.Duration.TotalSeconds / totalAppSeconds) * 100, 1)
            })
            .ToList();

        string topAppsText = topApps.Count == 0
            ? "--"
            : string.Join(", ", topApps.Select(item => $"{item.AppName} ({FormatDuration(item.Duration)})"));

        string appBreakdown = allApps.Count == 0
            ? "No app activity details."
            : string.Join(Environment.NewLine, allApps.Select(item => $"- {item.AppName}: {FormatDuration(item.Duration)}"));

        string startedAt = session.StartedAt.ToString("HH:mm:ss");
        string endedAt = session.EndedAt.ToString("HH:mm:ss");

        return new TrackingSessionListItem
        {
            Id = session.Id,
            SessionDate = session.SessionDate.ToString("yyyy-MM-dd"),
            SessionDateTitle = session.SessionDate.ToString("dddd, dd MMM yyyy"),
            StartedAt = startedAt,
            EndedAt = endedAt,
            TimeRange = $"{startedAt} - {endedAt}",
            TotalDuration = session.TotalDuration.ToString(@"hh\:mm\:ss"),
            IdleDuration = session.IdleDuration.ToString(@"hh\:mm\:ss"),
            TopApps = topAppsText,
            AppBreakdown = appBreakdown,
            AppUsageProgressItems = appUsageProgressItems
        };
    }
}
