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
    /// Возвращает записи с назначенными ID для корректной отправки клиентам.
    /// </summary>
    /// <param name="sessionId">ID сессии.</param>
    /// <param name="newEntries">Новые записи логов.</param>
    /// <param name="newPosition">Новая позиция в файле после чтения.</param>
    /// <returns>Записи с назначенными ID или пустой список если сессия не найдена.</returns>
    Task<IReadOnlyList<LogEntry>> AppendEntriesAsync(Guid sessionId, IEnumerable<LogEntry> newEntries, long newPosition);

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

    /// <summary>
    /// Возвращает количество активных сессий.
    /// </summary>
    /// <returns>Количество активных сессий.</returns>
    Task<int> GetActiveSessionCountAsync();

    /// <summary>
    /// Возвращает общее количество записей логов во всех сессиях.
    /// </summary>
    /// <returns>Общее количество записей логов.</returns>
    Task<long> GetTotalLogsCountAsync();

    /// <summary>
    /// Возвращает количество активных SignalR соединений.
    /// </summary>
    /// <returns>Количество активных соединений.</returns>
    Task<int> GetActiveConnectionsCountAsync();
}
