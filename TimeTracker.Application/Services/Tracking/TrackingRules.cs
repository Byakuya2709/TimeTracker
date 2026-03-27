using TimeTracker.Application.Models;

namespace TimeTracker.Application.Services.Tracking;

public static class TrackingRules
{
    public const string UnassignedAppName = "Unassigned";
    public const string IdleAppName = "Idle";

    public static bool CanPersistActivity(string trackedAppName)
    {
        return !IsUnassignedAppName(trackedAppName)
            && !IsIdleAppName(trackedAppName);
    }

    public static string ResolveTrackedAppName(string activeAppName)
    {
        if (IsIdleAppName(activeAppName))
        {
            return IdleAppName;
        }

        return IsUnknownOrEmpty(activeAppName)
            ? UnassignedAppName
            : activeAppName.Trim();
    }

    private static bool IsUnknownOrEmpty(string appName)
    {
        return string.IsNullOrWhiteSpace(appName)
            || appName.Equals("Unknown App", StringComparison.OrdinalIgnoreCase)
            || appName.Equals("Ứng dụng không xác định", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUnassignedAppName(string appName)
    {
        return appName.Equals(UnassignedAppName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIdleAppName(string appName)
    {
        return appName.Equals(IdleAppName, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDisplayAppName(TrackingSessionState state)
    {
        return state.State switch
        {
            TrackingState.Running => IsUnassignedAppName(state.LastTrackedAppName)
                ? "Đang chờ chuyển ứng dụng"
                : IsIdleAppName(state.LastTrackedAppName)
                    ? "Không hoạt động"
                : state.LastTrackedAppName,
            TrackingState.Paused => "Đã tạm dừng",
            _ => "Sẵn sàng"
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

    public static string GetFocusSummary(int score, TimeSpan totalRecorded, TimeSpan idleDuration)
    {
        if (totalRecorded < TimeSpan.FromMinutes(5))
        {
            return $"Điểm hiệu suất {score}/100 - Đang thu thập dữ liệu";
        }

        double idleRatio = totalRecorded.TotalSeconds <= 0
            ? 0
            : idleDuration.TotalSeconds / totalRecorded.TotalSeconds;

        if (score >= 85 && idleRatio <= 0.15)
        {
            return $"Điểm hiệu suất {score}/100 - Hiệu suất rất cao";
        }

        if (score >= 70 && idleRatio <= 0.25)
        {
            return $"Điểm hiệu suất {score}/100 - Hiệu suất tốt và ổn định";
        }

        if (score >= 50)
        {
            return $"Điểm hiệu suất {score}/100 - Có thể cải thiện thêm";
        }

        return $"Điểm hiệu suất {score}/100 - Cần tối ưu nhịp làm việc";
    }

    public static string GetSuggestionMessage(TimeSpan totalRecorded, int score)
    {
        if (totalRecorded >= TimeSpan.FromMinutes(90))
        {
            return "Bạn đã tập trung rất tốt. Đứng dậy và nghỉ 10 phút nhé.";
        }

        if (totalRecorded >= TimeSpan.FromMinutes(50))
        {
            return "Sắp đủ một chu kỳ. Có thể nghỉ mắt 5 phút để giữ nhịp.";
        }

        if (score >= 80)
        {
            return "Bạn đang vào guồng rất tốt. Cố lên, tiếp tục phát huy.";
        }

        if (score < 45)
        {
            return "Thử đổi tác vụ nhỏ hoặc nghỉ 2 phút để lấy lại nhịp.";
        }

        return "";
    }
}
