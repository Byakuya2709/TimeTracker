using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.App.Services;

namespace TimeTracker.App;
public partial class App : System.Windows.Application
{
	private ServiceProvider? _serviceProvider;
	private OverlayWindow? _overlayWindow;
	private MainWindow? _dashboardWindow;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		string databasePath = Path.Combine(AppContext.BaseDirectory, "timetracker.db");
		_serviceProvider = ServiceRegistration.BuildServiceProvider(databasePath);

		
		ServiceRegistration.EnsureDatabaseCreated(_serviceProvider);
		_overlayWindow = _serviceProvider.GetRequiredService<OverlayWindow>();
		_overlayWindow.Show();
	}

	public void OpenOrActivateMainWindow()
	{
		if (_serviceProvider is null)
		{
			return;
		}

		if (_dashboardWindow is not null)
		{
			if (_dashboardWindow.IsVisible)
			{
				if (_dashboardWindow.WindowState == WindowState.Minimized)
				{
					_dashboardWindow.WindowState = WindowState.Normal;
				}

				_dashboardWindow.Activate();
				_dashboardWindow.Focus();
				return;
			}

			_dashboardWindow = null;
		}

		_dashboardWindow = _serviceProvider.GetRequiredService<MainWindow>();
		_dashboardWindow.Closed += OnDashboardClosed;
		_dashboardWindow.Show();
	}

	public void CloseMainWindowOnly()
	{
		ReleaseDashboardWindowResources(closeIfVisible: true);
	}

	public void ExitEntireApplication()
	{
		ReleaseDashboardWindowResources(closeIfVisible: true);
		ReleaseOverlayWindowResources(closeIfVisible: true);

		Shutdown();
	}

	private void OnDashboardClosed(object? sender, EventArgs e)
	{
		if (sender is MainWindow dashboardWindow)
		{
			dashboardWindow.Closed -= OnDashboardClosed;
			DisposeDataContextIfNeeded(dashboardWindow.DataContext);
			dashboardWindow.DataContext = null;
			dashboardWindow.Content = null;
		}

		if (ReferenceEquals(_dashboardWindow, sender))
		{
			_dashboardWindow = null;
		}
	}

	private void ReleaseDashboardWindowResources(bool closeIfVisible)
	{
		if (_dashboardWindow is null)
		{
			return;
		}

		MainWindow dashboardWindow = _dashboardWindow;
		dashboardWindow.Closed -= OnDashboardClosed;

		if (closeIfVisible && dashboardWindow.IsVisible)
		{
			dashboardWindow.Close();
		}

		DisposeDataContextIfNeeded(dashboardWindow.DataContext);
		dashboardWindow.DataContext = null;
		dashboardWindow.Content = null;
		_dashboardWindow = null;
	}

	private void ReleaseOverlayWindowResources(bool closeIfVisible)
	{
		if (_overlayWindow is null)
		{
			return;
		}

		OverlayWindow overlayWindow = _overlayWindow;

		if (closeIfVisible && overlayWindow.IsVisible)
		{
			overlayWindow.Close();
		}

		DisposeDataContextIfNeeded(overlayWindow.DataContext);
		overlayWindow.DataContext = null;
		overlayWindow.Content = null;
		_overlayWindow = null;
	}

	private static void DisposeDataContextIfNeeded(object? dataContext)
	{
		if (dataContext is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		ReleaseDashboardWindowResources(closeIfVisible: true);
		ReleaseOverlayWindowResources(closeIfVisible: true);
		_serviceProvider?.Dispose();
		_serviceProvider = null;
		base.OnExit(e);
	}
}

