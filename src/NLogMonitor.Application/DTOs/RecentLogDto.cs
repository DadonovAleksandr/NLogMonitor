namespace NLogMonitor.Application.DTOs;

public class RecentLogDto
{
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public DateTime OpenedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
