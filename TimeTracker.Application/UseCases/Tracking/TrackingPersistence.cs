using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.UseCases.Tracking;

public static class TrackingPersistence
{
    public static void CloseCurrentActivity(TrackingSessionState state, IActivityLogStore activityLogStore, DateTime at)
    {
        if (state.CurrentActivity == null || state.CurrentActivity.EndTime.HasValue)
        {
            return;
        }

        state.CurrentActivity.EndTime = at;
        activityLogStore.Update(state.CurrentActivity);
    }

    public static void StartNewActivity(TrackingSessionState state, IActivityLogStore activityLogStore, string appName, DateTime at)
    {
        state.CurrentActivity = new ActivityLog
        {
            AppName = appName,
            StartTime = at
        };

        activityLogStore.Add(state.CurrentActivity);
    }
}
