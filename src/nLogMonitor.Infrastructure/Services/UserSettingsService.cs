using System.Text.Json;
using System.Text.Json.Serialization;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;

namespace nLogMonitor.Infrastructure.Services;

/// <summary>
/// Сервис для работы с пользовательскими настройками приложения.
/// Использует файловое хранилище в стандартных директориях ОС.
/// </summary>
public sealed class UserSettingsService : IUserSettingsService
{
    private static readonly SemaphoreSlim _fileLock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _settingsFilePath;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserSettingsService"/>
    /// </summary>
    public UserSettingsService()
    {
        _settingsFilePath = GetSettingsFilePath();
    }

    /// <summary>
    /// Получить настройки пользователя
    /// </summary>
    public async Task<UserSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new UserSettingsDto
                {
                    OpenedTabs = new List<TabSettingDto>(),
                    LastActiveTabIndex = 0
                };
            }

            await using var fileStream = new FileStream(
                _settingsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            var settings = await JsonSerializer.DeserializeAsync<UserSettingsDto>(
                fileStream,
                _jsonOptions,
                cancellationToken);

            return settings ?? new UserSettingsDto
            {
                OpenedTabs = new List<TabSettingDto>(),
                LastActiveTabIndex = 0
            };
        }
        catch (JsonException)
        {
            // Если файл поврежден, возвращаем настройки по умолчанию
            return new UserSettingsDto
            {
                OpenedTabs = new List<TabSettingDto>(),
                LastActiveTabIndex = 0
            };
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Сохранить настройки пользователя
    /// </summary>
    public async Task SaveSettingsAsync(UserSettingsDto settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Сначала записываем во временный файл
            var tempFilePath = _settingsFilePath + ".tmp";

            await using (var fileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    fileStream,
                    settings,
                    _jsonOptions,
                    cancellationToken);

                await fileStream.FlushAsync(cancellationToken);
            }

            // Атомарно заменяем основной файл
            File.Move(tempFilePath, _settingsFilePath, overwrite: true);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Получить путь к файлу настроек в зависимости от ОС
    /// </summary>
    private static string GetSettingsFilePath()
    {
        string configDirectory;

        if (OperatingSystem.IsWindows())
        {
            // Windows: %LOCALAPPDATA%\nLogMonitor\config.json
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            configDirectory = Path.Combine(localAppData, "nLogMonitor");
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // Linux/macOS: ~/.config/nlogmonitor/config.json
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            configDirectory = Path.Combine(home, ".config", "nlogmonitor");
        }
        else
        {
            // Fallback для других ОС
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            configDirectory = Path.Combine(appData, "nLogMonitor");
        }

        return Path.Combine(configDirectory, "config.json");
    }
}
