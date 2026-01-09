using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

public interface ILogExporter
{
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }
    Task<Stream> ExportAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);
}
