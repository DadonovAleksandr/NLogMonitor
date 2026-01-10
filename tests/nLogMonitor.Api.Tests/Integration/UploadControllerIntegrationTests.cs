using System.Net;
using System.Net.Http.Headers;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class UploadControllerIntegrationTests : WebApplicationTestBase
{
    [Test]
    public async Task Upload_WithNoFile_Returns400()
    {
        // Arrange
        var content = new MultipartFormDataContent();

        // Act
        var response = await Client.PostAsync("/api/upload", content);

        // Assert
        // Note: Empty multipart request triggers ASP.NET Core model binding error,
        // which returns ValidationProblemDetails, not our ApiErrorResponse
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Upload_WithInvalidExtension_Returns400WithApiError()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x01, 0x02 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "test.exe");

        // Act
        var response = await Client.PostAsync("/api/upload", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "extension");
    }

    [Test]
    public async Task Upload_WithPathTraversalAttempt_SanitizesFilename()
    {
        // Arrange - path traversal attempt
        // The controller uses Path.GetFileName which extracts only the filename part,
        // so "../../../etc/passwd.log" becomes just "passwd.log"
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("2024-01-01 10:00:00.0000|INFO|Test|Logger|1|1"u8.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "../../../etc/passwd.log");

        // Act
        var response = await Client.PostAsync("/api/upload", content);

        // Assert - file should be accepted with sanitized filename (passwd.log)
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var responseContent = await response.Content.ReadAsStringAsync();
        // Verify the filename was sanitized - should contain "passwd.log", not the full path
        Assert.That(responseContent, Does.Contain("passwd.log"));
        Assert.That(responseContent, Does.Not.Contain("../"));
    }

    [Test]
    public async Task Upload_WithValidLogFile_Returns200()
    {
        // Arrange
        var logContent = @"2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1
2024-01-15 10:30:46.5678|DEBUG|Loading config|MyApp.Config|1234|1";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.log");

        // Act
        var response = await Client.PostAsync("/api/upload", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.That(responseContent, Does.Contain("sessionId"));
        Assert.That(responseContent, Does.Contain("totalEntries"));
    }

    [Test]
    public async Task Upload_WithTxtExtension_Returns200()
    {
        // Arrange
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|Logger|1|1";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await Client.PostAsync("/api/upload", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Upload_WithEmptyFile_Returns400WithApiError()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "empty.log");

        // Act
        var response = await Client.PostAsync("/api/upload", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest");
    }

    [Test]
    public async Task Upload_ResponseContainsRequiredFields()
    {
        // Arrange
        var logContent = @"2024-01-15 10:30:45.1234|ERROR|Error message|MyApp.Service|1234|1
2024-01-15 10:30:46.5678|WARN|Warning message|MyApp.Service|1234|1
2024-01-15 10:30:47.9012|INFO|Info message|MyApp.Service|1234|1";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "app.log");

        // Act
        var response = await Client.PostAsync("/api/upload", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var responseContent = await response.Content.ReadAsStringAsync();

        // Check for required fields in response
        Assert.That(responseContent, Does.Contain("sessionId"));
        Assert.That(responseContent, Does.Contain("fileName"));
        Assert.That(responseContent, Does.Contain("filePath"));
        Assert.That(responseContent, Does.Contain("totalEntries"));
        Assert.That(responseContent, Does.Contain("levelCounts"));
    }
}
