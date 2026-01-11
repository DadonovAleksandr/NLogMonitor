namespace nLogMonitor.Application.DTOs;

/// <summary>
/// DTO для пользовательских настроек приложения
/// </summary>
public sealed class UserSettingsDto
{
    /// <summary>
    /// Список открытых вкладок
    /// </summary>
    public List<TabSettingDto> OpenedTabs { get; init; } = new();

    /// <summary>
    /// Индекс последней активной вкладки
    /// </summary>
    public int LastActiveTabIndex { get; init; }
}
