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

    private static readonly string[] ContractedApps = ["devenv", "code", "rider", "notepad", "word", "excel", "powerpoint", "teams", "slack", "zoom","abode", "figma", "cursor", "webstorm", "pycharm", "intellij"];
    private static readonly string[] CommittedApps = ["todoist", "notion", "calendar", "mail", "outlook", "evernote", "obsidian", "onenote", "microsoft teams", "slack", "zoom","chatgpt", "gemini", "copilot"];
    private static readonly string[] FreeApps = ["youtube", "discord", "steam", "epicgames", "netflix", "facebook", "tiktok", "x", "instagram", "reddit", "spotify", "music"];

    public enum TimeCategory
    {
        Contracted, // Thời gian làm việc chính
        Committed,  // Thời gian cam kết/quản lý
        Necessary,  // Thời gian bắt buộc (nghỉ ngơi, ăn uống)
        Free,       // Thời gian rảnh rỗi/giải trí
        Uncategorized
    }

   public static TimeCategory GetTimeCategory(string appName)
{
    if (string.IsNullOrWhiteSpace(appName))
        return TimeCategory.Uncategorized;

    appName = Normalize(appName);

    if (Matches(appName, ContractedApps)) return TimeCategory.Contracted;
    if (Matches(appName, CommittedApps)) return TimeCategory.Committed;
    if (Matches(appName, FreeApps)) return TimeCategory.Free;
    if (IsIdleAppName(appName)) return TimeCategory.Necessary;

    return TimeCategory.Uncategorized;
}

private static bool Matches(string source, string[] keywords)
{
    return keywords.Any(k => IsMatch(source, k));
}

private static bool IsMatch(string source, string keyword)
{
    keyword = keyword.ToLower();

    // Match nguyên từ (tránh "code" ăn vào "decode")
    return source == keyword
        || source.StartsWith(keyword + " ")
        || source.EndsWith(" " + keyword)
        || source.Contains(" " + keyword + " ")
        || source.Contains(keyword + ".")   // chrome.exe, code.exe
        || source.Contains(keyword + "-")   // youtube - chrome
        || source.Contains("-" + keyword);
}

private static string Normalize(string input)
{
    return input
        .ToLower()
        .Replace("_", " ")
        .Replace(".", " ")
        .Replace("-", " ")
        .Trim();
}

    public static int CalculateFocusScore(TrackingSessionState state, DateTime now)
    {
        double score = 0; // Điểm khởi đầu là 0

        double contractedMinutes = 0;
        double freeMinutes = 0;

        // Phân tích toàn bộ thời lượng đã ứng dụng
        foreach (var duration in state.SessionAppDurations)
        {
            double minutes = duration.Value.TotalMinutes;
            var category = GetTimeCategory(duration.Key);

            score += category switch
            {
                TimeCategory.Contracted => minutes * 1.2, // Tăng chậm, cần nhiều nỗ lực
                TimeCategory.Committed  => minutes * 0.8, // Tăng nhẹ, bổ trợ
                TimeCategory.Necessary  => minutes * 0.1, // Hầu như không ảnh hưởng
                TimeCategory.Free       => 0,             // Sẽ xử lý penalize rẽ nhánh ở dưới để tăng tính phi tuyến tính
                _                       => minutes * 0.05 // Uncategorized
            };

            if (category == TimeCategory.Contracted) contractedMinutes += minutes;
            if (category == TimeCategory.Free) freeMinutes += minutes;
        }

        // Nonlinear Free Penalty: Càng dùng lâu Free Time, mức độ trừ điểm càng khốc liệt
        if (freeMinutes > 0)
        {
            // Vd: Dùng 10 phút -> mất ~10 điểm, 20 phút -> mất ~40 điểm, 30 phút -> mất ~90 điểm
            score -= Math.Pow(freeMinutes, 1.3) * 0.8; 
        }

        // Tối ưu Contracted Bonus: Cho điểm vượt ngưỡng nếu có hiệu suất cực kỳ cao nhưng giới hạn bằng Logarit/Căn bậc 2
        // Thưởng thêm % nhỏ nếu tỉ lệ làm việc tốt (giúp đạt 100 êm ái hơn chứ ko phải gắt đoạn cuối)
        if (contractedMinutes > 30)
        {
            score += Math.Sqrt(contractedMinutes - 30) * 1.5;
        }

        // Penalty khi switch app liên tục - Phân biệt The switch context
        if (state.AppSwitchCount > 0)
        {
            // Trừ điểm dựa theo tổng số switch kết hợp việc switch giữa các loại nào
            // Tạm thời đơn giản hóa: phạt theo logarit để tránh switch 100 lần bị âm vô cực ngay
            score -= Math.Log(state.AppSwitchCount + 1) * 3.0;
        }

        // Bonus khi focus liên tục vào một ứng dụng hữu ích mà KHÔNG đổi TAB
        if (state.LastAppChangedAt.HasValue && state.State == TrackingState.Running)
        {
            var category = GetTimeCategory(state.LastTrackedAppName);
            // Chỉ thưởng cho ứng dụng chính hoặc hỗ trợ
            if (category == TimeCategory.Contracted || category == TimeCategory.Committed)
            {
                double continuousMins = (now - state.LastAppChangedAt.Value).TotalMinutes;
                if (continuousMins > 15)
                {
                    // Nonlinear Bonus cho việc giữ focus dài (Logarit)
                    score += Math.Log(continuousMins - 14) * 2.5; 
                }
            }
            else if (category == TimeCategory.Free)
            {
                double continuousMins = (now - state.LastAppChangedAt.Value).TotalMinutes;
                if (continuousMins > 10)
                {
                    // Dùng app giải trí liên tục không switch > 10 phút => xé sâu điểm ngay lập tức (-0.5 mỗi phút * số mũ nhỏ)
                    score -= Math.Pow(continuousMins - 10, 1.2) * 1.0;
                }
            }
        }

        return (int)Math.Clamp(score, 0, 100);
    }


