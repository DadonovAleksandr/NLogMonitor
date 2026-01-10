namespace nLogMonitor.Domain.Entities;

public class LogEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Logger { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public int ThreadId { get; set; }
    public string? Exception { get; set; }
}
