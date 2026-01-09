using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;

namespace nLogMonitor.Api.Controllers;

/// <summary>
/// Controller for managing recently opened log files.
/// </summary>
[ApiController]
[Route("api/recent")]
public class RecentController : ControllerBase
{
    private readonly IRecentLogsRepository _recentLogsRepository;
    private readonly ILogger<RecentController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentController"/> class.
    /// </summary>
    /// <param name="recentLogsRepository">Repository for recent logs.</param>
    /// <param name="logger">Logger instance.</param>
    public RecentController(
        IRecentLogsRepository recentLogsRepository,
        ILogger<RecentController> logger)
    {
        _recentLogsRepository = recentLogsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of recently opened log files.
    /// </summary>
    /// <returns>List of recently opened files sorted by OpenedAt descending.</returns>
    /// <response code="200">Returns the list of recent files.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RecentLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RecentLogDto>>> GetRecentFiles()
    {
        _logger.LogDebug("Getting recent files list");

        var entries = await _recentLogsRepository.GetAllAsync();

        var result = entries.Select(entry => new RecentLogDto
        {
            Path = entry.Path,
            IsDirectory = entry.IsDirectory,
            OpenedAt = entry.OpenedAt,
            DisplayName = Path.GetFileName(entry.Path)
        });

        _logger.LogInformation("Returned {Count} recent files", result.Count());

        return Ok(result);
    }

    /// <summary>
    /// Clears the list of recently opened files.
    /// </summary>
    /// <response code="204">Recent files list cleared successfully.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearRecentFiles()
    {
        _logger.LogInformation("Clearing recent files list");

        await _recentLogsRepository.ClearAsync();

        _logger.LogDebug("Recent files list cleared");

        return NoContent();
    }
}
