using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Desktop.Models;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Desktop.Controllers;

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
    /// <param name="minLevel">Minimum log level (Trace, Debug, Info, Warn, Error, Fatal). Ignored if levels is specified.</param>
    /// <param name="maxLevel">Maximum log level (Trace, Debug, Info, Warn, Error, Fatal). Ignored if levels is specified.</param>
    /// <param name="levels">Specific log levels to filter (Trace, Debug, Info, Warn, Error, Fatal). Takes precedence over minLevel/maxLevel. Can be specified multiple times: ?levels=Error&amp;levels=Fatal</param>
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
        [FromQuery] List<string>? levels,
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
            Levels = levels,
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
        List<LogLevel>? parsedLevels = null;

        // Parse levels array if provided
        // ВАЖНО: различаем два случая:
        // 1. Параметр levels отсутствует в query string → используем minLevel/maxLevel
        // 2. Параметр levels присутствует в query string → используем levels (может быть пустой список для NONE режима)

        // Проверяем явно, был ли параметр levels указан в query string
        // Это критично, т.к. ASP.NET model binding создает пустой List даже когда параметр отсутствует
        if (HttpContext.Request.Query.ContainsKey("levels"))
        {
            parsedLevels = new List<LogLevel>();

            if (levels != null)
            {
                foreach (var level in levels)
                {
                    // Пропускаем пустые строки (они приходят как levels= из frontend)
                    if (string.IsNullOrWhiteSpace(level))
                        continue;

                    var parsed = ParseLogLevel(level);
                    if (parsed.HasValue)
                    {
                        parsedLevels.Add(parsed.Value);
                    }
                }
            }

            // Если parsedLevels пустой, это режим NONE (возвращаем 0 записей)
            // НЕ преобразуем обратно в null, чтобы отличать от случая "параметр не указан"
        }

        var levelsString = parsedLevels != null && parsedLevels.Count > 0
            ? string.Join(", ", parsedLevels)
            : "null";

        _logger.LogDebug(
            "Getting logs for session {SessionId}: Page={Page}, PageSize={PageSize}, MinLevel={MinLevel}, MaxLevel={MaxLevel}, Levels=[{Levels}], Search={Search}",
            sessionId, page, pageSize, minLevel, maxLevel, levelsString, search);

        // Get logs from service
        var (entries, totalCount) = await _logService.GetLogsAsync(
            sessionId,
            searchText: search,
            minLevel: parsedMinLevel,
            maxLevel: parsedMaxLevel,
            levels: parsedLevels,
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
