using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;

namespace nLogMonitor.Desktop.Controllers;

/// <summary>
/// Controller for server metrics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    /// <summary>
    /// Server start time for uptime calculation.
    /// </summary>
    private static readonly DateTime StartTime = DateTime.UtcNow;

    /// <summary>
    /// Average estimated size of a log entry in bytes.
    /// </summary>
    private const int AverageLogEntrySizeBytes = 500;

    private readonly ISessionStorage _sessionStorage;
    private readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of the MetricsController.
    /// </summary>
    /// <param name="sessionStorage">Session storage for accessing session data.</param>
    /// <param name="logger">Logger instance.</param>
    public MetricsController(
        ISessionStorage sessionStorage,
        ILogger<MetricsController> logger)
    {
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves server metrics.
    /// </summary>
    /// <returns>Server metrics including active sessions, log counts, memory usage, and uptime.</returns>
    /// <response code="200">Returns the server metrics.</response>
    [HttpGet]
    [ProducesResponseType(typeof(MetricsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MetricsDto>> GetMetrics()
    {
        var now = DateTime.UtcNow;

        var sessionsCount = await _sessionStorage.GetActiveSessionCountAsync();
        var logsCount = await _sessionStorage.GetTotalLogsCountAsync();
        var connectionsCount = await _sessionStorage.GetActiveConnectionsCountAsync();
        var uptimeSeconds = (now - StartTime).TotalSeconds;
        var memoryBytes = logsCount * AverageLogEntrySizeBytes;

        var metrics = new MetricsDto
        {
            SessionsActiveCount = sessionsCount,
            LogsTotalCount = logsCount,
            SessionsMemoryBytes = memoryBytes,
            ServerUptimeSeconds = uptimeSeconds,
            SignalrConnectionsCount = connectionsCount,
            Timestamp = now
        };

        _logger.LogDebug(
            "Metrics retrieved: Sessions={SessionsCount}, Logs={LogsCount}, Connections={ConnectionsCount}, Uptime={Uptime}s",
            sessionsCount,
            logsCount,
            connectionsCount,
            uptimeSeconds);

        return Ok(metrics);
    }
}
