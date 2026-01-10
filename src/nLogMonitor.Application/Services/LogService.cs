using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.Exceptions;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Application.Services;

/// <summary>
/// Сервис для работы с логами: открытие файлов, фильтрация, пагинация.
/// </summary>
public class LogService : ILogService
{
    private readonly ILogParser _logParser;
    private readonly ISessionStorage _sessionStorage;
    private readonly IDirectoryScanner _directoryScanner;
    private readonly ILogger<LogService> _logger;
    private readonly SessionSettings _sessionSettings;

    public LogService(
        ILogParser logParser,
        ISessionStorage sessionStorage,
        IDirectoryScanner directoryScanner,
        ILogger<LogService> logger,
        IOptions<SessionSettings> sessionSettings)
    {
        _logParser = logParser ?? throw new ArgumentNullException(nameof(logParser));
        _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
        _directoryScanner = directoryScanner ?? throw new ArgumentNullException(nameof(directoryScanner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionSettings = sessionSettings?.Value ?? throw new ArgumentNullException(nameof(sessionSettings));
    }

    /// <inheritdoc />
    public async Task<Guid> OpenFileAsync(string filePath, CancellationToken cancellationToken = default, Guid? sessionId = null)
    {
        _logger.LogInformation("Opening log file: {FilePath}", filePath);

        // Проверяем существование файла
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Log file not found: {filePath}", filePath);
        }

        var fileInfo = new FileInfo(filePath);
        var actualSessionId = sessionId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Создаём сессию
        var session = new LogSession
        {
            Id = actualSessionId,
            FileName = fileInfo.Name,
            FilePath = filePath,
            FileSize = fileInfo.Length,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_sessionSettings.FallbackTtlMinutes),
            Entries = new List<LogEntry>(),
            LevelCounts = new Dictionary<LogLevel, int>()
        };

        // Инициализируем счётчики для всех уровней
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            session.LevelCounts[level] = 0;
        }

        _logger.LogDebug("Starting to parse file: {FilePath}, Size: {FileSize} bytes", filePath, fileInfo.Length);

        // Парсим файл
        long entryId = 0;
        await foreach (var entry in _logParser.ParseAsync(filePath, cancellationToken))
        {
            entry.Id = ++entryId;
            session.Entries.Add(entry);

            // Подсчитываем записи по уровням
            session.LevelCounts[entry.Level]++;
        }

        // Устанавливаем LastReadPosition в конец файла после первоначального парсинга
        session.LastReadPosition = fileInfo.Length;

        _logger.LogInformation(
            "Parsed log file: {FilePath}, Entries: {EntryCount}, Levels: {LevelCounts}",
            filePath,
            session.Entries.Count,
            FormatLevelCounts(session.LevelCounts));

        // Сохраняем сессию
        await _sessionStorage.SaveAsync(session);

        _logger.LogInformation(
            "Session created: {SessionId} for file {FileName} with {EntryCount} entries",
            actualSessionId,
            fileInfo.Name,
            session.Entries.Count);

        return actualSessionId;
    }

    /// <inheritdoc />
    public async Task<Guid> OpenDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Opening directory: {DirectoryPath}", directoryPath);

        // Находим последний лог-файл в директории
        var lastLogFile = await _directoryScanner.FindLastLogFileByNameAsync(directoryPath, cancellationToken);

        if (string.IsNullOrEmpty(lastLogFile))
        {
            _logger.LogWarning("No log files found in directory: {DirectoryPath}", directoryPath);
            throw new NoLogFilesFoundException(directoryPath);
        }

        _logger.LogInformation("Found last log file: {LogFile}", lastLogFile);

        // Открываем найденный файл
        return await OpenFileAsync(lastLogFile, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<LogEntry> Entries, int TotalCount)> GetLogsAsync(
        Guid sessionId,
        string? searchText = null,
        LogLevel? minLevel = null,
        LogLevel? maxLevel = null,
        IEnumerable<LogLevel>? levels = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? logger = null,
        int page = 1,
        int pageSize = 50)
    {
        // Конвертируем массив levels в список для удобства
        var levelsList = levels?.ToList();
        var levelsString = levelsList != null && levelsList.Any()
            ? string.Join(", ", levelsList)
            : "null";

        _logger.LogDebug(
            "GetLogsAsync: SessionId={SessionId}, SearchText={SearchText}, MinLevel={MinLevel}, MaxLevel={MaxLevel}, Levels=[{Levels}], FromDate={FromDate}, ToDate={ToDate}, Logger={Logger}, Page={Page}, PageSize={PageSize}",
            sessionId, searchText, minLevel, maxLevel, levelsString, fromDate, toDate, logger, page, pageSize);

        // Получаем сессию
        var session = await _sessionStorage.GetAsync(sessionId);

        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return (Enumerable.Empty<LogEntry>(), 0);
        }

        // Применяем фильтры через LINQ
        IEnumerable<LogEntry> filteredEntries = session.Entries;

        // Фильтр по тексту в сообщении
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filteredEntries = filteredEntries.Where(e =>
                e.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Фильтр по уровням: если указан массив levels, используем его, иначе minLevel/maxLevel
        if (levelsList != null)
        {
            if (levelsList.Any())
            {
                // Фильтруем по точному совпадению из массива
                var levelSet = new HashSet<LogLevel>(levelsList);
                filteredEntries = filteredEntries.Where(e => levelSet.Contains(e.Level));
            }
            else
            {
                // Пустой массив levels -> возвращаем 0 записей (режим NONE)
                filteredEntries = Enumerable.Empty<LogEntry>();
            }
        }
        else
        {
            // levels == null -> используем minLevel/maxLevel (или все записи если не указаны)
            // Фильтр по минимальному уровню
            if (minLevel.HasValue)
            {
                filteredEntries = filteredEntries.Where(e => e.Level >= minLevel.Value);
            }

            // Фильтр по максимальному уровню
            if (maxLevel.HasValue)
            {
                filteredEntries = filteredEntries.Where(e => e.Level <= maxLevel.Value);
            }
        }

        // Фильтр по начальной дате
        if (fromDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.Timestamp >= fromDate.Value);
        }

        // Фильтр по конечной дате
        if (toDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.Timestamp <= toDate.Value);
        }

        // Фильтр по логгеру
        if (!string.IsNullOrWhiteSpace(logger))
        {
            filteredEntries = filteredEntries.Where(e =>
                e.Logger.Contains(logger, StringComparison.OrdinalIgnoreCase));
        }

        // Материализуем отфильтрованный список для подсчёта и пагинации
        var filteredList = filteredEntries.ToList();
        var totalCount = filteredList.Count;

        // Применяем пагинацию
        var pagedEntries = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        _logger.LogDebug(
            "GetLogsAsync result: SessionId={SessionId}, TotalFiltered={TotalCount}, PageEntries={PageEntries}",
            sessionId, totalCount, pagedEntries.Count);

        return (pagedEntries, totalCount);
    }

    /// <inheritdoc />
    public Task<LogSession?> GetSessionAsync(Guid sessionId)
    {
        _logger.LogDebug("GetSessionAsync: SessionId={SessionId}", sessionId);
        return _sessionStorage.GetAsync(sessionId);
    }

    /// <summary>
    /// Форматирует счётчики уровней для логирования.
    /// </summary>
    private static string FormatLevelCounts(Dictionary<LogLevel, int> levelCounts)
    {
        var nonZeroCounts = levelCounts
            .Where(kv => kv.Value > 0)
            .Select(kv => $"{kv.Key}={kv.Value}");

        return string.Join(", ", nonZeroCounts);
    }
}
