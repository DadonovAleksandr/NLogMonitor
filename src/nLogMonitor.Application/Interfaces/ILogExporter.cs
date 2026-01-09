using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

/// <summary>
/// Interface for exporting log entries to various formats.
/// </summary>
public interface ILogExporter
{
    /// <summary>
    /// Export format name (e.g., "json", "csv").
    /// </summary>
    string Format { get; }

    /// <summary>
    /// MIME content type (e.g., "application/json", "text/csv").
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// File extension including dot (e.g., ".json", ".csv").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Exports log entries to the provided stream in a streaming manner.
    /// Does not close the stream.
    /// </summary>
    /// <param name="entries">Entries to export.</param>
    /// <param name="outputStream">Target stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportToStreamAsync(IEnumerable<LogEntry> entries, Stream outputStream, CancellationToken cancellationToken = default);
}
