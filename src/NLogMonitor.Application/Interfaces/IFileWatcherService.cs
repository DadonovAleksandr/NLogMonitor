namespace NLogMonitor.Application.Interfaces;

public interface IFileWatcherService
{
    Task StartWatchingAsync(Guid sessionId, string filePath);
    Task StopWatchingAsync(Guid sessionId);
    bool IsWatching(Guid sessionId);
    event EventHandler<FileChangedEventArgs>? FileChanged;
}

public class FileChangedEventArgs : EventArgs
{
    public Guid SessionId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long NewSize { get; set; }
}
