using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

/// <summary>
/// Service interface for log file operations and querying.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Opens a log file and creates a session.
    /// </summary>
    Task<Guid> OpenFileAsync(string filePath, CancellationToken cancellationToken = default, Guid? sessionId = null);

    /// <summary>
    /// Opens the last log file in a directory and creates a session.
    /// </summary>
    Task<Guid> OpenDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves log entries with filtering and pagination.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="searchText">Search text to filter messages.</param>
    /// <param name="minLevel">Minimum log level (ignored if levels is specified).</param>
    /// <param name="maxLevel">Maximum log level (ignored if levels is specified).</param>
    /// <param name="levels">Specific log levels to filter (takes precedence over minLevel/maxLevel).</param>
    /// <param name="fromDate">Filter logs from this date.</param>
    /// <param name="toDate">Filter logs until this date.</param>
    /// <param name="logger">Filter by logger name.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>Filtered entries and total count.</returns>
    Task<(IEnumerable<LogEntry> Entries, int TotalCount)> GetLogsAsync(
        Guid sessionId,
        string? searchText = null,
        LogLevel? minLevel = null,
        LogLevel? maxLevel = null,
        IEnumerable<LogLevel>? levels = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? logger = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Gets a log session by ID.
    /// </summary>
    Task<LogSession?> GetSessionAsync(Guid sessionId);
}
