using TimeTracker.Application.Models;

namespace TimeTracker.Application.UseCases.Tracking;

public static class TrackingRules
{
    public const string UnassignedAppName = "Unassigned";

    public static bool CanPersistActivity(string trackedAppName)
    {
        return !trackedAppName.Equals(UnassignedAppName, StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveTrackedAppName(string activeAppName)
    {
        return IsUnknownOrEmpty(activeAppName)
            ? UnassignedAppName
            : activeAppName;
    }

    private static bool IsUnknownOrEmpty(string appName)
    {
        return string.IsNullOrWhiteSpace(appName)
            || appName.Equals("Unknown App", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDisplayAppName(TrackingSessionState state)
    {
        return state.State switch
        {
            TrackingState.Running => state.LastTrackedAppName.Equals(UnassignedAppName, StringComparison.OrdinalIgnoreCase)
                ? "Waiting for app switch"
                : state.LastTrackedAppName,
            TrackingState.Paused => "Paused",
            _ => "Ready"
        };
    }

    public static int CalculateFocusScore(string appName, TimeSpan elapsed)
    {
        string[] productiveApps = ["devenv", "code", "rider", "notepad", "word"];
        bool seemsProductive = productiveApps.Contains(appName, StringComparer.OrdinalIgnoreCase);

        double minutes = elapsed.TotalMinutes;
        int score = seemsProductive
            ? 60 + (int)Math.Round(minutes * 2)
            : 50 - (int)Math.Round(minutes * 2);

        return Math.Clamp(score, 10, 100);
    }

    public static string GetFocusSummary(int score)
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

    public static string GetSuggestionMessage(TimeSpan totalRecorded, int score)
    {
        if (totalRecorded >= TimeSpan.FromMinutes(90))
        {
            return "Ban da tap trung rat tot. Dung day va nghi 10 phut nhe.";
        }

        if (totalRecorded >= TimeSpan.FromMinutes(50))
        {
            return "Sap du 1 chu ky. Co the nghi mat 5 phut de giu nhiet.";
        }

        if (score >= 80)
        {
            return "Dang vao form cuc on. Co len, tiep tuc phat huy.";
        }

        if (score < 45)
        {
            return "Thu doi tac vu nho hoac nghi 2 phut de lay lai nhip.";
        }

        return "Nhip do dang on. Giu deu, ban dang lam rat tot.";
    }
}
