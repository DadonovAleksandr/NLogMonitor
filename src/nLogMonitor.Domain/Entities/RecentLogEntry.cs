namespace nLogMonitor.Domain.Entities;

public class RecentLogEntry
{
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public DateTime OpenedAt { get; set; }
}
