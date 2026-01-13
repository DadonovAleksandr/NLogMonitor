namespace nLogMonitor.Application.Configuration;

/// <summary>
/// Configuration settings for file handling.
/// </summary>
public class FileSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "FileSettings";

    /// <summary>
    /// Maximum allowed file size in megabytes. Default: 100 MB.
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Allowed file extensions for upload. Default: [".log", ".txt"].
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [".log", ".txt"];

    /// <summary>
    /// Temporary directory for uploaded files. Default: "/app/temp".
    /// </summary>
    public string TempDirectory { get; set; } = "/app/temp";

    /// <summary>
    /// Maximum file size in bytes (calculated from MaxFileSizeMB).
    /// </summary>
    public long MaxFileSizeBytes => MaxFileSizeMB * 1024L * 1024L;
}
