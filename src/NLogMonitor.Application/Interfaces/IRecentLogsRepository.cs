using NLogMonitor.Domain.Entities;

namespace NLogMonitor.Application.Interfaces;

public interface IRecentLogsRepository
{
    Task<IEnumerable<RecentLogEntry>> GetAllAsync();
    Task AddAsync(RecentLogEntry entry);
    Task ClearAsync();
}
