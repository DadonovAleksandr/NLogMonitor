using System.Text.Json;
using System.Text.Json.Serialization;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Infrastructure.Export;

/// <summary>
/// Exports log entries to JSON format with async streaming.
/// </summary>
public sealed class JsonExporter : ILogExporter
{
    public string Format => "json";
    public string ContentType => "application/json";
    public string FileExtension => ".json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<Stream> ExportAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        var exportEntries = entries.Select(e => new LogEntryExportModel
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            Level = e.Level.ToString(),
            Message = e.Message,
            Logger = e.Logger,
            ProcessId = e.ProcessId,
            ThreadId = e.ThreadId,
            Exception = e.Exception
        });

        await JsonSerializer.SerializeAsync(memoryStream, exportEntries, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Internal model for JSON export with string Level.
    /// </summary>
    private sealed class LogEntryExportModel
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Logger { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public int ThreadId { get; set; }
        public string? Exception { get; set; }
    }
}
