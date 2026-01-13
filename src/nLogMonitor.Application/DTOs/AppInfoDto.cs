using System.Text.Json.Serialization;

namespace nLogMonitor.Application.DTOs;

/// <summary>
/// DTO for application information.
/// </summary>
public class AppInfoDto
{
    /// <summary>
    /// Application version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
