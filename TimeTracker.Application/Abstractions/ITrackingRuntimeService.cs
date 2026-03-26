using TimeTracker.Application.Models;

namespace TimeTracker.Application.Abstractions;

public interface ITrackingRuntimeService
{
    TrackingState State { get; }

    void SetIdleThreshold(TimeSpan idleThreshold);

    TrackingSnapshot Tick();

    void Start();

    void Pause();

    void Stop();
}
