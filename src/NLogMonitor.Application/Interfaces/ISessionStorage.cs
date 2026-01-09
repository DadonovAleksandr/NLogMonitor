using NLogMonitor.Domain.Entities;

namespace NLogMonitor.Application.Interfaces;

public interface ISessionStorage
{
    Task<LogSession?> GetAsync(Guid sessionId);
    Task SaveAsync(LogSession session);
    Task DeleteAsync(Guid sessionId);
    Task<IEnumerable<LogSession>> GetAllAsync();
}
