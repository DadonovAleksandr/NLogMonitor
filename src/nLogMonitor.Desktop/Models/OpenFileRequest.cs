namespace nLogMonitor.Desktop.Models;

/// <summary>
/// Request model for opening a log file by absolute path.
/// </summary>
public class OpenFileRequest
{
    /// <summary>
    /// Absolute path to the log file.
    /// </summary>
    /// <example>C:\logs\application.log</example>
    public string FilePath { get; set; } = string.Empty;
}
