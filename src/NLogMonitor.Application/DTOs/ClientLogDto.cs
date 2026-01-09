namespace NLogMonitor.Application.DTOs;

public class ClientLogDto
{
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? Timestamp { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}
