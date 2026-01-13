namespace nLogMonitor.Desktop.Models;

/// <summary>
/// Unified API error response model.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Error type/code (e.g., "NotFound", "BadRequest", "InternalServerError").
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Additional error details (stack trace, inner exception).
    /// Only populated in Development environment.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Unique trace ID for correlating logs and error reports.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;
}
