using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using nLogMonitor.Desktop.Models;
using nLogMonitor.Desktop.Validators;
using nLogMonitor.Application.DTOs;
using NLog;

namespace nLogMonitor.Desktop.Controllers;

/// <summary>
/// Controller for receiving client-side logs from the frontend.
/// </summary>
[ApiController]
[Route("api/client-logs")]
[Produces("application/json")]
[EnableRateLimiting("ClientLogs")]
public class ClientLogsController : ControllerBase
{
    private readonly IValidator<ClientLogDto> _validator;
    private readonly ILogger<ClientLogsController> _logger;

    // Separate NLog logger for client logs with [CLIENT] prefix
    private static readonly Logger ClientLogger = LogManager.GetLogger("ClientLogger");

    /// <summary>
    /// Initializes a new instance of the ClientLogsController.
    /// </summary>
    /// <param name="validator">Validator for client log entries.</param>
    /// <param name="logger">Logger instance.</param>
    public ClientLogsController(
        IValidator<ClientLogDto> validator,
        ILogger<ClientLogsController> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Receives a batch of client-side log entries from the frontend.
    /// </summary>
    /// <param name="logs">Array of client log entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status of the operation.</returns>
    /// <response code="200">Logs received successfully.</response>
    /// <response code="400">Invalid log entries.</response>
    /// <response code="429">Too many requests (rate limit exceeded).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClientLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ClientLogsResponse>> PostLogs(
        [FromBody] ClientLogDto[] logs,
        CancellationToken cancellationToken = default)
    {
        if (logs == null || logs.Length == 0)
        {
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = "No log entries provided.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var validationErrors = new List<string>();
        var processedCount = 0;

        foreach (var log in logs)
        {
            var validationResult = await _validator.ValidateAsync(log, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                validationErrors.Add($"Log #{processedCount + 1}: {errors}");
                processedCount++;
                continue;
            }

            // Sanitize input data
            var sanitizedLog = SanitizeLog(log);

            // Normalize log level
            var normalizedLevel = NormalizeLevel(sanitizedLog.Level);

            // Log with NLog using structured logging and [CLIENT] prefix
            LogClientEntry(sanitizedLog, normalizedLevel);

            processedCount++;
        }

        if (validationErrors.Count == logs.Length)
        {
            // All logs failed validation
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = $"All log entries failed validation: {string.Join(" | ", validationErrors)}",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogDebug("Received {Count} client logs, {FailedCount} failed validation",
            logs.Length, validationErrors.Count);

        return Ok(new ClientLogsResponse
        {
            Processed = logs.Length - validationErrors.Count,
            Failed = validationErrors.Count,
            Errors = validationErrors.Count > 0 ? validationErrors : null
        });
    }

    /// <summary>
    /// Normalizes log level aliases to standard NLog levels.
    /// </summary>
    /// <param name="level">Input log level.</param>
    /// <returns>Normalized log level.</returns>
    private static string NormalizeLevel(string level)
    {
        var lowerLevel = level.ToLowerInvariant();

        return lowerLevel switch
        {
            "warning" => "warn",
            "fatal" => "error",
            "critical" => "error",
            _ => lowerLevel
        };
    }

    /// <summary>
    /// Sanitizes log entry to prevent XSS and injection attacks.
    /// </summary>
    /// <param name="log">Original log entry.</param>
    /// <returns>Sanitized log entry.</returns>
    private static ClientLogDto SanitizeLog(ClientLogDto log)
    {
        return new ClientLogDto
        {
            Level = SanitizeString(log.Level, 50) ?? string.Empty,
            Message = SanitizeString(log.Message, ClientLogDtoValidator.MaxMessageLength) ?? string.Empty,
            Logger = SanitizeString(log.Logger, ClientLogDtoValidator.MaxShortStringLength),
            Url = SanitizeString(log.Url, ClientLogDtoValidator.MaxUrlLength),
            UserAgent = SanitizeString(log.UserAgent, ClientLogDtoValidator.MaxShortStringLength),
            UserId = SanitizeString(log.UserId, ClientLogDtoValidator.MaxShortStringLength),
            Version = SanitizeString(log.Version, ClientLogDtoValidator.MaxShortStringLength),
            SessionId = SanitizeString(log.SessionId, ClientLogDtoValidator.MaxShortStringLength),
            Timestamp = log.Timestamp,
            Context = log.Context // Context is not sanitized as it may contain structured data
        };
    }

    /// <summary>
    /// Sanitizes a string by removing control characters and escaping HTML.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="maxLength">Maximum allowed length.</param>
    /// <returns>Sanitized string.</returns>
    private static string? SanitizeString(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove control characters (except newlines and tabs in message)
        var sanitized = RemoveControlCharacters(input);

        // Escape HTML tags to prevent XSS
        sanitized = EscapeHtml(sanitized);

        // Truncate to max length
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        return sanitized;
    }

    /// <summary>
    /// Removes control characters except tab and newline.
    /// </summary>
    private static string RemoveControlCharacters(string input)
    {
        var sb = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            // Allow printable characters, tab (0x09), newline (0x0A), carriage return (0x0D)
            if (!char.IsControl(c) || c == '\t' || c == '\n' || c == '\r')
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes HTML special characters to prevent XSS.
    /// </summary>
    private static string EscapeHtml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");
    }

    /// <summary>
    /// Logs client entry using NLog with structured logging.
    /// </summary>
    private static void LogClientEntry(ClientLogDto log, string normalizedLevel)
    {
        // Build the log message with [CLIENT] prefix
        var message = $"[CLIENT] {log.Message}";

        // Create log event with structured data
        var logEvent = new LogEventInfo
        {
            Message = message,
            LoggerName = log.Logger ?? "ClientLogger",
            TimeStamp = log.Timestamp ?? DateTime.UtcNow
        };

        // Set log level
        logEvent.Level = normalizedLevel.ToLowerInvariant() switch
        {
            "trace" => NLog.LogLevel.Trace,
            "debug" => NLog.LogLevel.Debug,
            "info" => NLog.LogLevel.Info,
            "warn" => NLog.LogLevel.Warn,
            "error" => NLog.LogLevel.Error,
            _ => NLog.LogLevel.Info
        };

        // Add structured context using MDLC (Mapped Diagnostic Logical Context)
        if (!string.IsNullOrEmpty(log.UserId))
            logEvent.Properties["UserId"] = log.UserId;

        if (!string.IsNullOrEmpty(log.Version))
            logEvent.Properties["Version"] = log.Version;

        if (!string.IsNullOrEmpty(log.Url))
            logEvent.Properties["Url"] = log.Url;

        if (!string.IsNullOrEmpty(log.UserAgent))
            logEvent.Properties["UserAgent"] = log.UserAgent;

        if (!string.IsNullOrEmpty(log.SessionId))
            logEvent.Properties["ClientSessionId"] = log.SessionId;

        // Add context dictionary if present
        if (log.Context != null)
        {
            foreach (var kvp in log.Context)
            {
                logEvent.Properties[$"Context_{kvp.Key}"] = kvp.Value?.ToString();
            }
        }

        ClientLogger.Log(logEvent);
    }
}

/// <summary>
/// Response model for client logs endpoint.
/// </summary>
public class ClientLogsResponse
{
    /// <summary>
    /// Number of log entries successfully processed.
    /// </summary>
    public int Processed { get; set; }

    /// <summary>
    /// Number of log entries that failed validation.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Validation error messages (if any).
    /// </summary>
    public List<string>? Errors { get; set; }
}
