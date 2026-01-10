namespace nLogMonitor.Application.DTOs;

/// <summary>
/// DTO for client-side log entries sent from the frontend.
/// </summary>
public class ClientLogDto
{
    /// <summary>
    /// Log level (Trace, Debug, Info, Warn, Error, Fatal).
    /// Aliases: warning -> warn, fatal/critical -> error.
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Log message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Logger name/category.
    /// </summary>
    public string? Logger { get; set; }

    /// <summary>
    /// URL where the log was generated.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Browser user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// User identifier (if authenticated).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Application version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Session identifier for correlation.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Timestamp when the log was generated on the client.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Additional context data.
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}
