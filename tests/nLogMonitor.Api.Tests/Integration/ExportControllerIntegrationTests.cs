using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class ExportControllerIntegrationTests : WebApplicationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public async Task Export_WithNonExistentSession_Returns404WithApiError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/export/{sessionId}?format=json");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await AssertApiErrorAsync(response, "NotFound", sessionId.ToString());
    }

    [Test]
    public async Task Export_WithUnsupportedFormat_Returns400WithApiError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/export/{sessionId}?format=xml");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "Unsupported");
    }

    [Test]
    public async Task Export_WithInvalidSessionIdFormat_Returns404()
    {
        // Act
        var response = await Client.GetAsync("/api/export/invalid-guid?format=json");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Export_JsonFormat_AfterUpload_ReturnsJsonFile()
    {
        // Arrange - first upload a file to create a session
        var sessionId = await UploadLogFileAsync("2024-01-15 10:30:45.1234|INFO|Test message|Logger|1|1");

        // Act - export with JSON format
        var response = await Client.GetAsync($"/api/export/{sessionId}?format=json");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(response.Content.Headers.ContentDisposition?.FileName, Does.StartWith("logs_"));
        Assert.That(response.Content.Headers.ContentDisposition?.FileName, Does.EndWith(".json"));
    }

    [Test]
    public async Task Export_CsvFormat_AfterUpload_ReturnsCsvFile()
    {
        // Arrange - first upload a file to create a session
        var sessionId = await UploadLogFileAsync("2024-01-15 10:30:45.1234|WARN|Warning message|Logger|1|1");

        // Act - export with CSV format
        var response = await Client.GetAsync($"/api/export/{sessionId}?format=csv");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));
        Assert.That(response.Content.Headers.ContentDisposition?.FileName, Does.StartWith("logs_"));
        Assert.That(response.Content.Headers.ContentDisposition?.FileName, Does.EndWith(".csv"));
    }

    [Test]
    public async Task Export_DefaultFormat_ReturnsJson()
    {
        // Arrange - first upload a file to create a session
        var sessionId = await UploadLogFileAsync("2024-01-15 10:30:45.1234|INFO|Test|Logger|1|1");

        // Act - export without specifying format
        var response = await Client.GetAsync($"/api/export/{sessionId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    #region Helper Methods

    private async Task<Guid> UploadLogFileAsync(string logContent)
    {
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "test.log");

        var uploadResponse = await Client.PostAsync("/api/upload", uploadContent);
        Assert.That(uploadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>(JsonOptions);
        Assert.That(result, Is.Not.Null, "Could not deserialize upload response");
        Assert.That(result!.SessionId, Is.Not.EqualTo(Guid.Empty), "SessionId should not be empty");

        return result.SessionId;
    }

    #endregion

    #region Response DTOs

    private class UploadResponse
    {
        public Guid SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int TotalEntries { get; set; }
        public Dictionary<string, int> LevelCounts { get; set; } = new();
    }

    #endregion
}
