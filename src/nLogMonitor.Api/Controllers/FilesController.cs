using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Api.Filters;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Api.Controllers;

/// <summary>
/// Desktop-only endpoints for opening local log files and directories.
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly IRecentLogsRepository _recentLogsRepository;
    private readonly ILogger<FilesController> _logger;

    /// <summary>
    /// Initializes a new instance of the FilesController.
    /// </summary>
    /// <param name="logService">Service for log file operations.</param>
    /// <param name="recentLogsRepository">Repository for recent log entries.</param>
    /// <param name="logger">Logger instance.</param>
    public FilesController(
        ILogService logService,
        IRecentLogsRepository recentLogsRepository,
        ILogger<FilesController> logger)
    {
        _logService = logService;
        _recentLogsRepository = recentLogsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Opens a log file by absolute path.
    /// </summary>
    /// <param name="request">Request containing the file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session information with file statistics.</returns>
    /// <response code="200">File opened successfully.</response>
    /// <response code="400">Invalid file path.</response>
    /// <response code="404">File not found or endpoint not available in Web mode.</response>
    [HttpPost("open")]
    [DesktopOnly]
    [ProducesResponseType(typeof(OpenFileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpenFileResultDto>> OpenFile(
        [FromBody] OpenFileRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = "File path is required.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Opening file: {FilePath}", request.FilePath);

        var sessionId = await _logService.OpenFileAsync(request.FilePath, cancellationToken);
        var session = await _logService.GetSessionAsync(sessionId);

        if (session is null)
        {
            _logger.LogError("Session {SessionId} not found after file open", sessionId);
            throw new InvalidOperationException($"Session {sessionId} was created but could not be retrieved.");
        }

        // Add to recent files
        await _recentLogsRepository.AddAsync(new RecentLogEntry
        {
            Path = session.FilePath,
            IsDirectory = false,
            OpenedAt = DateTime.UtcNow
        });

        var result = MapToOpenFileResult(session);

        _logger.LogInformation(
            "File opened successfully. SessionId: {SessionId}, Entries: {TotalEntries}",
            result.SessionId,
            result.TotalEntries);

        return Ok(result);
    }

    /// <summary>
    /// Opens a directory and automatically selects the most recent .log file.
    /// </summary>
    /// <param name="request">Request containing the directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session information with file statistics.</returns>
    /// <response code="200">Directory opened successfully.</response>
    /// <response code="400">Invalid directory path.</response>
    /// <response code="404">Directory not found, no log files found, or endpoint not available in Web mode.</response>
    [HttpPost("open-directory")]
    [DesktopOnly]
    [ProducesResponseType(typeof(OpenFileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpenFileResultDto>> OpenDirectory(
        [FromBody] OpenDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DirectoryPath))
        {
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = "Directory path is required.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Opening directory: {DirectoryPath}", request.DirectoryPath);

        var sessionId = await _logService.OpenDirectoryAsync(request.DirectoryPath, cancellationToken);
        var session = await _logService.GetSessionAsync(sessionId);

        if (session is null)
        {
            _logger.LogError("Session {SessionId} not found after directory open", sessionId);
            throw new InvalidOperationException($"Session {sessionId} was created but could not be retrieved.");
        }

        // Add directory to recent files
        await _recentLogsRepository.AddAsync(new RecentLogEntry
        {
            Path = request.DirectoryPath,
            IsDirectory = true,
            OpenedAt = DateTime.UtcNow
        });

        var result = MapToOpenFileResult(session);

        _logger.LogInformation(
            "Directory opened successfully. SessionId: {SessionId}, File: {FileName}, Entries: {TotalEntries}",
            result.SessionId,
            result.FileName,
            result.TotalEntries);

        return Ok(result);
    }

    /// <summary>
    /// Stops file monitoring for a session.
    /// </summary>
    /// <remarks>
    /// This endpoint is planned for Phase 6 implementation.
    /// Currently returns 501 Not Implemented.
    /// </remarks>
    /// <param name="sessionId">Session identifier.</param>
    /// <returns>501 Not Implemented until Phase 6.</returns>
    /// <response code="501">File watching functionality not yet implemented.</response>
    /// <response code="404">Session not found.</response>
    [HttpPost("{sessionId:guid}/stop-watching")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopWatching(Guid sessionId)
    {
        _logger.LogInformation("Stop watching requested for session: {SessionId} (not implemented)", sessionId);

        var session = await _logService.GetSessionAsync(sessionId);
        if (session is null)
        {
            return NotFound(new ApiErrorResponse
            {
                Error = "NotFound",
                Message = $"Session {sessionId} not found.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // File watching planned for Phase 6
        return StatusCode(StatusCodes.Status501NotImplemented, new ApiErrorResponse
        {
            Error = "NotImplemented",
            Message = "File watching functionality is planned for Phase 6.",
            TraceId = HttpContext.TraceIdentifier
        });
    }

    private static OpenFileResultDto MapToOpenFileResult(LogSession session)
    {
        return new OpenFileResultDto
        {
            SessionId = session.Id,
            FileName = session.FileName,
            FilePath = session.FilePath,
            TotalEntries = session.Entries.Count,
            LevelCounts = session.LevelCounts.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value)
        };
    }
}
