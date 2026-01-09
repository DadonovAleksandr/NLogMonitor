using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Api.Controllers;

/// <summary>
/// Controller for retrieving and filtering log entries.
/// </summary>
[ApiController]
[Route("api/logs")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly IValidator<FilterOptionsDto> _validator;
    private readonly ILogger<LogsController> _logger;

    /// <summary>
    /// Initializes a new instance of the LogsController.
    /// </summary>
    /// <param name="logService">Log service for accessing log data.</param>
    /// <param name="validator">Validator for filter options.</param>
    /// <param name="logger">Logger instance.</param>
    public LogsController(
        ILogService logService,
        IValidator<FilterOptionsDto> validator,
        ILogger<LogsController> logger)
    {
        _logService = logService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves logs for a session with filtering and pagination.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="search">Search text to filter messages.</param>
    /// <param name="minLevel">Minimum log level (Trace, Debug, Info, Warn, Error, Fatal).</param>
    /// <param name="maxLevel">Maximum log level (Trace, Debug, Info, Warn, Error, Fatal).</param>
    /// <param name="fromDate">Filter logs from this date.</param>
    /// <param name="toDate">Filter logs until this date.</param>
    /// <param name="logger">Filter by logger name.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing log entries.</returns>
    /// <response code="200">Returns the paged log entries.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="404">Session not found.</response>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(PagedResultDto<LogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResultDto<LogEntryDto>>> GetLogs(
        [FromRoute] Guid sessionId,
        [FromQuery] string? search,
        [FromQuery] string? minLevel,
        [FromQuery] string? maxLevel,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? logger,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // Build filter options DTO
        var filterOptions = new FilterOptionsDto
        {
            SearchText = search,
            MinLevel = minLevel,
            MaxLevel = maxLevel,
            FromDate = fromDate,
            ToDate = toDate,
            Logger = logger,
            Page = page,
            PageSize = pageSize
        };

        // Validate filter options
        var validationResult = await _validator.ValidateAsync(filterOptions, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Invalid filter options for session {SessionId}: {Errors}", sessionId, errors);

            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = errors,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check if session exists
        var session = await _logService.GetSessionAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);

            return NotFound(new ApiErrorResponse
            {
                Error = "NotFound",
                Message = $"Session with ID '{sessionId}' was not found.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Parse log levels
        LogLevel? parsedMinLevel = ParseLogLevel(minLevel);
        LogLevel? parsedMaxLevel = ParseLogLevel(maxLevel);

        _logger.LogDebug(
            "Getting logs for session {SessionId}: Page={Page}, PageSize={PageSize}, MinLevel={MinLevel}, MaxLevel={MaxLevel}, Search={Search}",
            sessionId, page, pageSize, minLevel, maxLevel, search);

        // Get logs from service
        var (entries, totalCount) = await _logService.GetLogsAsync(
            sessionId,
            searchText: search,
            minLevel: parsedMinLevel,
            maxLevel: parsedMaxLevel,
            fromDate: fromDate,
            toDate: toDate,
            logger: logger,
            page: page,
            pageSize: pageSize);

        // Map to DTOs
        var logEntryDtos = entries.Select(MapToDto).ToList();

        var result = new PagedResultDto<LogEntryDto>
        {
            Items = logEntryDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        _logger.LogDebug(
            "Returning {Count} logs for session {SessionId} (total: {TotalCount}, page: {Page}/{TotalPages})",
            logEntryDtos.Count, sessionId, totalCount, page, result.TotalPages);

        return Ok(result);
    }

    private static LogLevel? ParseLogLevel(string? level)
    {
        if (string.IsNullOrEmpty(level))
            return null;

        return Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsedLevel)
            ? parsedLevel
            : null;
    }

    private static LogEntryDto MapToDto(Domain.Entities.LogEntry entry)
    {
        return new LogEntryDto
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            Level = entry.Level.ToString(),
            Message = entry.Message,
            Logger = entry.Logger,
            ProcessId = entry.ProcessId,
            ThreadId = entry.ThreadId,
            Exception = entry.Exception
        };
    }
}
