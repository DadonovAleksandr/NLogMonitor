namespace nLogMonitor.Application.Interfaces;

public interface IDirectoryScanner
{
    Task<string?> FindLastLogFileByNameAsync(string directoryPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetLogFilesAsync(string directoryPath, CancellationToken cancellationToken = default);
}
