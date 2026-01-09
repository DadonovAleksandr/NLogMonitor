using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Api.Controllers;

/// <summary>
/// Web-mode endpoint for uploading log files via multipart/form-data.
/// </summary>
[ApiController]
[Route("api/upload")]
// Note: RequestSizeLimit = FileSettings.MaxFileSizeMB (100MB) + 10MB overhead for multipart form data
// Keep this in sync with FileSettings.MaxFileSizeMB in appsettings.json
[RequestSizeLimit(110_000_000)] // 110 MB = 100 MB file + 10 MB multipart overhead
public class UploadController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly IRecentLogsRepository _recentLogsRepository;
    private readonly ISessionStorage _sessionStorage;
    private readonly ILogger<UploadController> _logger;
    private readonly FileSettings _fileSettings;

    /// <summary>
    /// Initializes a new instance of the UploadController.
    /// </summary>
    /// <param name="logService">Service for log file operations.</param>
    /// <param name="recentLogsRepository">Repository for recent log entries.</param>
    /// <param name="sessionStorage">Storage for log sessions.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="fileSettings">File handling configuration.</param>
    public UploadController(
        ILogService logService,
        IRecentLogsRepository recentLogsRepository,
        ISessionStorage sessionStorage,
        ILogger<UploadController> logger,
        IOptions<FileSettings> fileSettings)
    {
        _logService = logService;
        _recentLogsRepository = recentLogsRepository;
        _sessionStorage = sessionStorage;
        _logger = logger;
        _fileSettings = fileSettings.Value;
    }

    /// <summary>
    /// Uploads a log file for analysis.
    /// </summary>
    /// <param name="file">The log file to upload (max 100 MB, .log or .txt extension).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session information with file statistics.</returns>
    /// <response code="200">File uploaded and parsed successfully.</response>
    /// <response code="400">Invalid file (no file, wrong extension, or too large).</response>
    [HttpPost]
    [ProducesResponseType(typeof(OpenFileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<OpenFileResultDto>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        // Validate file presence
        if (file is null || file.Length == 0)
        {
            _logger.LogWarning("Upload attempt with no file");
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = "No file uploaded.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = _fileSettings.AllowedExtensions
            .Select(e => e.ToLowerInvariant())
            .ToArray();

        if (!allowedExtensions.Contains(extension))
        {
            _logger.LogWarning(
                "Upload attempt with invalid extension: {Extension}, File: {FileName}",
                extension, file.FileName);

            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = $"File extension not allowed. Allowed: {string.Join(", ", _fileSettings.AllowedExtensions)}",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Validate file size
        if (file.Length > _fileSettings.MaxFileSizeBytes)
        {
            _logger.LogWarning(
                "Upload attempt with file too large: {Size} bytes, Max: {MaxSize} bytes, File: {FileName}",
                file.Length, _fileSettings.MaxFileSizeBytes, file.FileName);

            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = $"File size exceeds maximum allowed size ({_fileSettings.MaxFileSizeMB} MB).",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Generate session ID and create temp directory
        var sessionId = Guid.NewGuid();
        var tempDirectory = Path.Combine(_fileSettings.TempDirectory, sessionId.ToString());

        try
        {
            // Ensure temp directory exists
            Directory.CreateDirectory(tempDirectory);

            // Sanitize filename to prevent path traversal
            var safeFileName = Path.GetFileName(file.FileName);

            // Validate that the resulting path is within the temp directory
            var tempFilePath = Path.Combine(tempDirectory, safeFileName);
            var normalizedPath = Path.GetFullPath(tempFilePath);
            var normalizedTempDir = Path.GetFullPath(tempDirectory);

            if (!normalizedPath.StartsWith(normalizedTempDir, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {FileName}", file.FileName);
                return BadRequest(new ApiErrorResponse
                {
                    Error = "BadRequest",
                    Message = "Invalid file name.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            _logger.LogInformation(
                "Saving uploaded file to temp: {TempPath}, Size: {Size} bytes",
                tempFilePath, file.Length);

            await using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Parse the file using LogService (pass sessionId to sync with temp directory)
            var createdSessionId = await _logService.OpenFileAsync(tempFilePath, cancellationToken, sessionId);
            var session = await _logService.GetSessionAsync(createdSessionId);

            if (session is null)
            {
                _logger.LogError("Session {SessionId} not found after file upload", createdSessionId);
                throw new InvalidOperationException($"Session {createdSessionId} was created but could not be retrieved.");
            }

            // Add to recent files (using temp path as this is Web mode)
            await _recentLogsRepository.AddAsync(new RecentLogEntry
            {
                Path = tempFilePath,
                IsDirectory = false,
                OpenedAt = DateTime.UtcNow
            });

            // Register cleanup callback to delete temp directory when session is deleted
            await _sessionStorage.RegisterCleanupCallbackAsync(
                createdSessionId,
                () => DeleteTempDirectoryAsync(tempDirectory));

            var result = MapToOpenFileResult(session);

            _logger.LogInformation(
                "File uploaded successfully. SessionId: {SessionId}, FileName: {FileName}, Entries: {TotalEntries}",
                result.SessionId,
                result.FileName,
                result.TotalEntries);

            return Ok(result);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error processing uploaded file: {FileName}", file.FileName);

            // Cleanup temp directory on error
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup temp directory: {TempDirectory}", tempDirectory);
            }

            throw;
        }
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

    /// <summary>
    /// Удаляет временную директорию с загруженными файлами.
    /// </summary>
    /// <param name="tempDirectory">Путь к временной директории.</param>
    private Task DeleteTempDirectoryAsync(string tempDirectory)
    {
        try
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
                _logger.LogInformation("Temp directory deleted: {TempDirectory}", tempDirectory);
            }
            else
            {
                _logger.LogDebug("Temp directory not found for deletion: {TempDirectory}", tempDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp directory: {TempDirectory}", tempDirectory);
        }

        return Task.CompletedTask;
    }
}
