using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class LogsControllerIntegrationTests : WebApplicationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public async Task GetLogs_WithoutSession_Returns404WithApiError()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/logs/{nonExistentSessionId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await AssertApiErrorAsync(response, "NotFound", nonExistentSessionId.ToString());
    }

    [Test]
    public async Task GetLogs_WithInvalidSessionIdFormat_Returns404()
    {
        // Act
        var response = await Client.GetAsync("/api/logs/invalid-guid");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetLogs_AfterUpload_ReturnsLogs()
    {
        // Arrange - upload a log file first
        var logContent = @"2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1
2024-01-15 10:30:46.5678|DEBUG|Loading config|MyApp.Config|1234|1
2024-01-15 10:30:47.9012|ERROR|Connection failed|MyApp.Database|1234|2";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act
        var response = await Client.GetAsync($"/api/logs/{sessionId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(3));
        Assert.That(result.Items, Has.Count.EqualTo(3));
        Assert.That(result.Page, Is.EqualTo(1));
    }

    [Test]
    public async Task GetLogs_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - upload a log file with multiple entries
        var logContent = @"2024-01-15 10:30:45.1234|INFO|Message 1|Logger|1|1
2024-01-15 10:30:46.5678|INFO|Message 2|Logger|1|1
2024-01-15 10:30:47.9012|INFO|Message 3|Logger|1|1
2024-01-15 10:30:48.0000|INFO|Message 4|Logger|1|1
2024-01-15 10:30:49.0000|INFO|Message 5|Logger|1|1";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act - get page 2 with pageSize 2
        var response = await Client.GetAsync($"/api/logs/{sessionId}?page=2&pageSize=2");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(5));
        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Page, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(3));
        Assert.That(result.HasPreviousPage, Is.True);
        Assert.That(result.HasNextPage, Is.True);
    }

    [Test]
    public async Task GetLogs_WithLevelFilter_FiltersCorrectly()
    {
        // Arrange - upload a log file with different levels
        var logContent = @"2024-01-15 10:30:45.1234|TRACE|Trace message|Logger|1|1
2024-01-15 10:30:46.5678|DEBUG|Debug message|Logger|1|1
2024-01-15 10:30:47.9012|INFO|Info message|Logger|1|1
2024-01-15 10:30:48.0000|WARN|Warning message|Logger|1|1
2024-01-15 10:30:49.0000|ERROR|Error message|Logger|1|1
2024-01-15 10:30:50.0000|FATAL|Fatal message|Logger|1|1";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act - filter with minLevel=Warn (should return Warn, Error, Fatal)
        var response = await Client.GetAsync($"/api/logs/{sessionId}?minLevel=Warn");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(3));
        Assert.That(result.Items, Has.Count.EqualTo(3));

        // Verify only Warn, Error, Fatal levels are present
        var levels = result.Items.Select(i => i.Level).ToList();
        Assert.That(levels, Does.Contain("Warn"));
        Assert.That(levels, Does.Contain("Error"));
        Assert.That(levels, Does.Contain("Fatal"));
        Assert.That(levels, Does.Not.Contain("Info"));
        Assert.That(levels, Does.Not.Contain("Debug"));
        Assert.That(levels, Does.Not.Contain("Trace"));
    }

    [Test]
    public async Task GetLogs_WithMaxLevelFilter_FiltersCorrectly()
    {
        // Arrange
        var logContent = @"2024-01-15 10:30:45.1234|TRACE|Trace message|Logger|1|1
2024-01-15 10:30:46.5678|DEBUG|Debug message|Logger|1|1
2024-01-15 10:30:47.9012|INFO|Info message|Logger|1|1
2024-01-15 10:30:48.0000|ERROR|Error message|Logger|1|1";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act - filter with maxLevel=Debug (should return Trace, Debug)
        var response = await Client.GetAsync($"/api/logs/{sessionId}?maxLevel=Debug");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(2));

        var levels = result.Items.Select(i => i.Level).ToList();
        Assert.That(levels, Does.Contain("Trace"));
        Assert.That(levels, Does.Contain("Debug"));
        Assert.That(levels, Does.Not.Contain("Info"));
        Assert.That(levels, Does.Not.Contain("Error"));
    }

    [Test]
    public async Task GetLogs_WithSearch_FiltersCorrectly()
    {
        // Arrange
        var logContent = @"2024-01-15 10:30:45.1234|INFO|Application started successfully|Logger|1|1
2024-01-15 10:30:46.5678|INFO|User logged in|Logger|1|1
2024-01-15 10:30:47.9012|ERROR|Database connection failed|Logger|1|1
2024-01-15 10:30:48.0000|INFO|Application shutdown|Logger|1|1";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act - search for "Application"
        var response = await Client.GetAsync($"/api/logs/{sessionId}?search=Application");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(2));
        Assert.That(result.Items.All(i => i.Message.Contains("Application")), Is.True);
    }

    [Test]
    public async Task GetLogs_WithLoggerFilter_FiltersCorrectly()
    {
        // Arrange
        var logContent = @"2024-01-15 10:30:45.1234|INFO|Message 1|MyApp.Service|1|1
2024-01-15 10:30:46.5678|INFO|Message 2|MyApp.Controller|1|1
2024-01-15 10:30:47.9012|INFO|Message 3|MyApp.Service|1|1
2024-01-15 10:30:48.0000|INFO|Message 4|OtherApp.Repository|1|1";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act - filter by logger name
        var response = await Client.GetAsync($"/api/logs/{sessionId}?logger=MyApp.Service");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(2));
        Assert.That(result.Items.All(i => i.Logger == "MyApp.Service"), Is.True);
    }

    [Test]
    public async Task GetLogs_WithDateFilter_FiltersCorrectly()
    {
        // Arrange
        var logContent = @"2024-01-14 10:30:45.1234|INFO|Yesterday message|Logger|1|1
2024-01-15 10:30:45.1234|INFO|Today morning|Logger|1|1
2024-01-15 18:30:45.1234|INFO|Today evening|Logger|1|1
2024-01-16 10:30:45.1234|INFO|Tomorrow message|Logger|1|1";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act - filter by date range
        var fromDate = Uri.EscapeDataString("2024-01-15T00:00:00");
        var toDate = Uri.EscapeDataString("2024-01-15T23:59:59");
        var response = await Client.GetAsync($"/api/logs/{sessionId}?fromDate={fromDate}&toDate={toDate}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetLogs_WithInvalidPage_Returns400WithApiError()
    {
        // Arrange
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|Logger|1|1";
        var sessionId = await UploadLogFileAsync(logContent);

        // Act - page 0 is invalid
        var response = await Client.GetAsync($"/api/logs/{sessionId}?page=0");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest");
    }

    [Test]
    public async Task GetLogs_WithInvalidPageSize_Returns400WithApiError()
    {
        // Arrange
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|Logger|1|1";
        var sessionId = await UploadLogFileAsync(logContent);

        // Act - pageSize > 500 is invalid
        var response = await Client.GetAsync($"/api/logs/{sessionId}?pageSize=501");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest");
    }

    [Test]
    public async Task GetLogs_WithCombinedFilters_FiltersCorrectly()
    {
        // Arrange
        var logContent = @"2024-01-15 10:30:45.1234|INFO|User started action|MyApp.Service|1|1
2024-01-15 10:30:46.5678|DEBUG|User debug info|MyApp.Service|1|1
2024-01-15 10:30:47.9012|ERROR|User error occurred|MyApp.Service|1|1
2024-01-15 10:30:48.0000|ERROR|System error occurred|OtherApp.System|1|1";

        var sessionId = await UploadLogFileAsync(logContent);

        // Act - combine search + minLevel + logger
        var response = await Client.GetAsync(
            $"/api/logs/{sessionId}?search=User&minLevel=Info&logger=MyApp.Service");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(2)); // INFO + ERROR for User in MyApp.Service
    }

    [Test]
    public async Task GetLogs_ResponseContainsLogEntryFields()
    {
        // Arrange
        var logContent = "2024-01-15 10:30:45.1234|ERROR|Test error message|MyApp.TestLogger|5678|42";
        var sessionId = await UploadLogFileAsync(logContent);

        // Act
        var response = await Client.GetAsync($"/api/logs/{sessionId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items, Has.Count.EqualTo(1));

        var entry = result.Items[0];
        Assert.That(entry.Level, Is.EqualTo("Error"));
        Assert.That(entry.Message, Is.EqualTo("Test error message"));
        Assert.That(entry.Logger, Is.EqualTo("MyApp.TestLogger"));
        Assert.That(entry.ProcessId, Is.EqualTo(5678));
        Assert.That(entry.ThreadId, Is.EqualTo(42));
    }

    [Test]
    public async Task GetLogs_EmptyResult_ReturnsEmptyItems()
    {
        // Arrange
        var logContent = "2024-01-15 10:30:45.1234|INFO|Test message|Logger|1|1";
        var sessionId = await UploadLogFileAsync(logContent);

        // Act - search for something that doesn't exist
        var response = await Client.GetAsync($"/api/logs/{sessionId}?search=NonExistentText12345");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResultResponse>(content, JsonOptions);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(0));
        Assert.That(result.Items, Is.Empty);
    }

    #region Helper Methods

    private async Task<string> UploadLogFileAsync(string logContent)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(logContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.log");

        var response = await Client.PostAsync("/api/upload", content);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Upload should succeed");

        var result = await response.Content.ReadFromJsonAsync<UploadResponse>(JsonOptions);
        Assert.That(result, Is.Not.Null, "Could not deserialize upload response");
        Assert.That(result!.SessionId, Is.Not.EqualTo(Guid.Empty), "SessionId should not be empty");

        return result.SessionId.ToString();
    }

    #endregion

    #region Response DTOs

    private class PagedResultResponse
    {
        public List<LogEntryResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    private class LogEntryResponse
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Logger { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public int ThreadId { get; set; }
        public string? Exception { get; set; }
    }

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
