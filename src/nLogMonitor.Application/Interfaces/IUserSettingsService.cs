using nLogMonitor.Application.DTOs;

namespace nLogMonitor.Application.Interfaces;

/// <summary>
/// Сервис для работы с пользовательскими настройками приложения
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Получить настройки пользователя
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Настройки пользователя или настройки по умолчанию, если файл не существует</returns>
    Task<UserSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить настройки пользователя
    /// </summary>
    /// <param name="settings">Настройки для сохранения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SaveSettingsAsync(UserSettingsDto settings, CancellationToken cancellationToken = default);
}