public static string GetFocusSummary(int score, TimeSpan totalRecorded, TimeSpan idleDuration)
{
    if (totalRecorded < TimeSpan.FromMinutes(5))
    {
        return "Đang thu thập thêm dữ liệu...";
    }

    double idleRatio = totalRecorded.TotalSeconds <= 0
        ? 0
        : idleDuration.TotalSeconds / totalRecorded.TotalSeconds;

    if (score >= 85 && idleRatio <= 0.15)
    {
        return "Bạn đang tập trung rất tốt vào các công việc quan trọng";
    }

    if (score >= 70 && idleRatio <= 0.25)
    {
        return "Bạn đang phân bổ thời gian khá hợp lý";
    }

    if (score >= 50)
    {
        return "Bạn nên giảm bớt thời gian giải trí để tập trung hơn";
    }

    return "Đang chờ hoạt động...";
}
    public static string GetSuggestionMessage(TimeSpan totalRecorded, int score, string currentAppName = "")
    {
        var category = GetTimeCategory(currentAppName);

        if (totalRecorded >= TimeSpan.FromMinutes(90) && category == TimeCategory.Contracted)
        {
            return "Bạn đã làm việc liên tục quá lâu. Hãy chuyển qua Necessary Time (vận động/uống nước) tầm 10 phút.";
        }

        if (totalRecorded >= TimeSpan.FromMinutes(50) && score >= 70)
        {
            return "Sắp hết một chu kỳ. Một chút Necessary Time sẽ giúp tái tạo năng lượng.";
        }

        if (score >= 80)
        {
            return "Bạn đang tối ưu Time-use rất tốt. Contracted/Committed Time đang được ưu tiên!";
        }

        if (category == TimeCategory.Free && totalRecorded > TimeSpan.FromMinutes(30))
        {
            return "Sử dụng Free Time quá lâu có thể làm tụt điểm. Hãy cân nhắc trở lại làm việc.";
        }

        if (score < 40 && totalRecorded >= TimeSpan.FromMinutes(30))
        {
            return "Điểm phân bổ hiện ở mức thấp. Hạn chế sử dụng Free Time và đổi app ít lại để hồi điểm.";
        }

        return "";
    }
}
