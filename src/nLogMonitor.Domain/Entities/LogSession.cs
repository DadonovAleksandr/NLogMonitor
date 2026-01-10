namespace nLogMonitor.Domain.Entities;

public class LogSession
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<LogEntry> Entries { get; set; } = new();

    // Статистика по уровням
    public Dictionary<LogLevel, int> LevelCounts { get; set; } = new();
}
