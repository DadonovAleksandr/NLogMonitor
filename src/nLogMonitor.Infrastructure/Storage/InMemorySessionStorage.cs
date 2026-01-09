using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Infrastructure.Storage;

/// <summary>
/// In-memory хранилище сессий логов с поддержкой sliding expiration и автоматической очистки.
/// </summary>
public class InMemorySessionStorage : ISessionStorage, IDisposable
{
    private readonly ConcurrentDictionary<Guid, SessionWrapper> _sessions = new();
    private readonly ConcurrentDictionary<string, Guid> _connectionToSession = new();
    private readonly ConcurrentDictionary<Guid, Func<Task>> _cleanupCallbacks = new();
    private readonly TimeSpan _ttl;
    private readonly Timer _cleanupTimer;
    private readonly ILogger<InMemorySessionStorage> _logger;
    private bool _disposed;

    /// <summary>
    /// Создаёт новый экземпляр InMemorySessionStorage.
    /// </summary>
    /// <param name="options">Настройки сессий из конфигурации.</param>
    /// <param name="logger">Логгер.</param>
    public InMemorySessionStorage(
        IOptions<SessionSettings> options,
        ILogger<InMemorySessionStorage> logger)
    {
        _logger = logger;
        var settings = options.Value;
        _ttl = TimeSpan.FromMinutes(settings.FallbackTtlMinutes);
        var cleanupInterval = TimeSpan.FromMinutes(settings.CleanupIntervalMinutes);

        // Запуск таймера очистки с настраиваемым интервалом
        _cleanupTimer = new Timer(
            CleanupExpiredSessions,
            null,
            cleanupInterval,
            cleanupInterval);

        _logger.LogInformation(
            "InMemorySessionStorage initialized with TTL={TtlMinutes} minutes, cleanup interval={CleanupInterval} minutes",
            settings.FallbackTtlMinutes,
            settings.CleanupIntervalMinutes);
    }

