using System.Globalization;
using System.Text;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Infrastructure.Export;

/// <summary>
/// Exports log entries to CSV format with streaming.
/// </summary>
public sealed class CsvExporter : ILogExporter
{
    public string Format => "csv";
    public string ContentType => "text/csv";
    public string FileExtension => ".csv";

    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffff";
    private const string Header = "Id,Timestamp,Level,Message,Logger,ProcessId,ThreadId,Exception";
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    public async Task ExportToStreamAsync(IEnumerable<LogEntry> entries, Stream outputStream, CancellationToken cancellationToken = default)
    {
        // Write UTF-8 BOM
        await outputStream.WriteAsync(Utf8Bom, cancellationToken).ConfigureAwait(false);

        await using var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: 65536, leaveOpen: true);

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
    }

    private static string FormatCsvLine(LogEntry entry)
    {
        var sb = new StringBuilder();
        sb.Append(entry.Id);
        sb.Append(',');
        sb.Append(entry.Timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture));
        sb.Append(',');
        sb.Append(entry.Level.ToString());
        sb.Append(',');
        sb.Append(EscapeCsvField(entry.Message));
        sb.Append(',');
        sb.Append(EscapeCsvField(entry.Logger));
        sb.Append(',');
        sb.Append(entry.ProcessId);
        sb.Append(',');
        sb.Append(entry.ThreadId);
        sb.Append(',');
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

        bool needsQuoting = value.Contains(',') ||
                            value.Contains('"') ||
                            value.Contains('\n') ||
                            value.Contains('\r');

        if (!needsQuoting)
            return value;

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
