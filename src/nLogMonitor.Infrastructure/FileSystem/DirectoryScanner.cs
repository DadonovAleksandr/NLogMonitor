using nLogMonitor.Application.Interfaces;

namespace nLogMonitor.Infrastructure.FileSystem;

public class DirectoryScanner : IDirectoryScanner
{
    private const string LogFilePattern = "*.log";

    public Task<string?> FindLastLogFileByNameAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var logFiles = GetSortedLogFiles(directoryPath);
        var lastFile = logFiles.FirstOrDefault();

        return Task.FromResult(lastFile);
    }

    public Task<IEnumerable<string>> GetLogFilesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var logFiles = GetSortedLogFiles(directoryPath);

        return Task.FromResult<IEnumerable<string>>(logFiles);
    }

    private static List<string> GetSortedLogFiles(string directoryPath)
    {
        // Directory.GetFiles may throw UnauthorizedAccessException if no access rights
        var files = Directory.GetFiles(directoryPath, LogFilePattern, SearchOption.TopDirectoryOnly);

        // Sort by file name in descending order (last name = most recent date for ${shortdate}.log format)
        return files
            .OrderByDescending(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