    /// <inheritdoc />
    public Task<LogSession?> GetAsync(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var wrapper))
        {
            // Sliding expiration - продлеваем TTL при каждом обращении
            var newExpiresAt = DateTime.UtcNow + _ttl;
            wrapper.LastAccessedAt = DateTime.UtcNow;
            wrapper.Session.ExpiresAt = newExpiresAt;

            _logger.LogDebug(
                "Session {SessionId} accessed, TTL extended to {ExpiresAt}",
                sessionId,
                newExpiresAt);

            return Task.FromResult<LogSession?>(wrapper.Session);
        }

        _logger.LogDebug("Session {SessionId} not found", sessionId);
        return Task.FromResult<LogSession?>(null);
    }

    /// <inheritdoc />
    public Task SaveAsync(LogSession session)
    {
        var now = DateTime.UtcNow;

        // Устанавливаем время истечения если не задано
        if (session.ExpiresAt == default)
        {
            session.ExpiresAt = now + _ttl;
        }

        var wrapper = new SessionWrapper
        {
            Session = session,
            LastAccessedAt = now
        };

        _sessions.AddOrUpdate(session.Id, wrapper, (_, _) => wrapper);

        _logger.LogInformation(
            "Session {SessionId} saved for file '{FileName}', expires at {ExpiresAt}",
            session.Id,
            session.FileName,
            session.ExpiresAt);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var wrapper))
        {
            _logger.LogInformation(
                "Session {SessionId} deleted (file: '{FileName}')",
                sessionId,
                wrapper.Session.FileName);

            // Вызываем cleanup callback если зарегистрирован
            await ExecuteCleanupCallbackAsync(sessionId);
        }
        else
        {
            _logger.LogDebug("Session {SessionId} not found for deletion", sessionId);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<LogSession>> GetAllAsync()
    {
        var sessions = _sessions.Values
            .Select(w => w.Session)
            .ToList();

        _logger.LogDebug("Retrieved {Count} sessions", sessions.Count);

        return Task.FromResult<IEnumerable<LogSession>>(sessions);
    }

    /// <inheritdoc />
    public Task RegisterCleanupCallbackAsync(Guid sessionId, Func<Task> cleanupCallback)
    {
        _cleanupCallbacks.AddOrUpdate(sessionId, cleanupCallback, (_, _) => cleanupCallback);

        _logger.LogDebug(
            "Cleanup callback registered for session {SessionId}",
            sessionId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Связывает SignalR connectionId с сессией.
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    /// <param name="sessionId">ID сессии логов.</param>
    public Task BindConnectionAsync(string connectionId, Guid sessionId)
    {
        _connectionToSession.AddOrUpdate(connectionId, sessionId, (_, _) => sessionId);

        _logger.LogDebug(
            "Connection {ConnectionId} bound to session {SessionId}",
            connectionId,
            sessionId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Отвязывает SignalR connectionId от сессии и удаляет сессию.
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    public Task UnbindConnectionAsync(string connectionId)
    {
        if (_connectionToSession.TryRemove(connectionId, out var sessionId))
        {
            _logger.LogDebug(
                "Connection {ConnectionId} unbound from session {SessionId}",
                connectionId,
                sessionId);

            // Удаляем связанную сессию
            return DeleteAsync(sessionId);
        }

        _logger.LogDebug(
            "Connection {ConnectionId} not found for unbinding",
            connectionId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Получает ID сессии по connectionId.
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    /// <returns>ID сессии или null если не найдена.</returns>
    public Task<Guid?> GetSessionByConnectionAsync(string connectionId)
    {
        if (_connectionToSession.TryGetValue(connectionId, out var sessionId))
        {
            return Task.FromResult<Guid?>(sessionId);
        }

        return Task.FromResult<Guid?>(null);
    }

    /// <summary>
    /// Callback для таймера очистки просроченных сессий.
    /// </summary>
    private void CleanupExpiredSessions(object? state)
    {
        // Запускаем асинхронную очистку без ожидания (fire-and-forget)
        _ = CleanupExpiredSessionsAsync();
    }

    /// <summary>
    /// Асинхронная очистка просроченных сессий с вызовом cleanup callbacks.
    /// </summary>
    private async Task CleanupExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredCount = 0;

        foreach (var kvp in _sessions)
        {
            if (kvp.Value.Session.ExpiresAt < now)
            {
                if (_sessions.TryRemove(kvp.Key, out var wrapper))
                {
                    expiredCount++;
                    _logger.LogInformation(
                        "Session {SessionId} expired and removed (file: '{FileName}', expired at {ExpiresAt})",
                        kvp.Key,
                        wrapper.Session.FileName,
                        wrapper.Session.ExpiresAt);

                    // Вызываем cleanup callback перед удалением связей
                    await ExecuteCleanupCallbackAsync(kvp.Key);

                    // Удаляем связанные connection mappings
                    var connectionToRemove = _connectionToSession
                        .FirstOrDefault(c => c.Value == kvp.Key);

                    if (connectionToRemove.Key != null)
                    {
                        _connectionToSession.TryRemove(connectionToRemove.Key, out _);
                    }
                }
            }
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation(
                "Cleanup completed: {ExpiredCount} expired sessions removed, {ActiveCount} active sessions remaining",
                expiredCount,
                _sessions.Count);
        }
    }

    /// <summary>
    /// Выполняет cleanup callback для сессии и удаляет его из словаря.
    /// </summary>
    /// <param name="sessionId">ID сессии.</param>
    private async Task ExecuteCleanupCallbackAsync(Guid sessionId)
    {
        if (_cleanupCallbacks.TryRemove(sessionId, out var callback))
        {
            try
            {
                _logger.LogDebug("Executing cleanup callback for session {SessionId}", sessionId);
                await callback();
                _logger.LogDebug("Cleanup callback completed for session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cleanup callback failed for session {SessionId}",
                    sessionId);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Освобождает ресурсы.
    /// </summary>
    /// <param name="disposing">True если вызван из Dispose().</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cleanupTimer.Dispose();
            _sessions.Clear();
            _connectionToSession.Clear();
            _cleanupCallbacks.Clear();
            _logger.LogInformation("InMemorySessionStorage disposed");
        }

        _disposed = true;
    }

    /// <summary>
    /// Внутренний класс-обёртка для сессии с метаданными.
    /// </summary>
    private class SessionWrapper
    {
        /// <summary>
        /// Сессия логов.
        /// </summary>
        public LogSession Session { get; set; } = null!;

        /// <summary>
        /// Время последнего доступа к сессии.
        /// </summary>
        public DateTime LastAccessedAt { get; set; }
    }
}
