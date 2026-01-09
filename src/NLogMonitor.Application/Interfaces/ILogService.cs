using NLogMonitor.Domain.Entities;

namespace NLogMonitor.Application.Interfaces;

public interface ILogService
{
    Task<Guid> OpenFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<Guid> OpenDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
    Task<(IEnumerable<LogEntry> Entries, int TotalCount)> GetLogsAsync(
        Guid sessionId, 
        string? searchText = null,
        LogLevel? minLevel = null,
        LogLevel? maxLevel = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? logger = null,
        int page = 1,
        int pageSize = 50);
    Task<LogSession?> GetSessionAsync(Guid sessionId);
}
