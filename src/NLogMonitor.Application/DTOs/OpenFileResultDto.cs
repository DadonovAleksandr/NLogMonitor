namespace NLogMonitor.Application.DTOs;

public class OpenFileResultDto
{
    public Guid SessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int TotalEntries { get; set; }
    public Dictionary<string, int> LevelCounts { get; set; } = new();
}
