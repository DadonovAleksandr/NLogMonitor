namespace nLogMonitor.Application.DTOs;

/// <summary>
/// DTO для настроек вкладки
/// </summary>
public sealed class TabSettingDto
{
    /// <summary>
    /// Тип вкладки (file или directory)
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Полный путь к файлу или директории
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Имя для отображения во вкладке
    /// </summary>
    public required string DisplayName { get; init; }
}
