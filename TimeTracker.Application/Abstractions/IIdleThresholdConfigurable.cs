namespace TimeTracker.Application.Abstractions;

public interface IIdleThresholdConfigurable
{
    void SetIdleThreshold(TimeSpan idleThreshold);
}
