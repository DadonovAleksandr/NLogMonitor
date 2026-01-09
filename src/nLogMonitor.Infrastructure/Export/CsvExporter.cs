using System.Globalization;
using System.Text;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Infrastructure.Export;

/// <summary>
/// Exports log entries to CSV format with proper escaping and UTF-8 BOM for Excel compatibility.
/// </summary>
public sealed class CsvExporter : ILogExporter
{
    public string Format => "csv";
    public string ContentType => "text/csv";
    public string FileExtension => ".csv";

    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffff";
    private const string Header = "Id,Timestamp,Level,Message,Logger,ProcessId,ThreadId,Exception";

    // UTF-8 BOM for Excel compatibility
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    public async Task<Stream> ExportAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        // Write UTF-8 BOM
        await memoryStream.WriteAsync(Utf8Bom, cancellationToken).ConfigureAwait(false);

        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

        // Write header
        await writer.WriteLineAsync(Header).ConfigureAwait(false);

        // Write entries
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = FormatCsvLine(entry);
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

        memoryStream.Position = 0;
        return memoryStream;
    }

    private static string FormatCsvLine(LogEntry entry)
    {
        var sb = new StringBuilder();

        // Id
        sb.Append(entry.Id);
        sb.Append(',');

        // Timestamp
        sb.Append(entry.Timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture));
        sb.Append(',');

        // Level
        sb.Append(entry.Level.ToString());
        sb.Append(',');

        // Message (needs escaping)
        sb.Append(EscapeCsvField(entry.Message));
        sb.Append(',');

        // Logger (needs escaping for safety)
        sb.Append(EscapeCsvField(entry.Logger));
        sb.Append(',');

        // ProcessId
        sb.Append(entry.ProcessId);
        sb.Append(',');

        // ThreadId
        sb.Append(entry.ThreadId);
        sb.Append(',');

        // Exception (needs escaping)
        sb.Append(EscapeCsvField(entry.Exception));

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a CSV field according to RFC 4180.
    /// Fields containing comma, double quote, or newline must be quoted.
    /// Double quotes within the field must be escaped by doubling them.
    /// </summary>
    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Check if escaping is needed
        bool needsQuoting = value.Contains(',') ||
                            value.Contains('"') ||
                            value.Contains('\n') ||
                            value.Contains('\r');

        if (!needsQuoting)
            return value;

        // Escape double quotes by doubling them
        var escaped = value.Replace("\"", "\"\"");

        // Wrap in quotes
        return $"\"{escaped}\"";
    }
}
