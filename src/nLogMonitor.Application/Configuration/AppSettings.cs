namespace nLogMonitor.Application.Configuration;

/// <summary>
/// Global application settings.
/// </summary>
public class AppSettings
{
    public const string SectionName = "App";

    /// <summary>
    /// Application mode: Web or Desktop.
    /// </summary>
    public AppMode Mode { get; set; } = AppMode.Web;
}

/// <summary>
/// Application operation mode.
/// </summary>
public enum AppMode
{
    /// <summary>
    /// Web mode - only upload-based file access.
    /// </summary>
    Web,

    /// <summary>
    /// Desktop mode - full filesystem access via native dialogs.
    /// </summary>
    Desktop
}
