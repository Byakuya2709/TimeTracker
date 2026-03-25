using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TimeTracker.App.ViewModels;
using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Services;
using TimeTracker.Domain.Entities;
using TimeTracker.Infrastructure.Persistence;
using TimeTracker.Infrastructure.Services;

namespace TimeTracker.App;
public partial class App : System.Windows.Application
{
	private ServiceProvider? _serviceProvider;
	private OverlayWindow? _overlayWindow;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		string databasePath = Path.Combine(AppContext.BaseDirectory, "timetracker.db");

		ServiceCollection services = new();

		services.AddDbContextFactory<TimeTrackerDbContext>(options =>
			options.UseSqlite($"Data Source={databasePath}"));
			
		services.AddSingleton<IActivityLogStore, SqliteActivityLogStore>();
		services.AddSingleton<IUserSettingsStore, SqliteUserSettingsStore>();
		services.AddSingleton<IUserSettingsService, UserSettingsService>();
		services.AddSingleton(provider =>
		{
			IUserSettingsService userSettingsService = provider.GetRequiredService<IUserSettingsService>();
			TimeSpan idleThreshold = ResolveIdleThreshold(userSettingsService);

			return new ActivityTracker(
				new Win32ActiveAppReader(idleThreshold),
				provider.GetRequiredService<IActivityLogStore>());
		});
		services.AddSingleton<MainViewModel>();
		services.AddSingleton(provider => new MainWindow(provider.GetRequiredService<MainViewModel>()));
		services.AddSingleton(provider => new OverlayWindow(provider.GetRequiredService<MainViewModel>()));

		_serviceProvider = services.BuildServiceProvider();

		using (TimeTrackerDbContext dbContext = _serviceProvider
			.GetRequiredService<IDbContextFactory<TimeTrackerDbContext>>()
			.CreateDbContext())
		{
			dbContext.Database.EnsureCreated();
		}

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

	private static TimeSpan ResolveIdleThreshold(IUserSettingsService userSettingsService)
	{
		try
		{
			UserSettings settings = userSettingsService.GetUserSettings();
			if (settings.IdleDetectionMinutes > 0)
			{
				return TimeSpan.FromMinutes(settings.IdleDetectionMinutes);
			}
		}
		catch
		{
			// Fallback to environment variable when settings table is unavailable.
		}

		return ResolveIdleThresholdFromEnvironment();
	}

	private static TimeSpan ResolveIdleThresholdFromEnvironment()
	{
		string? secondsValue = Environment.GetEnvironmentVariable("TIMETRACKER_IDLE_SECONDS") ?? "300";
		if (!int.TryParse(secondsValue, out int seconds) || seconds < 1)
		{
			return TimeSpan.FromMinutes(5);
		}

		return TimeSpan.FromSeconds(seconds);
	}
}

