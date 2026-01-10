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

    /// <summary>
    /// Последняя прочитанная позиция в байтах для инкрементального чтения файла.
    /// Используется FileWatcherService для чтения только новых записей.
    /// </summary>
    public long LastReadPosition { get; set; }

    // Статистика по уровням
    public Dictionary<LogLevel, int> LevelCounts { get; set; } = new();
}
