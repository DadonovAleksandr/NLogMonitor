using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class FilesControllerIntegrationTests : WebApplicationTestBase
{
    [Test]
    public async Task OpenFile_InWebMode_Returns404()
    {
        // Arrange
        var request = new { FilePath = "C:\\test.log" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/files/open", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task OpenDirectory_InWebMode_Returns404()
    {
        // Arrange
        var request = new { DirectoryPath = "C:\\logs" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/files/open-directory", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task StopWatching_WithNonExistentSession_Returns404()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/files/{sessionId}/stop-watching", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

[TestFixture]
public class FilesControllerDesktopIntegrationTests : DesktopModeTestBase
{
    [Test]
    public async Task OpenFile_InDesktopMode_WithMissingFile_Returns404WithApiError()
    {
        // Arrange
        var request = new { FilePath = "C:\\nonexistent\\test.log" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/files/open", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await AssertApiErrorAsync(response, "NotFound");
    }

    [Test]
    public async Task OpenFile_InDesktopMode_WithEmptyPath_Returns400WithApiError()
    {
        // Arrange
        var request = new { FilePath = "" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/files/open", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest");
    }

    [Test]
    public async Task OpenDirectory_InDesktopMode_WithMissingDirectory_Returns404WithApiError()
    {
        // Arrange
        var request = new { DirectoryPath = "C:\\nonexistent\\directory" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/files/open-directory", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await AssertApiErrorAsync(response, "NotFound");
    }

    [Test]
    public async Task OpenDirectory_InDesktopMode_WithEmptyPath_Returns400WithApiError()
    {
        // Arrange
        var request = new { DirectoryPath = "" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/files/open-directory", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest");
    }

    [Test]
    public async Task StopWatching_WithNonExistentSession_Returns404()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/files/{sessionId}/stop-watching", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
