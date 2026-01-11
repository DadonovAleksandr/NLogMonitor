using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class SettingsControllerIntegrationTests : WebApplicationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // === GET /api/settings ===

    [Test]
    public async Task GetSettings_WhenNoSettingsExist_ReturnsDefaultSettings()
    {
        // Arrange - сначала очистим настройки, установив пустые
        var emptySettings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>(),
            LastActiveTabIndex = 0
        };
        await Client.PutAsJsonAsync("/api/settings", emptySettings);

        // Act
        var response = await Client.GetAsync("/api/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>(JsonOptions);
        settings.Should().NotBeNull();
        settings!.OpenedTabs.Should().BeEmpty();
        settings.LastActiveTabIndex.Should().Be(0);
    }

    [Test]
    public async Task GetSettings_AfterSave_ReturnsSavedSettings()
    {
        // Arrange
        var settingsToSave = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\logs\\app.log", DisplayName = "app.log" },
                new() { Type = "directory", Path = "C:\\logs", DisplayName = "logs" }
            },
            LastActiveTabIndex = 1
        };

        // Save settings
        var saveResponse = await Client.PutAsJsonAsync("/api/settings", settingsToSave);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var response = await Client.GetAsync("/api/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>(JsonOptions);
        settings.Should().NotBeNull();
        settings!.OpenedTabs.Should().HaveCount(2);
        settings.LastActiveTabIndex.Should().Be(1);

        settings.OpenedTabs[0].Type.Should().Be("file");
        settings.OpenedTabs[0].Path.Should().Be("C:\\logs\\app.log");
        settings.OpenedTabs[0].DisplayName.Should().Be("app.log");

        settings.OpenedTabs[1].Type.Should().Be("directory");
        settings.OpenedTabs[1].Path.Should().Be("C:\\logs");
        settings.OpenedTabs[1].DisplayName.Should().Be("logs");
    }

    [Test]
    public async Task GetSettings_ResponseContainsRequiredFields()
    {
        // Act
        var response = await Client.GetAsync("/api/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("openedTabs");
        content.Should().Contain("lastActiveTabIndex");
    }

    // === PUT /api/settings ===

    [Test]
    public async Task SaveSettings_WithValidSettings_ReturnsNoContent()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\test.log", DisplayName = "test" }
            },
            LastActiveTabIndex = 0
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task SaveSettings_WithEmptyTabs_ReturnsNoContent()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>(),
            LastActiveTabIndex = 0
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task SaveSettings_WithMultipleTabs_ReturnsNoContent()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\logs\\app1.log", DisplayName = "app1.log" },
                new() { Type = "file", Path = "C:\\logs\\app2.log", DisplayName = "app2.log" },
                new() { Type = "directory", Path = "C:\\logs", DisplayName = "logs" },
                new() { Type = "directory", Path = "C:\\data", DisplayName = "data" }
            },
            LastActiveTabIndex = 2
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task SaveSettings_OverwritesPreviousSettings()
    {
        // Arrange
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
                new() { Type = "file", Path = "C:\\updated.log", DisplayName = "updated" }
            },
            LastActiveTabIndex = 0
        };

        // Act
        await Client.PutAsJsonAsync("/api/settings", initialSettings);
        await Client.PutAsJsonAsync("/api/settings", updatedSettings);

        var getResponse = await Client.GetAsync("/api/settings");
        var settings = await getResponse.Content.ReadFromJsonAsync<UserSettingsDto>(JsonOptions);

        // Assert
        settings.Should().NotBeNull();
        settings!.OpenedTabs.Should().HaveCount(1);
        settings.OpenedTabs[0].Path.Should().Be("C:\\updated.log");
    }

    // === Validation Tests ===

    [Test]
    public async Task SaveSettings_WithNullBody_ReturnsUnsupportedMediaType()
    {
        // Act
        var response = await Client.PutAsync("/api/settings", null);

        // Assert - ASP.NET Core возвращает 415 Unsupported Media Type при null content
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task SaveSettings_WithInvalidTabType_ReturnsBadRequest()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "invalid_type", Path = "C:\\test.log", DisplayName = "test" }
            },
            LastActiveTabIndex = 0
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await AssertApiErrorAsync(response, "BadRequest");
        error.Message.Should().Contain("Type must be 'file' or 'directory'");
    }

    [Test]
    public async Task SaveSettings_WithEmptyTabType_ReturnsBadRequest()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "", Path = "C:\\test.log", DisplayName = "test" }
            },
            LastActiveTabIndex = 0
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await AssertApiErrorAsync(response, "BadRequest");
        error.Message.Should().Contain("Type is required");
    }

    [Test]
    public async Task SaveSettings_WithEmptyPath_ReturnsBadRequest()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "", DisplayName = "test" }
            },
            LastActiveTabIndex = 0
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await AssertApiErrorAsync(response, "BadRequest");
        error.Message.Should().Contain("Path is required");
    }

    [Test]
    public async Task SaveSettings_WithEmptyDisplayName_ReturnsBadRequest()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\test.log", DisplayName = "" }
            },
            LastActiveTabIndex = 0
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await AssertApiErrorAsync(response, "BadRequest");
        error.Message.Should().Contain("DisplayName is required");
    }

    [Test]
    public async Task SaveSettings_WithNegativeActiveTabIndex_ReturnsBadRequest()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\test.log", DisplayName = "test" }
            },
            LastActiveTabIndex = -1
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await AssertApiErrorAsync(response, "BadRequest");
        error.Message.Should().Contain("Invalid LastActiveTabIndex");
    }

    [Test]
    public async Task SaveSettings_WithActiveTabIndexOutOfRange_ReturnsBadRequest()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>
            {
                new() { Type = "file", Path = "C:\\test.log", DisplayName = "test" }
            },
            LastActiveTabIndex = 10 // Вне диапазона (0-0)
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await AssertApiErrorAsync(response, "BadRequest");
        error.Message.Should().Contain("Invalid LastActiveTabIndex");
    }

    [Test]
    public async Task SaveSettings_WithActiveTabIndexZeroAndEmptyTabs_ReturnsNoContent()
    {
        // Arrange - допустимый случай: нет вкладок и индекс = 0
        var settings = new UserSettingsDto
        {
            OpenedTabs = new List<TabSettingDto>(),
            LastActiveTabIndex = 0
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // === DTOs for Testing ===

    private class UserSettingsDto
    {
        public List<TabSettingDto> OpenedTabs { get; init; } = new();
        public int LastActiveTabIndex { get; init; }
    }

    private class TabSettingDto
    {
        public string Type { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
    }
}
