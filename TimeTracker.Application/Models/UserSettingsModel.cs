namespace TimeTracker.Application.Models;

public sealed class UserSettingsModel
{
    public int IdleDetectionMinutes { get; init; } = 10;

    public bool AutoStartOnBoot { get; init; }

    public int OverlayOpacity { get; init; } = 85;

    public string OverlayPosition { get; init; } = "TopRight";
}
