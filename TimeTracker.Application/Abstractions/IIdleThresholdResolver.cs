namespace TimeTracker.Application.Abstractions;

public interface IIdleThresholdResolver
{
    TimeSpan Resolve();
}
