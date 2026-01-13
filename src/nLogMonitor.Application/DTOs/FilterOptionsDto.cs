namespace nLogMonitor.Application.DTOs;

/// <summary>
/// DTO for filtering log entries.
/// </summary>
public class FilterOptionsDto
{
    /// <summary>
    /// Search text to filter messages (case-insensitive).
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Minimum log level (Trace, Debug, Info, Warn, Error, Fatal).
    /// Used for range filtering. Ignored if Levels is specified.
    /// </summary>
    public string? MinLevel { get; set; }

    /// <summary>
    /// Maximum log level (Trace, Debug, Info, Warn, Error, Fatal).
    /// Used for range filtering. Ignored if Levels is specified.
    /// </summary>
    public string? MaxLevel { get; set; }

    /// <summary>
    /// Specific log levels to filter (Trace, Debug, Info, Warn, Error, Fatal).
    /// When specified, takes precedence over MinLevel/MaxLevel.
    /// </summary>
    public List<string>? Levels { get; set; }

    /// <summary>
    /// Filter logs from this date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter logs until this date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by logger name (case-insensitive substring match).
    /// </summary>
    public string? Logger { get; set; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (1-500).
    /// </summary>
    public int PageSize { get; set; } = 50;
}
