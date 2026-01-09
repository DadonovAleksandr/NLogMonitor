using System.Text;
using System.Text.Json;
using nLogMonitor.Domain.Entities;
using nLogMonitor.Infrastructure.Export;

namespace nLogMonitor.Infrastructure.Tests.Export;

[TestFixture]
public class JsonExporterTests
{
    private JsonExporter _exporter = null!;

    [SetUp]
    public void Setup()
    {
        _exporter = new JsonExporter();
    }

    // === Properties ===

    [Test]
    public void Format_ReturnsJson()
    {
        Assert.That(_exporter.Format, Is.EqualTo("json"));
    }

    [Test]
    public void ContentType_ReturnsApplicationJson()
    {
        Assert.That(_exporter.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public void FileExtension_ReturnsDotJson()
    {
        Assert.That(_exporter.FileExtension, Is.EqualTo(".json"));
    }

    // === ExportToStreamAsync ===

    [Test]
    public async Task ExportToStreamAsync_WithEmptyEntries_WritesEmptyArray()
    {
        // Arrange
        var entries = Array.Empty<LogEntry>();
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(entries, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream).ReadToEndAsync();
        Assert.That(content, Is.EqualTo("[]"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithSingleEntry_WritesValidJson()
    {
        // Arrange
        var entry = CreateTestEntry(1, "Test message");
        var entries = new[] { entry };
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(entries, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream).ReadToEndAsync();

        // Parse as JSON array
        var jsonArray = JsonDocument.Parse(content).RootElement;
        Assert.That(jsonArray.GetArrayLength(), Is.EqualTo(1));

        var firstElement = jsonArray[0];
        Assert.That(firstElement.GetProperty("id").GetInt64(), Is.EqualTo(1));
        Assert.That(firstElement.GetProperty("message").GetString(), Is.EqualTo("Test message"));
        Assert.That(firstElement.GetProperty("level").GetString(), Is.EqualTo("Info"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithMultipleEntries_WritesAllEntries()
    {
        // Arrange
        var entries = new[]
        {
            CreateTestEntry(1, "First message"),
            CreateTestEntry(2, "Second message"),
            CreateTestEntry(3, "Third message")
        };
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(entries, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream).ReadToEndAsync();

        var jsonArray = JsonDocument.Parse(content).RootElement;
        Assert.That(jsonArray.GetArrayLength(), Is.EqualTo(3));
        Assert.That(jsonArray[0].GetProperty("id").GetInt64(), Is.EqualTo(1));
        Assert.That(jsonArray[1].GetProperty("id").GetInt64(), Is.EqualTo(2));
        Assert.That(jsonArray[2].GetProperty("id").GetInt64(), Is.EqualTo(3));
    }

    [Test]
    public async Task ExportToStreamAsync_WithAllFields_WritesAllFields()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, 123);
        var entry = new LogEntry
        {
            Id = 42,
            Timestamp = timestamp,
            Level = LogLevel.Error,
            Message = "Error occurred",
            Logger = "MyApp.Services.UserService",
            ProcessId = 1234,
            ThreadId = 5678,
            Exception = "System.Exception: Test"
        };
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream).ReadToEndAsync();
        var jsonArray = JsonDocument.Parse(content).RootElement;
        var element = jsonArray[0];

        Assert.That(element.GetProperty("id").GetInt64(), Is.EqualTo(42));
        Assert.That(element.GetProperty("level").GetString(), Is.EqualTo("Error"));
        Assert.That(element.GetProperty("message").GetString(), Is.EqualTo("Error occurred"));
        Assert.That(element.GetProperty("logger").GetString(), Is.EqualTo("MyApp.Services.UserService"));
        Assert.That(element.GetProperty("processId").GetInt32(), Is.EqualTo(1234));
        Assert.That(element.GetProperty("threadId").GetInt32(), Is.EqualTo(5678));
        Assert.That(element.GetProperty("exception").GetString(), Is.EqualTo("System.Exception: Test"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithNullException_WritesNullValue()
    {
        // Arrange
        var entry = CreateTestEntry(1, "No exception");
        entry.Exception = null;
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream).ReadToEndAsync();
        var jsonArray = JsonDocument.Parse(content).RootElement;
        var element = jsonArray[0];

        Assert.That(element.GetProperty("exception").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task ExportToStreamAsync_WithSpecialCharactersInMessage_EscapesCorrectly()
    {
        // Arrange
        var entry = CreateTestEntry(1, "Message with \"quotes\" and \\ backslash and \n newline");
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream).ReadToEndAsync();
        var jsonArray = JsonDocument.Parse(content).RootElement;
        var message = jsonArray[0].GetProperty("message").GetString();

        Assert.That(message, Is.EqualTo("Message with \"quotes\" and \\ backslash and \n newline"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var entries = Enumerable.Range(1, 10000).Select(i => CreateTestEntry(i, $"Message {i}"));
        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _exporter.ExportToStreamAsync(entries, stream, cts.Token));
    }

    [Test]
    public async Task ExportToStreamAsync_DoesNotCloseStream()
    {
        // Arrange
        var entries = new[] { CreateTestEntry(1, "Test") };
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(entries, stream);

        // Assert - stream should still be usable
        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.CanWrite, Is.True);
    }

    [Test]
    public async Task ExportToStreamAsync_WithAllLogLevels_SerializesLevelsCorrectly()
    {
        // Arrange
        var entries = new[]
        {
            CreateTestEntryWithLevel(1, LogLevel.Trace),
            CreateTestEntryWithLevel(2, LogLevel.Debug),
            CreateTestEntryWithLevel(3, LogLevel.Info),
            CreateTestEntryWithLevel(4, LogLevel.Warn),
            CreateTestEntryWithLevel(5, LogLevel.Error),
            CreateTestEntryWithLevel(6, LogLevel.Fatal)
        };
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(entries, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream).ReadToEndAsync();
        var jsonArray = JsonDocument.Parse(content).RootElement;

        Assert.That(jsonArray[0].GetProperty("level").GetString(), Is.EqualTo("Trace"));
        Assert.That(jsonArray[1].GetProperty("level").GetString(), Is.EqualTo("Debug"));
        Assert.That(jsonArray[2].GetProperty("level").GetString(), Is.EqualTo("Info"));
        Assert.That(jsonArray[3].GetProperty("level").GetString(), Is.EqualTo("Warn"));
        Assert.That(jsonArray[4].GetProperty("level").GetString(), Is.EqualTo("Error"));
        Assert.That(jsonArray[5].GetProperty("level").GetString(), Is.EqualTo("Fatal"));
    }

    // === Helper methods ===

    private static LogEntry CreateTestEntry(long id, string message)
    {
        return new LogEntry
        {
            Id = id,
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Info,
            Message = message,
            Logger = "TestLogger",
            ProcessId = 1000,
            ThreadId = 1,
            Exception = null
        };
    }

    private static LogEntry CreateTestEntryWithLevel(long id, LogLevel level)
    {
        return new LogEntry
        {
            Id = id,
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = $"Message at {level}",
            Logger = "TestLogger",
            ProcessId = 1000,
            ThreadId = 1,
            Exception = null
        };
    }
}
