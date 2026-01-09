using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

public interface ILogParser
{
    IAsyncEnumerable<LogEntry> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
    IAsyncEnumerable<LogEntry> ParseAsync(string filePath, CancellationToken cancellationToken = default);
    bool CanParse(string line);
}
