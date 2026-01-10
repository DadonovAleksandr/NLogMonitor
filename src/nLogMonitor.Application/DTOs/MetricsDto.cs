using System.Text.Json.Serialization;

namespace nLogMonitor.Application.DTOs;

/// <summary>
/// DTO for server metrics.
/// </summary>
public class MetricsDto
{
    /// <summary>
    /// Number of active sessions.
    /// </summary>
    [JsonPropertyName("sessions_active_count")]
    public int SessionsActiveCount { get; set; }

    /// <summary>
    /// Total number of log entries across all sessions.
    /// </summary>
    [JsonPropertyName("logs_total_count")]
    public long LogsTotalCount { get; set; }

    /// <summary>
    /// Estimated memory usage by sessions in bytes.
    /// </summary>
    [JsonPropertyName("sessions_memory_bytes")]
    public long SessionsMemoryBytes { get; set; }

    /// <summary>
    /// Server uptime in seconds.
    /// </summary>
    [JsonPropertyName("server_uptime_seconds")]
    public double ServerUptimeSeconds { get; set; }

    /// <summary>
    /// Number of active SignalR connections.
    /// </summary>
    [JsonPropertyName("signalr_connections_count")]
    public int SignalrConnectionsCount { get; set; }

    /// <summary>
    /// Timestamp when metrics were collected.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
