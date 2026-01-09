using System.Text.Json;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Infrastructure.Export;

/// <summary>
/// Exports log entries to JSON format with streaming.
/// </summary>
public sealed class JsonExporter : ILogExporter
{
    public string Format => "json";
    public string ContentType => "application/json";
    public string FileExtension => ".json";

    public async Task ExportToStreamAsync(IEnumerable<LogEntry> entries, Stream outputStream, CancellationToken cancellationToken = default)
    {
        await using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions
        {
            Indented = false
        });

        writer.WriteStartArray();

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteStartObject();
            writer.WriteNumber("id", entry.Id);
            writer.WriteString("timestamp", entry.Timestamp);
            writer.WriteString("level", entry.Level.ToString());
            writer.WriteString("message", entry.Message);
            writer.WriteString("logger", entry.Logger);
            writer.WriteNumber("processId", entry.ProcessId);
            writer.WriteNumber("threadId", entry.ThreadId);
            if (entry.Exception != null)
                writer.WriteString("exception", entry.Exception);
            else
                writer.WriteNull("exception");
            writer.WriteEndObject();

            // Flush periodically to avoid memory buildup
            if (writer.BytesPending > 16384)
            {
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
