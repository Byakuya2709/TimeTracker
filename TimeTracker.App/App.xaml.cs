using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.App.Services;

namespace TimeTracker.App;
public partial class App : System.Windows.Application
{
	private ServiceProvider? _serviceProvider;
	private OverlayWindow? _overlayWindow;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		string databasePath = Path.Combine(AppContext.BaseDirectory, "timetracker.db");
		_serviceProvider = ServiceRegistration.BuildServiceProvider(databasePath);

		
		ServiceRegistration.EnsureDatabaseCreated(_serviceProvider);

		MainWindow dashboardWindow = _serviceProvider.GetRequiredService<MainWindow>();
		_overlayWindow = _serviceProvider.GetRequiredService<OverlayWindow>();

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
		_serviceProvider?.Dispose();
		_serviceProvider = null;
		base.OnExit(e);
	}
}

