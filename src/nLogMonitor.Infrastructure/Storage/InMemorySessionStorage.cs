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
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _sessionToConnections = new();
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
            wrapper.LastAccessedAt = DateTime.UtcNow;

            // Если сессия привязана к SignalR - TTL не применяется (сессия живёт бесконечно)
            var hasConnection = _sessionToConnections.TryGetValue(sessionId, out var connections)
                && connections != null
                && !connections.IsEmpty;

            if (hasConnection)
            {
                // Устанавливаем ExpiresAt в далёкое будущее для индикации "не истекает"
                wrapper.Session.ExpiresAt = DateTime.MaxValue;

                _logger.LogDebug(
                    "Session {SessionId} accessed, SignalR connected ({ConnectionCount} connections) - TTL disabled",
                    sessionId,
                    connections!.Count);
            }
            else
            {
                // Fallback: sliding expiration для старых клиентов без SignalR
                var newExpiresAt = DateTime.UtcNow + _ttl;
                wrapper.Session.ExpiresAt = newExpiresAt;

                _logger.LogDebug(
                    "Session {SessionId} accessed, no SignalR - TTL extended to {ExpiresAt}",
                    sessionId,
                    newExpiresAt);
            }

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
    public Task<IReadOnlyList<LogEntry>> AppendEntriesAsync(Guid sessionId, IEnumerable<LogEntry> newEntries, long newPosition)
    {
        if (!_sessions.TryGetValue(sessionId, out var wrapper))
        {
            _logger.LogWarning(
                "Cannot append entries to non-existent session {SessionId}",
                sessionId);
            return Task.FromResult<IReadOnlyList<LogEntry>>(Array.Empty<LogEntry>());
        }

        var entriesList = newEntries.ToList();
        if (entriesList.Count == 0)
        {
            _logger.LogDebug("No new entries to append to session {SessionId}", sessionId);
            return Task.FromResult<IReadOnlyList<LogEntry>>(Array.Empty<LogEntry>());
        }

        // Thread-safe добавление записей
        lock (wrapper.Session)
        {
            // Продолжаем нумерацию ID с последней записи
            var lastId = wrapper.Session.Entries.Count > 0
                ? wrapper.Session.Entries[^1].Id
                : 0;

            foreach (var entry in entriesList)
            {
                entry.Id = ++lastId;
                wrapper.Session.Entries.Add(entry);

                // Обновляем счётчики по уровням
                if (wrapper.Session.LevelCounts.ContainsKey(entry.Level))
                {
                    wrapper.Session.LevelCounts[entry.Level]++;
                }
                else
                {
                    wrapper.Session.LevelCounts[entry.Level] = 1;
                }
            }

            // Обновляем позицию чтения
            wrapper.Session.LastReadPosition = newPosition;

            _logger.LogInformation(
                "Appended {Count} new entries to session {SessionId}, new position: {Position}",
                entriesList.Count,
                sessionId,
                newPosition);
        }

        // Возвращаем записи с назначенными ID
        return Task.FromResult<IReadOnlyList<LogEntry>>(entriesList);
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
    /// После привязки TTL для сессии отключается (сессия живёт бесконечно пока соединение активно).
    /// Поддерживает множественные подключения к одной сессии (multi-tab, reconnect).
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    /// <param name="sessionId">ID сессии логов.</param>
    public Task BindConnectionAsync(string connectionId, Guid sessionId)
    {
        // Добавляем маппинг connectionId -> sessionId для быстрого поиска
        _connectionToSession.AddOrUpdate(connectionId, sessionId, (_, _) => sessionId);

        // Добавляем connectionId в коллекцию подключений для sessionId (1:N маппинг)
        // Используем ConcurrentDictionary<string, byte> для thread-safe добавления/удаления O(1)
        var connections = _sessionToConnections.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, byte>());
        connections.TryAdd(connectionId, 0);

        // Отключаем TTL для привязанной сессии
        if (_sessions.TryGetValue(sessionId, out var wrapper))
        {
            wrapper.Session.ExpiresAt = DateTime.MaxValue;

            _logger.LogInformation(
                "Connection {ConnectionId} bound to session {SessionId} (total connections: {ConnectionCount}), TTL disabled",
                connectionId,
                sessionId,
                connections.Count);
        }
        else
        {
            _logger.LogWarning(
                "Connection {ConnectionId} bound to non-existent session {SessionId}",
                connectionId,
                sessionId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Отвязывает SignalR connectionId от сессии.
    /// Удаляет сессию ТОЛЬКО если это был последний активный connectionId для данной сессии.
    /// Это позволяет поддерживать multi-tab и автореконнект.
    /// </summary>
    /// <param name="connectionId">ID SignalR соединения.</param>
    public async Task UnbindConnectionAsync(string connectionId)
    {
        if (!_connectionToSession.TryRemove(connectionId, out var sessionId))
        {
            _logger.LogDebug(
                "Connection {ConnectionId} not found for unbinding",
                connectionId);
            return;
        }

        _logger.LogDebug(
            "Connection {ConnectionId} unbound from session {SessionId}",
            connectionId,
            sessionId);

        // Удаляем connectionId из коллекции подключений сессии (атомарная операция O(1))
        if (_sessionToConnections.TryGetValue(sessionId, out var connections))
        {
            // Thread-safe удаление из ConcurrentDictionary
            connections.TryRemove(connectionId, out _);

            // Проверяем: остались ли другие подключения?
            if (connections.IsEmpty)
            {
                // Это был последний connectionId - удаляем сессию
                _sessionToConnections.TryRemove(sessionId, out _);

                _logger.LogInformation(
                    "No more connections for session {SessionId}, deleting session",
                    sessionId);

                await DeleteAsync(sessionId);
            }
            else
            {
                // Есть другие подключения - сессия остаётся
                _logger.LogInformation(
                    "Session {SessionId} has {RemainingCount} remaining connections, keeping session alive",
                    sessionId,
                    connections.Count);
            }
        }
        else
        {
            _logger.LogWarning(
                "No connections found for session {SessionId} during unbind",
                sessionId);
        }
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

    /// <inheritdoc />
    public Task<int> GetActiveSessionCountAsync()
    {
        return Task.FromResult(_sessions.Count);
    }

    /// <inheritdoc />
    public Task<long> GetTotalLogsCountAsync()
    {
        long totalCount = 0;
        foreach (var wrapper in _sessions.Values)
        {
            totalCount += wrapper.Session.Entries.Count;
        }

        return Task.FromResult(totalCount);
    }

    /// <inheritdoc />
    public Task<int> GetActiveConnectionsCountAsync()
    {
        return Task.FromResult(_connectionToSession.Count);
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
                    if (_sessionToConnections.TryRemove(kvp.Key, out var connections))
                    {
                        // Удаляем все connectionId -> sessionId маппинги
                        foreach (var connId in connections.Keys)
                        {
                            _connectionToSession.TryRemove(connId, out _);
                        }

                        _logger.LogDebug(
                            "Removed {ConnectionCount} connection mappings for expired session {SessionId}",
                            connections.Count,
                            kvp.Key);
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
            _sessionToConnections.Clear();
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
