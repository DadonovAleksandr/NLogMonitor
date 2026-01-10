using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using nLogMonitor.Desktop.Hubs;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Desktop.Services;

/// <summary>
/// Background service для обработки событий FileWatcherService и отправки обновлений через SignalR.
/// Подписывается на FileChanged события и отправляет новые записи логов клиентам через LogWatcherHub.
/// </summary>
public class FileWatcherBackgroundService : BackgroundService
{
    private readonly IFileWatcherService _fileWatcherService;
    private readonly IHubContext<LogWatcherHub> _hubContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FileWatcherBackgroundService> _logger;

    /// <summary>
    /// Семафоры для сериализации обработки событий per session.
    /// Предотвращает параллельное чтение одного и того же диапазона файла.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _sessionLocks = new();

    /// <summary>
    /// Создаёт новый экземпляр FileWatcherBackgroundService.
    /// </summary>
    /// <param name="fileWatcherService">Сервис мониторинга файлов.</param>
    /// <param name="hubContext">Контекст SignalR Hub для отправки обновлений.</param>
    /// <param name="serviceScopeFactory">Фабрика для создания scope (для получения scoped сервисов).</param>
    /// <param name="logger">Логгер.</param>
    public FileWatcherBackgroundService(
        IFileWatcherService fileWatcherService,
        IHubContext<LogWatcherHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FileWatcherBackgroundService> logger)
    {
        _fileWatcherService = fileWatcherService;
        _hubContext = hubContext;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Подписываемся на событие изменения файла
        _fileWatcherService.FileChanged += OnFileChanged;

        _logger.LogInformation("FileWatcherBackgroundService started");

        // Возвращаем завершённую задачу, так как обработка событий происходит асинхронно
        return Task.CompletedTask;
    }

    /// <summary>
    /// Обработчик события изменения файла.
    /// Читает только новые строки из файла (инкрементально) и отправляет их через SignalR.
    /// Сериализует обработку per session для предотвращения дубликатов.
    /// </summary>
    private async void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        // Получаем или создаём семафор для сериализации обработки этой сессии
        var sessionLock = _sessionLocks.GetOrAdd(e.SessionId, _ => new SemaphoreSlim(1, 1));

        // Ждём освобождения семафора (сериализация обработки для одной сессии)
        await sessionLock.WaitAsync();
        try
        {
            _logger.LogDebug(
                "Processing file change for session {SessionId}: {FilePath} (new size: {Size} bytes)",
                e.SessionId,
                e.FilePath,
                e.NewSize);

            IReadOnlyList<LogEntry> appendedEntries;
            long newPosition;
            long startPosition;

            // Создаём scope для получения scoped сервисов
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var logParser = scope.ServiceProvider.GetRequiredService<ILogParser>();
                var sessionStorage = scope.ServiceProvider.GetRequiredService<ISessionStorage>();

                // Получаем сессию для чтения LastReadPosition
                var session = await sessionStorage.GetAsync(e.SessionId);
                if (session == null)
                {
                    _logger.LogWarning(
                        "Session {SessionId} not found, skipping file change processing",
                        e.SessionId);

                    // Удаляем семафор для несуществующей сессии
                    CleanupSessionLock(e.SessionId);
                    return;
                }

                startPosition = session.LastReadPosition;

                // Проверяем: был ли файл усечён (truncated)?
                if (e.NewSize < startPosition)
                {
                    _logger.LogWarning(
                        "File {FilePath} was truncated (new size: {NewSize}, last position: {LastPosition}), re-parsing from beginning",
                        e.FilePath,
                        e.NewSize,
                        startPosition);
                    startPosition = 0;
                }

                // Парсим только новые записи
                var entries = new List<LogEntry>();
                await foreach (var entry in logParser.ParseFromPositionAsync(e.FilePath, startPosition))
                {
                    entries.Add(entry);
                }

                // Обновляем позицию: новая позиция = новый размер файла
                newPosition = e.NewSize;

                // Атомарно добавляем новые записи в сессию и получаем записи с назначенными ID
                appendedEntries = await sessionStorage.AppendEntriesAsync(e.SessionId, entries, newPosition);
            }

            if (appendedEntries.Count > 0)
            {
                // Формируем DTO ПОСЛЕ AppendEntriesAsync с корректными ID
                var newLogs = appendedEntries.Select(entry => new LogEntryDto
                {
                    Id = entry.Id, // ID уже назначен в AppendEntriesAsync
                    Timestamp = entry.Timestamp,
                    Level = entry.Level.ToString(),
                    Message = entry.Message,
                    Logger = entry.Logger,
                    ProcessId = entry.ProcessId,
                    ThreadId = entry.ThreadId,
                    Exception = entry.Exception
                }).ToList();

                // Отправляем новые записи всем подписчикам группы сессии
                await _hubContext.Clients
                    .Group(e.SessionId.ToString())
                    .SendAsync("NewLogs", newLogs);

                _logger.LogInformation(
                    "Sent {Count} new log entries to session {SessionId} (position: {OldPosition} -> {NewPosition})",
                    newLogs.Count,
                    e.SessionId,
                    startPosition,
                    newPosition);
            }
            else
            {
                _logger.LogDebug(
                    "No new logs parsed for session {SessionId}",
                    e.SessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing file change for session {SessionId}",
                e.SessionId);
        }
        finally
        {
            sessionLock.Release();
        }
    }

    /// <summary>
    /// Удаляет семафор для сессии и освобождает ресурсы.
    /// </summary>
    private void CleanupSessionLock(Guid sessionId)
    {
        if (_sessionLocks.TryRemove(sessionId, out var semaphore))
        {
            semaphore.Dispose();
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        // Отписываемся от события при остановке сервиса
        _fileWatcherService.FileChanged -= OnFileChanged;

        // Освобождаем все семафоры
        foreach (var kvp in _sessionLocks)
        {
            kvp.Value.Dispose();
        }
        _sessionLocks.Clear();

        _logger.LogInformation("FileWatcherBackgroundService stopped");

        base.Dispose();
    }
}
