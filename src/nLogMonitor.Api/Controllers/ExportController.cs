using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Api.Controllers;

/// <summary>
/// Controller for exporting log entries to various formats.
/// </summary>
[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly IEnumerable<ILogExporter> _exporters;
    private readonly ILogger<ExportController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportController"/> class.
    /// </summary>
    /// <param name="logService">Log service for accessing sessions and entries.</param>
    /// <param name="exporters">Collection of registered log exporters.</param>
    /// <param name="logger">Logger instance.</param>
    public ExportController(
        ILogService logService,
        IEnumerable<ILogExporter> exporters,
        ILogger<ExportController> logger)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _exporters = exporters ?? throw new ArgumentNullException(nameof(exporters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports log entries for the specified session to JSON or CSV format.
    /// Uses streaming to minimize memory usage for large exports.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="format">Export format: "json" or "csv" (default: "json").</param>
    /// <param name="search">Optional text search in message field.</param>
    /// <param name="minLevel">Optional minimum log level filter.</param>
    /// <param name="maxLevel">Optional maximum log level filter.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="logger">Optional logger name filter (partial match).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File stream with exported log entries.</returns>
    /// <response code="200">Returns the exported file.</response>
    /// <response code="400">Unsupported export format.</response>
    /// <response code="404">Session not found.</response>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportLogs(
        [FromRoute] Guid sessionId,
        [FromQuery] string format = "json",
        [FromQuery] string? search = null,
        [FromQuery] LogLevel? minLevel = null,
        [FromQuery] LogLevel? maxLevel = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? logger = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Export request: SessionId={SessionId}, Format={Format}, Search={Search}, MinLevel={MinLevel}, MaxLevel={MaxLevel}, FromDate={FromDate}, ToDate={ToDate}, Logger={Logger}",
            sessionId, format, search, minLevel, maxLevel, fromDate, toDate, logger);

        // Find exporter by format (case-insensitive)
        var exporter = _exporters.FirstOrDefault(e =>
            e.Format.Equals(format, StringComparison.OrdinalIgnoreCase));

        if (exporter == null)
        {
            var supportedFormats = string.Join(", ", _exporters.Select(e => e.Format));
            _logger.LogWarning("Unsupported export format: {Format}. Supported: {SupportedFormats}", format, supportedFormats);
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = $"Unsupported export format. Supported: {supportedFormats}",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Get session
        var session = await _logService.GetSessionAsync(sessionId);

        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return NotFound(new ApiErrorResponse
            {
                Error = "NotFound",
                Message = $"Session {sessionId} not found.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Apply filters to entries
        var filteredEntries = ApplyFilters(session.Entries, search, minLevel, maxLevel, fromDate, toDate, logger);

        // Generate filename
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var filename = $"logs_{sessionId}_{timestamp}{exporter.FileExtension}";

        _logger.LogInformation("Starting streaming export for session {SessionId} in {Format} format", sessionId, format);

        // Stream directly to response
        Response.ContentType = exporter.ContentType;
        Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";

        await exporter.ExportToStreamAsync(filteredEntries, Response.Body, cancellationToken);

        _logger.LogInformation("Export completed: {Filename}, ContentType={ContentType}", filename, exporter.ContentType);

        return new EmptyResult();
    }

    /// <summary>
    /// Applies filter criteria to log entries.
    /// </summary>
    private static IEnumerable<LogEntry> ApplyFilters(
        IEnumerable<LogEntry> entries,
        string? searchText,
        LogLevel? minLevel,
        LogLevel? maxLevel,
        DateTime? fromDate,
        DateTime? toDate,
        string? logger)
    {
        var filtered = entries;

        // Filter by text in message
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(e =>
                e.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by minimum level
        if (minLevel.HasValue)
        {
            filtered = filtered.Where(e => e.Level >= minLevel.Value);
        }

        // Filter by maximum level
        if (maxLevel.HasValue)
        {
            filtered = filtered.Where(e => e.Level <= maxLevel.Value);
        }

        // Filter by start date
        if (fromDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp >= fromDate.Value);
        }

        // Filter by end date
        if (toDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp <= toDate.Value);
        }

        // Filter by logger name
        if (!string.IsNullOrWhiteSpace(logger))
        {
            filtered = filtered.Where(e =>
                e.Logger.Contains(logger, StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }
}
