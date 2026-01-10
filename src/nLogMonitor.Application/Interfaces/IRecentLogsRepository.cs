using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

public interface IRecentLogsRepository
{
    Task<IEnumerable<RecentLogEntry>> GetAllAsync();
    Task AddAsync(RecentLogEntry entry);
    Task ClearAsync();
}
