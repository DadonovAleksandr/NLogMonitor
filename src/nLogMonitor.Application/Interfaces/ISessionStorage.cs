using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

public interface ISessionStorage
{
    Task<LogSession?> GetAsync(Guid sessionId);
    Task SaveAsync(LogSession session);
    Task DeleteAsync(Guid sessionId);
    Task<IEnumerable<LogSession>> GetAllAsync();
}
