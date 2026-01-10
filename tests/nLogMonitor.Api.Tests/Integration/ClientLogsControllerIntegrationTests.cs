using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using nLogMonitor.Application.DTOs;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class ClientLogsControllerIntegrationTests : WebApplicationTestBase
{
    private const string Endpoint = "/api/client-logs";

    [Test]
    public async Task PostLogs_WithValidBatch_Returns200WithProcessedCount()
    {
        // Arrange
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "Application started" },
            new ClientLogDto { Level = "debug", Message = "Loading configuration" },
            new ClientLogDto { Level = "error", Message = "Connection failed" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(3));
        Assert.That(result.Failed, Is.EqualTo(0));
        Assert.That(result.Errors, Is.Null.Or.Empty);
    }

    [Test]
    public async Task PostLogs_WithSingleValidLog_Returns200()
    {
        // Arrange
        var logs = new[]
        {
            new ClientLogDto
            {
                Level = "warn",
                Message = "User session about to expire",
                Logger = "SessionManager",
                Url = "https://example.com/dashboard",
                UserId = "user123",
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
        Assert.That(result.Failed, Is.EqualTo(0));
    }

    [Test]
    public async Task PostLogs_WithMissingLevel_Returns400BadRequest()
    {
        // Arrange - Level is empty string (default)
        var logs = new[]
        {
            new ClientLogDto { Level = "", Message = "Message without level" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "Level is required");
    }

    [Test]
    public async Task PostLogs_WithMissingMessage_Returns400BadRequest()
    {
        // Arrange - Message is empty string (default)
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "Message is required");
    }

    [Test]
    public async Task PostLogs_WithInvalidLevel_Returns400BadRequest()
    {
        // Arrange
        var logs = new[]
        {
            new ClientLogDto { Level = "invalidlevel", Message = "Some message" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "Invalid log level");
    }

    [Test]
    public async Task PostLogs_WithWarningLevel_Returns200_NormalizesToWarn()
    {
        // Arrange - "warning" is a valid alias that normalizes to "warn"
        var logs = new[]
        {
            new ClientLogDto { Level = "warning", Message = "Warning message" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithFatalLevel_Returns200_NormalizesToError()
    {
        // Arrange - "fatal" is a valid alias that normalizes to "error"
        var logs = new[]
        {
            new ClientLogDto { Level = "fatal", Message = "Fatal error occurred" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithCriticalLevel_Returns200_NormalizesToError()
    {
        // Arrange - "critical" is a valid alias that normalizes to "error"
        var logs = new[]
        {
            new ClientLogDto { Level = "critical", Message = "Critical error occurred" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithAllLevelAliases_Returns200()
    {
        // Arrange - test all valid levels including aliases
        var logs = new[]
        {
            new ClientLogDto { Level = "trace", Message = "Trace message" },
            new ClientLogDto { Level = "debug", Message = "Debug message" },
            new ClientLogDto { Level = "info", Message = "Info message" },
            new ClientLogDto { Level = "warn", Message = "Warn message" },
            new ClientLogDto { Level = "warning", Message = "Warning message (alias)" },
            new ClientLogDto { Level = "error", Message = "Error message" },
            new ClientLogDto { Level = "fatal", Message = "Fatal message (alias)" },
            new ClientLogDto { Level = "critical", Message = "Critical message (alias)" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(8));
        Assert.That(result.Failed, Is.EqualTo(0));
    }

    [Test]
    public async Task PostLogs_WithEmptyArray_Returns400BadRequest()
    {
        // Arrange
        var logs = Array.Empty<ClientLogDto>();

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "No log entries provided");
    }

    [Test]
    public async Task PostLogs_WithMessageExceedingMaxLength_Returns400BadRequest()
    {
        // Arrange - MaxMessageLength is 10000 characters
        var longMessage = new string('A', 10001);
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = longMessage }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "10000 characters");
    }

    [Test]
    public async Task PostLogs_WithMessageAtMaxLength_Returns200()
    {
        // Arrange - exactly 10000 characters should be valid
        var exactMaxMessage = new string('B', 10000);
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = exactMaxMessage }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithMixedValidAndInvalid_ReturnsPartialSuccess()
    {
        // Arrange - mix of valid and invalid logs
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "Valid message 1" },
            new ClientLogDto { Level = "", Message = "Invalid - missing level" },
            new ClientLogDto { Level = "debug", Message = "Valid message 2" },
            new ClientLogDto { Level = "info", Message = "" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(2), "Should process 2 valid logs");
        Assert.That(result.Failed, Is.EqualTo(2), "Should fail 2 invalid logs");
        Assert.That(result.Errors, Is.Not.Null);
        Assert.That(result.Errors, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task PostLogs_WithAllOptionalFields_Returns200()
    {
        // Arrange - log with all optional fields populated
        var logs = new[]
        {
            new ClientLogDto
            {
                Level = "info",
                Message = "Full log entry",
                Logger = "App.Component",
                Url = "https://app.example.com/page?param=value",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                UserId = "user-123-abc",
                Version = "1.2.3",
                SessionId = "session-xyz-789",
                Timestamp = DateTime.UtcNow,
                Context = new Dictionary<string, object>
                {
                    { "browser", "Chrome" },
                    { "screenWidth", 1920 },
                    { "feature", "dashboard" }
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithCaseInsensitiveLevel_Returns200()
    {
        // Arrange - levels should be case insensitive
        var logs = new[]
        {
            new ClientLogDto { Level = "INFO", Message = "Uppercase level" },
            new ClientLogDto { Level = "Debug", Message = "Mixed case level" },
            new ClientLogDto { Level = "WARN", Message = "Another uppercase" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(3));
    }

    [Test]
    public async Task PostLogs_WithLoggerExceedingMaxLength_Returns400BadRequest()
    {
        // Arrange - MaxShortStringLength is 500 characters
        var longLogger = new string('L', 501);
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "Message", Logger = longLogger }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "500 characters");
    }

    [Test]
    public async Task PostLogs_WithUrlExceedingMaxLength_Returns400BadRequest()
    {
        // Arrange - MaxUrlLength is 2000 characters
        var longUrl = "https://example.com/" + new string('u', 2001);
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "Message", Url = longUrl }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertApiErrorAsync(response, "BadRequest", "2000 characters");
    }

    [Test]
    public async Task PostLogs_WithSpecialCharactersInMessage_Returns200()
    {
        // Arrange - message with special characters that should be sanitized
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "Message with <script>alert('xss')</script>" },
            new ClientLogDto { Level = "debug", Message = "Message with \"quotes\" and 'apostrophes'" },
            new ClientLogDto { Level = "warn", Message = "Message with\nnewlines\tand\ttabs" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(3));
    }

    [Test]
    public async Task PostLogs_WithUnicodeMessage_Returns200()
    {
        // Arrange - message with unicode characters
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "Сообщение на русском языке" },
            new ClientLogDto { Level = "debug", Message = "日本語メッセージ" },
            new ClientLogDto { Level = "warn", Message = "Emoji test: 🎉 ✅ ❌" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(3));
    }

    [Test]
    public async Task PostLogs_WithNullTimestamp_Returns200()
    {
        // Arrange - timestamp is optional and should default to server time
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "No timestamp provided", Timestamp = null }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithNullContext_Returns200()
    {
        // Arrange
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "No context", Context = null }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithEmptyContext_Returns200()
    {
        // Arrange
        var logs = new[]
        {
            new ClientLogDto { Level = "info", Message = "Empty context", Context = new Dictionary<string, object>() }
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(1));
    }

    [Test]
    public async Task PostLogs_WithLargeBatch_Returns200()
    {
        // Arrange - send 50 logs at once
        var logs = Enumerable.Range(1, 50)
            .Select(i => new ClientLogDto { Level = "info", Message = $"Log entry #{i}" })
            .ToArray();

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, logs);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ClientLogsResponse>(DefaultJsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Processed, Is.EqualTo(50));
        Assert.That(result.Failed, Is.EqualTo(0));
    }

    #region Response DTO

    private class ClientLogsResponse
    {
        public int Processed { get; set; }
        public int Failed { get; set; }
        public List<string>? Errors { get; set; }
    }

    #endregion
}
