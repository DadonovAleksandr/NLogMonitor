namespace nLogMonitor.Application.Configuration;

/// <summary>
/// Настройки для хранения недавно открытых лог-файлов.
/// </summary>
public class RecentLogsSettings
{
    /// <summary>
    /// Секция конфигурации в appsettings.json.
    /// </summary>
    public const string SectionName = "RecentLogsSettings";

    /// <summary>
    /// Максимальное количество хранимых записей.
    /// Default: 20.
    /// </summary>
    public int MaxEntries { get; set; } = 20;

    /// <summary>
    /// Пользовательский путь к файлу хранения.
    /// Если не задан, используется {LocalApplicationData}/nLogMonitor/recent.json.
    /// </summary>
    public string? CustomStoragePath { get; set; }
}
