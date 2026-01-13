using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class RecentControllerIntegrationTests : WebApplicationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public async Task GetRecent_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange - clear any existing entries first
        await Client.DeleteAsync("/api/recent");

        // Act
        var response = await Client.GetAsync("/api/recent");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        var recentFiles = JsonSerializer.Deserialize<List<RecentLogEntry>>(content, JsonOptions);
        Assert.That(recentFiles, Is.Not.Null);
        Assert.That(recentFiles, Is.Empty);
    }

    [Test]
    public async Task GetRecent_AfterUpload_ContainsUploadedFile()
    {
        // Arrange - clear existing entries
        await Client.DeleteAsync("/api/recent");

        // Upload a file
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|MyApp|1234|1";
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "test-recent.log");

        var uploadResponse = await Client.PostAsync("/api/upload", uploadContent);
        Assert.That(uploadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act
        var response = await Client.GetAsync("/api/recent");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        var recentFiles = JsonSerializer.Deserialize<List<RecentLogEntry>>(content, JsonOptions);

        Assert.That(recentFiles, Is.Not.Null);
        Assert.That(recentFiles, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(recentFiles!.Any(f => f.DisplayName == "test-recent.log"), Is.True);
    }

    [Test]
    public async Task GetRecent_ResponseContainsRequiredFields()
    {
        // Arrange - clear existing entries and upload a file
        await Client.DeleteAsync("/api/recent");

        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|MyApp|1234|1";
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "fields-test.log");

        var uploadResponse = await Client.PostAsync("/api/upload", uploadContent);
        Assert.That(uploadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act
        var response = await Client.GetAsync("/api/recent");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();

        // Check that response contains all required fields
        Assert.That(content, Does.Contain("path"));
        Assert.That(content, Does.Contain("isDirectory"));
        Assert.That(content, Does.Contain("openedAt"));
        Assert.That(content, Does.Contain("displayName"));

        // Verify deserialization works correctly
        var recentFiles = JsonSerializer.Deserialize<List<RecentLogEntry>>(content, JsonOptions);
        Assert.That(recentFiles, Is.Not.Null);
        Assert.That(recentFiles, Has.Count.GreaterThanOrEqualTo(1));

        var entry = recentFiles!.First(f => f.DisplayName == "fields-test.log");
        Assert.That(entry.Path, Is.Not.Null.And.Not.Empty);
        Assert.That(entry.IsDirectory, Is.False);
        Assert.That(entry.OpenedAt, Is.GreaterThan(DateTime.MinValue));
        Assert.That(entry.DisplayName, Is.EqualTo("fields-test.log"));
    }

    [Test]
    public async Task ClearRecent_RemovesAllEntries()
    {
        // Arrange - upload a file first
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|MyApp|1234|1";
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "to-clear.log");

        var uploadResponse = await Client.PostAsync("/api/upload", uploadContent);
        Assert.That(uploadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify file is in recent list
        var checkResponse = await Client.GetAsync("/api/recent");
        var checkContent = await checkResponse.Content.ReadAsStringAsync();
        var beforeClear = JsonSerializer.Deserialize<List<RecentLogEntry>>(checkContent, JsonOptions);
        Assert.That(beforeClear, Has.Count.GreaterThanOrEqualTo(1));

        // Act
        var clearResponse = await Client.DeleteAsync("/api/recent");

        // Assert
        Assert.That(clearResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify list is empty
        var response = await Client.GetAsync("/api/recent");
        var content = await response.Content.ReadAsStringAsync();
        var afterClear = JsonSerializer.Deserialize<List<RecentLogEntry>>(content, JsonOptions);
        Assert.That(afterClear, Is.Not.Null);
        Assert.That(afterClear, Is.Empty);
    }

    [Test]
    public async Task ClearRecent_WhenEmpty_Returns204()
    {
        // Arrange - ensure list is empty
        await Client.DeleteAsync("/api/recent");

        // Act - clear again when already empty
        var response = await Client.DeleteAsync("/api/recent");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    /// <summary>
    /// DTO for deserializing recent log entries from API response.
    /// </summary>
    private class RecentLogEntry
    {
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public DateTime OpenedAt { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
