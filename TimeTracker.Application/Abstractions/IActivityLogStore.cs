using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Abstractions;

public interface IActivityLogStore
{
    void Add(ActivityLog activityLog);

    void Update(ActivityLog activityLog);

    IReadOnlyList<ActivityLog> GetAll();

    ActivityLog GetActivityLogById(Guid id);
}
