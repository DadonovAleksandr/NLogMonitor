using System.Text;
using nLogMonitor.Domain.Entities;
using nLogMonitor.Infrastructure.Export;

namespace nLogMonitor.Infrastructure.Tests.Export;

[TestFixture]
public class CsvExporterTests
{
    private CsvExporter _exporter = null!;
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private const string ExpectedHeader = "Id,Timestamp,Level,Message,Logger,ProcessId,ThreadId,Exception";

    [SetUp]
    public void Setup()
    {
        _exporter = new CsvExporter();
    }

    // === Properties ===

    [Test]
    public void Format_ReturnsCsv()
    {
        Assert.That(_exporter.Format, Is.EqualTo("csv"));
    }

    [Test]
    public void ContentType_ReturnsTextCsv()
    {
        Assert.That(_exporter.ContentType, Is.EqualTo("text/csv"));
    }

    [Test]
    public void FileExtension_ReturnsDotCsv()
    {
        Assert.That(_exporter.FileExtension, Is.EqualTo(".csv"));
    }

    // === ExportToStreamAsync ===

    [Test]
    public async Task ExportToStreamAsync_WithEmptyEntries_WritesOnlyHeader()
    {
        // Arrange
        var entries = Array.Empty<LogEntry>();
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(entries, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines.Length, Is.EqualTo(1));
        Assert.That(lines[0], Is.EqualTo(ExpectedHeader));
    }

    [Test]
    public async Task ExportToStreamAsync_WritesUtf8Bom()
    {
        // Arrange
        var entries = Array.Empty<LogEntry>();
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(entries, stream);

        // Assert
        stream.Position = 0;
        var bytes = new byte[3];
        var bytesRead = await stream.ReadAsync(bytes, 0, 3);

        Assert.That(bytesRead, Is.EqualTo(3));
        Assert.That(bytes, Is.EqualTo(Utf8Bom));
    }

    [Test]
    public async Task ExportToStreamAsync_WithSingleEntry_WritesHeaderAndEntry()
    {
        // Arrange
        // 1234 sub-milliseconds via ticks (1234 * 100 nanoseconds = 0.1234 ms)
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc).AddTicks(1234);
        var entry = new LogEntry
        {
            Id = 1,
            Timestamp = timestamp,
            Level = LogLevel.Info,
            Message = "Test message",
            Logger = "TestLogger",
            ProcessId = 1000,
            ThreadId = 1,
            Exception = null
        };
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines.Length, Is.EqualTo(2));
        Assert.That(lines[0], Is.EqualTo(ExpectedHeader));
        Assert.That(lines[1], Does.StartWith("1,2024-01-15 10:30:45.0001,Info,Test message,TestLogger,1000,1,"));
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
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines.Length, Is.EqualTo(4)); // header + 3 entries
        Assert.That(lines[1], Does.StartWith("1,"));
        Assert.That(lines[2], Does.StartWith("2,"));
        Assert.That(lines[3], Does.StartWith("3,"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithCommaInMessage_QuotesField()
    {
        // Arrange
        var entry = CreateTestEntry(1, "Message with, comma");
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();

        Assert.That(content, Does.Contain("\"Message with, comma\""));
    }

    [Test]
    public async Task ExportToStreamAsync_WithQuoteInMessage_EscapesQuotes()
    {
        // Arrange
        var entry = CreateTestEntry(1, "Message with \"quotes\"");
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();

        Assert.That(content, Does.Contain("\"Message with \"\"quotes\"\"\""));
    }

    [Test]
    public async Task ExportToStreamAsync_WithNewlineInMessage_QuotesField()
    {
        // Arrange
        var entry = CreateTestEntry(1, "Message with\nnewline");
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();

        Assert.That(content, Does.Contain("\"Message with\nnewline\""));
    }

    [Test]
    public async Task ExportToStreamAsync_WithCarriageReturnInMessage_QuotesField()
    {
        // Arrange
        var entry = CreateTestEntry(1, "Message with\rcarriage return");
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();

        Assert.That(content, Does.Contain("\"Message with\rcarriage return\""));
    }

    [Test]
    public async Task ExportToStreamAsync_WithException_IncludesException()
    {
        // Arrange
        var entry = CreateTestEntry(1, "Error occurred");
        entry.Exception = "System.Exception: Test exception";
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();

        Assert.That(content, Does.Contain("System.Exception: Test exception"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithNullException_WritesEmptyField()
    {
        // Arrange
        var entry = CreateTestEntry(1, "No exception");
        entry.Exception = null;
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Last field should be empty (ends with comma or nothing after last comma)
        Assert.That(lines[1].EndsWith(",") || !lines[1].Split(',').Last().Contains("Exception"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var entries = Enumerable.Range(1, 10000).Select(i => CreateTestEntry(i, $"Message {i}"));
        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - OperationCanceledException (или наследник TaskCanceledException)
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
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();

        Assert.That(content, Does.Contain(",Trace,"));
        Assert.That(content, Does.Contain(",Debug,"));
        Assert.That(content, Does.Contain(",Info,"));
        Assert.That(content, Does.Contain(",Warn,"));
        Assert.That(content, Does.Contain(",Error,"));
        Assert.That(content, Does.Contain(",Fatal,"));
    }

    [Test]
    public async Task ExportToStreamAsync_WithEmptyMessage_WritesEmptyField()
    {
        // Arrange
        var entry = CreateTestEntry(1, string.Empty);
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Should have consecutive commas for empty message field
        Assert.That(lines[1], Does.Contain(",,"));
    }

    [Test]
    public async Task ExportToStreamAsync_TimestampFormat_IsCorrect()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc).AddTicks(1234 * TimeSpan.TicksPerMillisecond / 10);
        var entry = new LogEntry
        {
            Id = 1,
            Timestamp = timestamp,
            Level = LogLevel.Info,
            Message = "Test",
            Logger = "TestLogger",
            ProcessId = 1000,
            ThreadId = 1
        };
        using var stream = new MemoryStream();

        // Act
        await _exporter.ExportToStreamAsync(new[] { entry }, stream);

        // Assert
        stream.Position = 0;
        var content = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();

        // Format: yyyy-MM-dd HH:mm:ss.ffff
        Assert.That(content, Does.Contain("2024-01-15 10:30:45."));
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
