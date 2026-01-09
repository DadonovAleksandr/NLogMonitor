namespace nLogMonitor.Api.Models;

/// <summary>
/// Request model for opening a directory with log files.
/// The system will automatically select the most recent .log file.
/// </summary>
public class OpenDirectoryRequest
{
    /// <summary>
    /// Absolute path to the directory containing log files.
    /// </summary>
    /// <example>C:\logs</example>
    public string DirectoryPath { get; set; } = string.Empty;
}
