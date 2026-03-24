using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Services;

public class ActivityTracker
{
    private readonly IActiveAppReader _activeAppReader;
    private readonly IActivityLogStore _activityLogStore;

    private ActivityLog? _currentActivity;

    public ActivityTracker(IActiveAppReader activeAppReader, IActivityLogStore activityLogStore)
    {
        _activeAppReader = activeAppReader;
        _activityLogStore = activityLogStore;
    }

    public TrackingSnapshot Tick()
    {
        string activeAppName = _activeAppReader.GetActiveAppName();

        bool hasChanged = _currentActivity == null
            || !_currentActivity.AppName.Equals(activeAppName, StringComparison.OrdinalIgnoreCase);

        if (hasChanged)
        {
            CloseCurrentActivity();
            StartNewActivity(activeAppName);
        }

        if (_currentActivity == null)
        {
            return new TrackingSnapshot();
        }

        TimeSpan elapsed = DateTime.Now - _currentActivity.StartTime;
        int score = CalculateFocusScore(_currentActivity.AppName, elapsed);

        return new TrackingSnapshot
        {
            CurrentAppName = _currentActivity.AppName,
            Elapsed = elapsed,
            FocusScore = score,
            FocusSummary = GetFocusSummary(score)
        };
    }

    public void Stop()
    {
        CloseCurrentActivity();
    }

    private void CloseCurrentActivity()
    {
        if (_currentActivity == null || _currentActivity.EndTime.HasValue)
        {
            return;
        }

        _currentActivity.EndTime = DateTime.Now;
        _activityLogStore.Update(_currentActivity);
    }

    private void StartNewActivity(string appName)
    {
        _currentActivity = new ActivityLog
        {
            AppName = appName,
            StartTime = DateTime.Now
        };

        _activityLogStore.Add(_currentActivity);
    }

    private static int CalculateFocusScore(string appName, TimeSpan elapsed)
    {
        string[] productiveApps = ["devenv", "code", "rider", "notepad", "word"];
        bool seemsProductive = productiveApps.Contains(appName, StringComparer.OrdinalIgnoreCase);

        double minutes = elapsed.TotalMinutes;
        int score = seemsProductive
            ? 60 + (int)Math.Round(minutes * 2)
            : 50 - (int)Math.Round(minutes * 2);

        return Math.Clamp(score, 10, 100);
    }

    private static string GetFocusSummary(int score)
    {
        if (score >= 85)
        {
            return "Peak Performance";
        }

        if (score >= 65)
        {
            return "Deep Work";
        }

        if (score >= 40)
        {
            return "Steady Focus";
        }

        return "Needs Attention";
    }
}
