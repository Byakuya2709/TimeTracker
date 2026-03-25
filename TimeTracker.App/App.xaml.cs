using System.IO;
using System.Windows;
using TimeTracker.App.ViewModels;
using TimeTracker.Application.Services;
using TimeTracker.Infrastructure.Persistence;
using TimeTracker.Infrastructure.Services;

namespace TimeTracker.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
	private MainViewModel? _mainViewModel;
	private OverlayWindow? _overlayWindow;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		string databasePath = Path.Combine(AppContext.BaseDirectory, "timetracker.db");
		TimeSpan idleThreshold = ResolveIdleThreshold();
		SqliteActivityLogStore activityLogStore = new(databasePath);

		ActivityTracker tracker = new(
			new Win32ActiveAppReader(idleThreshold),
			activityLogStore);

		_mainViewModel = new MainViewModel(tracker, activityLogStore);

		MainWindow dashboardWindow = new(_mainViewModel);
		_overlayWindow = new OverlayWindow(_mainViewModel);

		dashboardWindow.Closed += OnDashboardClosed;

		MainWindow = dashboardWindow;
		dashboardWindow.Show();
		_overlayWindow.Show();
	}

	private void OnDashboardClosed(object? sender, EventArgs e)
	{
		if (_overlayWindow is null)
		{
			return;
		}

		if (_overlayWindow.IsVisible)
		{
			_overlayWindow.Close();
		}

		_overlayWindow = null;
	}

	protected override void OnExit(ExitEventArgs e)
	{
		if (_overlayWindow is not null && _overlayWindow.IsVisible)
		{
			_overlayWindow.Close();
		}

		_overlayWindow = null;
		_mainViewModel?.Dispose();
		base.OnExit(e);
	}

	private static TimeSpan ResolveIdleThreshold()
	{
		string? secondsValue = Environment.GetEnvironmentVariable("TIMETRACKER_IDLE_SECONDS") ?? "300";
		if (!int.TryParse(secondsValue, out int seconds) || seconds < 1)
		{
			return TimeSpan.FromMinutes(5);
		}

		return TimeSpan.FromSeconds(seconds);
	}
}

