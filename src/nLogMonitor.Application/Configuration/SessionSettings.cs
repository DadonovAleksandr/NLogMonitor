namespace nLogMonitor.Application.Configuration;

/// <summary>
/// Настройки управления сессиями логов.
/// </summary>
public class SessionSettings
{
    /// <summary>
    /// Секция конфигурации в appsettings.json.
    /// </summary>
    public const string SectionName = "SessionSettings";

    /// <summary>
    /// Fallback TTL в минутах для потерянных сессий (при потере SignalR соединения).
    /// Default: 5 минут.
    /// </summary>
    public int FallbackTtlMinutes { get; set; } = 5;

    /// <summary>
    /// Интервал очистки просроченных сессий в минутах.
    /// Default: 1 минута.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 1;
}
