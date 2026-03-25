namespace TimeTracker.Domain.Entities;

public class UserSettings
{
    public int IdleDetectionMinutes { get; set; } = 10;

    public bool AutoStartOnBoot { get; set; }

    public int OverlayOpacity { get; set; } = 85;

    public string OverlayPosition { get; set; } = "TopRight";
}
