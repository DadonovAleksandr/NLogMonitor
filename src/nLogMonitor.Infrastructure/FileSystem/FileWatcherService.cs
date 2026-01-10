using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using nLogMonitor.Application.Interfaces;

namespace nLogMonitor.Infrastructure.FileSystem;

/// <summary>
/// Сервис мониторинга изменений лог-файлов с использованием FileSystemWatcher.
/// Поддерживает одновременное отслеживание нескольких сессий с debounce механизмом.
/// </summary>
public class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly ConcurrentDictionary<Guid, WatcherContext> _watchers = new();
    private readonly object _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// Событие, возникающее при обнаружении изменений в файле.
    /// </summary>
    public event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Создаёт новый экземпляр FileWatcherService.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartWatchingAsync(Guid sessionId, string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        lock (_lockObject)
        {
            // Если уже отслеживается - останавливаем старый watcher
            if (_watchers.ContainsKey(sessionId))
            {
                StopWatchingInternal(sessionId);
            }

            try
            {
                var directoryPath = Path.GetDirectoryName(filePath)
                    ?? throw new ArgumentException("Cannot determine directory path", nameof(filePath));

                var fileName = Path.GetFileName(filePath);

                var watcher = new FileSystemWatcher(directoryPath)
                {
                    Filter = fileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                var context = new WatcherContext
                {
                    SessionId = sessionId,
                    FilePath = filePath,
                    Watcher = watcher,
                    DebounceTimer = null,
                    LastEventTime = DateTime.UtcNow
                };

                // Подписываемся на события
                watcher.Changed += (sender, args) => OnFileChanged(sessionId, args);
                watcher.Renamed += (sender, args) => OnFileRenamed(sessionId, args);
                watcher.Error += (sender, args) => OnWatcherError(sessionId, args);

                _watchers.TryAdd(sessionId, context);

                _logger.LogInformation(
                    "Started watching file '{FilePath}' for session {SessionId}",
                    filePath,
                    sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to start watching file '{FilePath}' for session {SessionId}",
                    filePath,
                    sessionId);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopWatchingAsync(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lockObject)
        {
            StopWatchingInternal(sessionId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool IsWatching(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _watchers.ContainsKey(sessionId);
    }

    /// <summary>
    /// Внутренний метод остановки мониторинга (без проверки disposed и блокировки).
    /// Должен вызываться внутри lock (_lockObject).
    /// </summary>
    /// <param name="sessionId">ID сессии.</param>
    private void StopWatchingInternal(Guid sessionId)
    {
        if (_watchers.TryRemove(sessionId, out var context))
        {
            try
            {
                // Останавливаем и освобождаем FileSystemWatcher
                context.Watcher.EnableRaisingEvents = false;
                context.Watcher.Dispose();

                // Останавливаем и освобождаем debounce timer
                context.DebounceTimer?.Dispose();

                _logger.LogInformation(
                    "Stopped watching file '{FilePath}' for session {SessionId}",
                    context.FilePath,
                    sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error while stopping watcher for session {SessionId}",
                    sessionId);
            }
        }
        else
        {
            _logger.LogDebug(
                "No active watcher found for session {SessionId}",
                sessionId);
        }
    }

    /// <summary>
    /// Обработчик события изменения файла.
    /// </summary>
    private void OnFileChanged(Guid sessionId, FileSystemEventArgs e)
    {
        if (!_watchers.TryGetValue(sessionId, out var context))
        {
            return;
        }

        _logger.LogDebug(
            "File change detected for session {SessionId}: {ChangeType} - {FilePath}",
            sessionId,
            e.ChangeType,
            e.FullPath);

        // Debounce: перезапускаем таймер при каждом событии
        ScheduleFileChangedNotification(context);
    }

    /// <summary>
    /// Обработчик события переименования файла.
    /// </summary>
    private void OnFileRenamed(Guid sessionId, RenamedEventArgs e)
    {
        if (!_watchers.TryGetValue(sessionId, out var context))
        {
            return;
        }

        _logger.LogInformation(
            "File renamed for session {SessionId}: {OldName} -> {NewName}",
            sessionId,
            e.OldFullPath,
            e.FullPath);

        // При переименовании также уведомляем через debounce
        ScheduleFileChangedNotification(context);
    }

    /// <summary>
    /// Обработчик ошибок FileSystemWatcher.
    /// </summary>
    private void OnWatcherError(Guid sessionId, ErrorEventArgs e)
    {
        var exception = e.GetException();

        _logger.LogError(
            exception,
            "FileSystemWatcher error for session {SessionId}",
            sessionId);
    }

    /// <summary>
    /// Планирует отправку уведомления об изменении файла с debounce механизмом.
    /// </summary>
    /// <param name="context">Контекст watcher'а.</param>
    private void ScheduleFileChangedNotification(WatcherContext context)
    {
        lock (context.TimerLock)
        {
            // Останавливаем предыдущий таймер если был запущен
            context.DebounceTimer?.Dispose();

            // Создаём новый таймер с задержкой 200ms
            context.DebounceTimer = new Timer(
                state => NotifyFileChanged(context),
                null,
                TimeSpan.FromMilliseconds(200),
                Timeout.InfiniteTimeSpan); // Однократное срабатывание

            context.LastEventTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Отправляет уведомление об изменении файла через событие FileChanged.
    /// </summary>
    /// <param name="context">Контекст watcher'а.</param>
    private void NotifyFileChanged(WatcherContext context)
    {
        try
        {
            // Получаем текущий размер файла
            var fileInfo = new FileInfo(context.FilePath);

            if (!fileInfo.Exists)
            {
                _logger.LogWarning(
                    "File no longer exists for session {SessionId}: {FilePath}",
                    context.SessionId,
                    context.FilePath);
                return;
            }

            var args = new FileChangedEventArgs
            {
                SessionId = context.SessionId,
                FilePath = context.FilePath,
                NewSize = fileInfo.Length
            };

            _logger.LogInformation(
                "Notifying file change for session {SessionId}: {FilePath} (size: {Size} bytes)",
                context.SessionId,
                context.FilePath,
                fileInfo.Length);

            FileChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error notifying file change for session {SessionId}",
                context.SessionId);
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
            lock (_lockObject)
            {
                // Останавливаем все активные watchers
                foreach (var sessionId in _watchers.Keys.ToList())
                {
                    StopWatchingInternal(sessionId);
                }

                _watchers.Clear();
            }

            _logger.LogInformation("FileWatcherService disposed");
        }

        _disposed = true;
    }

    /// <summary>
    /// Контекст для отслеживания одного файла.
    /// </summary>
    private class WatcherContext
    {
        /// <summary>
        /// ID сессии.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Полный путь к отслеживаемому файлу.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// FileSystemWatcher для мониторинга файла.
        /// </summary>
        public FileSystemWatcher Watcher { get; set; } = null!;

        /// <summary>
        /// Таймер для debounce механизма.
        /// </summary>
        public Timer? DebounceTimer { get; set; }

        /// <summary>
        /// Время последнего события изменения файла.
        /// </summary>
        public DateTime LastEventTime { get; set; }

        /// <summary>
        /// Блокировка для синхронизации доступа к таймеру.
        /// </summary>
        public object TimerLock { get; } = new();
    }
}
