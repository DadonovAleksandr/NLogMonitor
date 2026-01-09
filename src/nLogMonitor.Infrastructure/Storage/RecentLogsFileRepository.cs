using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Infrastructure.Storage;

/// <summary>
/// Репозиторий для хранения недавно открытых лог-файлов в JSON-файле.
/// </summary>
public class RecentLogsFileRepository : IRecentLogsRepository
{
    private readonly string _storagePath;
    private readonly int _maxEntries;
    private readonly ILogger<RecentLogsFileRepository> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Создаёт новый экземпляр RecentLogsFileRepository.
    /// </summary>
    /// <param name="options">Настройки хранения недавних логов.</param>
    /// <param name="logger">Логгер.</param>
    public RecentLogsFileRepository(
        IOptions<RecentLogsSettings> options,
        ILogger<RecentLogsFileRepository> logger)
    {
        _logger = logger;
        var settings = options.Value;
        _maxEntries = settings.MaxEntries;

        _storagePath = GetStoragePath(settings.CustomStoragePath);

        _logger.LogInformation(
            "RecentLogsFileRepository initialized. StoragePath={StoragePath}, MaxEntries={MaxEntries}",
            _storagePath,
            _maxEntries);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RecentLogEntry>> GetAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var entries = await ReadEntriesAsync();
            return entries.OrderByDescending(e => e.OpenedAt);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(RecentLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Path))
        {
            throw new ArgumentException("Path cannot be empty", nameof(entry));
        }

        await _semaphore.WaitAsync();
        try
        {
            var entries = await ReadEntriesAsync();

            // Удаляем существующую запись с таким же путём (для обновления)
            var existingIndex = entries.FindIndex(e =>
                string.Equals(e.Path, entry.Path, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                entries.RemoveAt(existingIndex);
                _logger.LogDebug("Updated existing entry: {Path}", entry.Path);
            }
            else
            {
                _logger.LogDebug("Adding new entry: {Path}", entry.Path);
            }

            // Добавляем в начало списка (самый новый)
            entries.Insert(0, entry);

            // Ограничиваем количество записей
            if (entries.Count > _maxEntries)
            {
                var removedCount = entries.Count - _maxEntries;
                entries.RemoveRange(_maxEntries, removedCount);
                _logger.LogDebug("Removed {Count} oldest entries", removedCount);
            }

            await WriteEntriesAsync(entries);

            _logger.LogInformation(
                "Recent entry saved: {Path}, IsDirectory={IsDirectory}, OpenedAt={OpenedAt}",
                entry.Path,
                entry.IsDirectory,
                entry.OpenedAt);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await WriteEntriesAsync(new List<RecentLogEntry>());
            _logger.LogInformation("Recent entries cleared");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Определяет путь к файлу хранения.
    /// </summary>
    private static string GetStoragePath(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return customPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localAppData, "nLogMonitor");
        return Path.Combine(appFolder, "recent.json");
    }

    /// <summary>
    /// Читает записи из файла.
    /// </summary>
    private async Task<List<RecentLogEntry>> ReadEntriesAsync()
    {
        try
        {
            if (!File.Exists(_storagePath))
            {
                _logger.LogDebug("Storage file does not exist: {Path}", _storagePath);
                return new List<RecentLogEntry>();
            }

            var json = await File.ReadAllTextAsync(_storagePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogDebug("Storage file is empty: {Path}", _storagePath);
                return new List<RecentLogEntry>();
            }

            var entries = JsonSerializer.Deserialize<List<RecentLogEntry>>(json, JsonOptions);

            if (entries == null)
            {
                _logger.LogWarning("Failed to deserialize storage file (null result): {Path}", _storagePath);
                return new List<RecentLogEntry>();
            }

            _logger.LogDebug("Loaded {Count} entries from storage", entries.Count);
            return entries;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Corrupted storage file, returning empty list: {Path}", _storagePath);
            return new List<RecentLogEntry>();
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read storage file: {Path}", _storagePath);
            return new List<RecentLogEntry>();
        }
    }

    /// <summary>
    /// Записывает записи в файл.
    /// </summary>
    private async Task WriteEntriesAsync(List<RecentLogEntry> entries)
    {
        try
        {
            // Создаём директорию если не существует
            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created storage directory: {Directory}", directory);
            }

            var json = JsonSerializer.Serialize(entries, JsonOptions);
            await File.WriteAllTextAsync(_storagePath, json);

            _logger.LogDebug("Saved {Count} entries to storage", entries.Count);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to write storage file: {Path}", _storagePath);
            throw;
        }
    }
}
