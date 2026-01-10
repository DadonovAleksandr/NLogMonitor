using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

public interface ISessionStorage
{
    Task<LogSession?> GetAsync(Guid sessionId);
    Task SaveAsync(LogSession session);
    Task DeleteAsync(Guid sessionId);
    Task<IEnumerable<LogSession>> GetAllAsync();

    /// <summary>
    /// Атомарно добавляет новые записи в сессию и обновляет LastReadPosition.
    /// Thread-safe метод для использования из FileWatcherService.
    /// </summary>
    /// <param name="sessionId">ID сессии.</param>
    /// <param name="newEntries">Новые записи логов.</param>
    /// <param name="newPosition">Новая позиция в файле после чтения.</param>
    Task AppendEntriesAsync(Guid sessionId, IEnumerable<LogEntry> newEntries, long newPosition);

    /// <summary>
    /// Регистрирует callback, который будет вызван при удалении сессии.
    /// Используется для очистки временных файлов и других ресурсов, связанных с сессией.
    /// </summary>
    /// <param name="sessionId">ID сессии.</param>
    /// <param name="cleanupCallback">Асинхронный callback для очистки ресурсов.</param>
    Task RegisterCleanupCallbackAsync(Guid sessionId, Func<Task> cleanupCallback);

    /// <summary>
    /// Связывает SignalR connectionId с сессией.
    /// После связывания сессия живёт бесконечно (TTL не применяется) пока соединение активно.
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    /// <param name="sessionId">ID сессии логов.</param>
    Task BindConnectionAsync(string connectionId, Guid sessionId);

    /// <summary>
    /// Отвязывает SignalR connectionId от сессии и удаляет сессию.
    /// Вызывается при явном LeaveSession или при разрыве соединения.
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    Task UnbindConnectionAsync(string connectionId);

    /// <summary>
    /// Получает ID сессии по connectionId.
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    /// <returns>ID сессии или null если не найдена.</returns>
    Task<Guid?> GetSessionByConnectionAsync(string connectionId);
}
