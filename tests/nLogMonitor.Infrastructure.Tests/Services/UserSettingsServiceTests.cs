using NUnit.Framework;
using FluentAssertions;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Infrastructure.Services;

namespace nLogMonitor.Infrastructure.Tests.Services;

[TestFixture]
public class UserSettingsServiceTests
{
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        // Создаём временную директорию для тестов
        _testDirectory = Path.Combine(Path.GetTempPath(), "nLogMonitor_SettingsTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        // Очищаем тестовую директорию
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Игнорируем ошибки удаления
            }
        }
    }

    // === Получение настроек ===

    [Test]
    public async Task GetSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Arrange
        var service = new UserSettingsService();

        // Act
        var settings = await service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.OpenedTabs.Should().BeEmpty();
        settings.LastActiveTabIndex.Should().Be(0);
    }

    [Test]
    public async Task GetSettingsAsync_WhenFileIsCorrupted_ReturnsDefaultSettings()
    {
        // Arrange
        var service = new UserSettingsService();
        var settingsPath = GetSettingsFilePath();
        var directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Записываем некорректный JSON
        await File.WriteAllTextAsync(settingsPath, "{ invalid json");

        // Act
        var settings = await service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.OpenedTabs.Should().BeEmpty();
        settings.LastActiveTabIndex.Should().Be(0);

        // Cleanup
        File.Delete(settingsPath);
    }

    [Test]
    public async Task GetSettingsAsync_AfterSave_ReturnsSavedSettings()
    {
        // Arrange
        var service = new UserSettingsService();
        var originalSettings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\logs\\app.log", DisplayName = "app.log" },
                new() { Type = "directory", Path = "C:\\logs", DisplayName = "logs" }
            },
            LastActiveTabIndex = 1
        };

        // Act
        await service.SaveSettingsAsync(originalSettings);
        var retrievedSettings = await service.GetSettingsAsync();

        // Assert
        retrievedSettings.Should().NotBeNull();
        retrievedSettings.OpenedTabs.Should().HaveCount(2);
        retrievedSettings.LastActiveTabIndex.Should().Be(1);

        retrievedSettings.OpenedTabs[0].Type.Should().Be("file");
        retrievedSettings.OpenedTabs[0].Path.Should().Be("C:\\logs\\app.log");
        retrievedSettings.OpenedTabs[0].DisplayName.Should().Be("app.log");

        retrievedSettings.OpenedTabs[1].Type.Should().Be("directory");
        retrievedSettings.OpenedTabs[1].Path.Should().Be("C:\\logs");
        retrievedSettings.OpenedTabs[1].DisplayName.Should().Be("logs");

        // Cleanup
        var settingsPath = GetSettingsFilePath();
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    // === Сохранение настроек ===

    [Test]
    public async Task SaveSettingsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new UserSettingsService();

        // Act & Assert
        await Assert.ThatAsync(
            async () => await service.SaveSettingsAsync(null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public async Task SaveSettingsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var service = new UserSettingsService();
        var settingsPath = GetSettingsFilePath();
        var directory = Path.GetDirectoryName(settingsPath);

        // Убедимся, что директория не существует
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>(),
            LastActiveTabIndex = 0
        };

        // Act
        await service.SaveSettingsAsync(settings);

        // Assert
        Directory.Exists(directory).Should().BeTrue();
        File.Exists(settingsPath).Should().BeTrue();

        // Cleanup
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    [Test]
    public async Task SaveSettingsAsync_OverwritesExistingFile()
    {
        // Arrange
        var service = new UserSettingsService();

        var initialSettings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\initial.log", DisplayName = "initial" }
            },
            LastActiveTabIndex = 0
        };

        var updatedSettings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\updated.log", DisplayName = "updated" },
                new() { Type = "directory", Path = "C:\\logs", DisplayName = "logs" }
            },
            LastActiveTabIndex = 1
        };

        // Act
        await service.SaveSettingsAsync(initialSettings);
        await service.SaveSettingsAsync(updatedSettings);
        var retrievedSettings = await service.GetSettingsAsync();

        // Assert
        retrievedSettings.OpenedTabs.Should().HaveCount(2);
        retrievedSettings.OpenedTabs[0].Path.Should().Be("C:\\updated.log");
        retrievedSettings.LastActiveTabIndex.Should().Be(1);

        // Cleanup
        var settingsPath = GetSettingsFilePath();
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    [Test]
    public async Task SaveSettingsAsync_WithEmptyTabs_SavesCorrectly()
    {
        // Arrange
        var service = new UserSettingsService();
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>(),
            LastActiveTabIndex = 0
        };

        // Act
        await service.SaveSettingsAsync(settings);
        var retrievedSettings = await service.GetSettingsAsync();

        // Assert
        retrievedSettings.OpenedTabs.Should().BeEmpty();
        retrievedSettings.LastActiveTabIndex.Should().Be(0);

        // Cleanup
        var settingsPath = GetSettingsFilePath();
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    // === Concurrent Access ===

    [Test]
    public async Task ConcurrentAccess_MultipleSaves_AllSucceed()
    {
        // Arrange
        var service = new UserSettingsService();
        const int concurrentOperations = 10;

        // Act
        var tasks = Enumerable.Range(0, concurrentOperations)
            .Select(i => Task.Run(async () =>
            {
                var settings = new UserSettingsDto
                {
                    OpenedTabs = new List<TabSettingDto>
                    {
                        new() { Type = "file", Path = $"C:\\log{i}.log", DisplayName = $"log{i}" }
                    },
                    LastActiveTabIndex = i
                };
                await service.SaveSettingsAsync(settings);
            }))
            .ToArray();

        // Assert - все задачи должны завершиться без исключений
        await Assert.ThatAsync(async () => await Task.WhenAll(tasks), Throws.Nothing);

        // Проверяем, что файл существует и содержит валидные данные
        var finalSettings = await service.GetSettingsAsync();
        finalSettings.Should().NotBeNull();
        finalSettings.OpenedTabs.Should().HaveCount(1); // Последнее сохранение

        // Cleanup
        var settingsPath = GetSettingsFilePath();
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    [Test]
    public async Task ConcurrentAccess_SaveAndRead_NoExceptions()
    {
        // Arrange
        var service = new UserSettingsService();
        const int operations = 20;

        var initialSettings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\test.log", DisplayName = "test" }
            },
            LastActiveTabIndex = 0
        };

        await service.SaveSettingsAsync(initialSettings);

        // Act - чередуем операции чтения и записи
        var tasks = Enumerable.Range(0, operations)
            .Select(i => Task.Run(async () =>
            {
                if (i % 2 == 0)
                {
                    // Чтение
                    var settings = await service.GetSettingsAsync();
                    settings.Should().NotBeNull();
                }
                else
                {
                    // Запись
                    var settings = new UserSettingsDto
                    {
                        OpenedTabs = new List<TabSettingDto>
                        {
                            new() { Type = "file", Path = $"C:\\log{i}.log", DisplayName = $"log{i}" }
                        },
                        LastActiveTabIndex = 0
                    };
                    await service.SaveSettingsAsync(settings);
                }
            }))
            .ToArray();

        // Assert - все задачи должны завершиться без исключений
        await Assert.ThatAsync(async () => await Task.WhenAll(tasks), Throws.Nothing);

        // Cleanup
        var settingsPath = GetSettingsFilePath();
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    // === Helper Methods ===

    private static string GetSettingsFilePath()
    {
        string configDirectory;

        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            configDirectory = Path.Combine(localAppData, "nLogMonitor");
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            configDirectory = Path.Combine(home, ".config", "nlogmonitor");
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            configDirectory = Path.Combine(appData, "nLogMonitor");
        }

        return Path.Combine(configDirectory, "config.json");
    }
}
