using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

public interface ISessionStorage
{
    Task<LogSession?> GetAsync(Guid sessionId);
    Task SaveAsync(LogSession session);
    Task DeleteAsync(Guid sessionId);
    Task<IEnumerable<LogSession>> GetAllAsync();

    /// <summary>
    /// Регистрирует callback, который будет вызван при удалении сессии.
    /// Используется для очистки временных файлов и других ресурсов, связанных с сессией.
    /// </summary>
    /// <param name="sessionId">ID сессии.</param>
    /// <param name="cleanupCallback">Асинхронный callback для очистки ресурсов.</param>
    Task RegisterCleanupCallbackAsync(Guid sessionId, Func<Task> cleanupCallback);
}
