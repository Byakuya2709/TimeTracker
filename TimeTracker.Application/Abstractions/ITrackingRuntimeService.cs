using TimeTracker.Application.Models;
using System.Threading;
using System.Threading.Tasks;

namespace TimeTracker.Application.Abstractions;

public interface ITrackingRuntimeService
{
    TrackingState State { get; }

    void SetIdleThreshold(TimeSpan idleThreshold);

    TrackingSnapshot Tick();

    void Start();

    void Pause();

    Task StopAsync(CancellationToken cancellationToken = default);

    void Stop();
}
