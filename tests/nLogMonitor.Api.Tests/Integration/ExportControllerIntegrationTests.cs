using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class ExportControllerIntegrationTests : WebApplicationTestBase
{
    [Test]
    public async Task Export_WithNonExistentSession_Returns404()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/export/{sessionId}?format=json");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Export_WithUnsupportedFormat_Returns400()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/export/{sessionId}?format=xml");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("Unsupported"));
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
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|Logger|1|1";

        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "test.log");

        var uploadResponse = await Client.PostAsync("/api/upload", uploadContent);
        Assert.That(uploadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
        var sessionIdMatch = System.Text.RegularExpressions.Regex.Match(
            uploadResponseContent, @"""sessionId""\s*:\s*""([^""]+)""");
        Assert.That(sessionIdMatch.Success, Is.True, "Could not extract sessionId from upload response");

        var sessionId = sessionIdMatch.Groups[1].Value;

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
        var logContent = "2024-01-15 10:30:45.1234|WARN|Warning message|Logger|1|1";

        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "test.log");

        var uploadResponse = await Client.PostAsync("/api/upload", uploadContent);
        Assert.That(uploadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
        var sessionIdMatch = System.Text.RegularExpressions.Regex.Match(
            uploadResponseContent, @"""sessionId""\s*:\s*""([^""]+)""");
        Assert.That(sessionIdMatch.Success, Is.True);

        var sessionId = sessionIdMatch.Groups[1].Value;

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
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test|Logger|1|1";

        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "test.log");

        var uploadResponse = await Client.PostAsync("/api/upload", uploadContent);
        Assert.That(uploadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
        var sessionIdMatch = System.Text.RegularExpressions.Regex.Match(
            uploadResponseContent, @"""sessionId""\s*:\s*""([^""]+)""");
        Assert.That(sessionIdMatch.Success, Is.True);

        var sessionId = sessionIdMatch.Groups[1].Value;

        // Act - export without specifying format
        var response = await Client.GetAsync($"/api/export/{sessionId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }
}
