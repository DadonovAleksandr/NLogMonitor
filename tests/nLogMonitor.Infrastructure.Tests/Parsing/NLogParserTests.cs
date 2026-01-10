using NUnit.Framework;
using nLogMonitor.Infrastructure.Parsing;
using nLogMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

// Alias для разрешения конфликта LogLevel
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Infrastructure.Tests.Parsing;

[TestFixture]
public class NLogParserTests
{
    private NLogParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        var loggerMock = new Mock<ILogger<NLogParser>>();
        _parser = new NLogParser(loggerMock.Object);
    }

    // === Однострочный парсинг ===

    [Test]
    public async Task ParseAsync_SingleLineEntry_ParsesCorrectly()
    {
        // Arrange
        var logContent = "2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1";
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(1));
        var entry = entries[0];
        Assert.Multiple(() =>
        {
            Assert.That(entry.Timestamp, Is.EqualTo(new DateTime(2024, 1, 15, 10, 30, 45, 123).AddTicks(4000)));
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Info));
            Assert.That(entry.Message, Is.EqualTo("Application started"));
            Assert.That(entry.Logger, Is.EqualTo("MyApp.Program"));
            Assert.That(entry.ProcessId, Is.EqualTo(1234));
            Assert.That(entry.ThreadId, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ParseAsync_AllLogLevels_ParsesCorrectly()
    {
        // Arrange
        var logContent = """
            2024-01-15 10:00:00.0000|TRACE|Trace message|Logger|1|1
            2024-01-15 10:00:01.0000|DEBUG|Debug message|Logger|1|1
            2024-01-15 10:00:02.0000|INFO|Info message|Logger|1|1
            2024-01-15 10:00:03.0000|WARN|Warn message|Logger|1|1
            2024-01-15 10:00:04.0000|ERROR|Error message|Logger|1|1
            2024-01-15 10:00:05.0000|FATAL|Fatal message|Logger|1|1
            """;
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(6));
        Assert.That(entries[0].Level, Is.EqualTo(LogLevel.Trace));
        Assert.That(entries[1].Level, Is.EqualTo(LogLevel.Debug));
        Assert.That(entries[2].Level, Is.EqualTo(LogLevel.Info));
        Assert.That(entries[3].Level, Is.EqualTo(LogLevel.Warn));
        Assert.That(entries[4].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(entries[5].Level, Is.EqualTo(LogLevel.Fatal));
    }

    [Test]
    public async Task ParseAsync_MultipleEntriesSameSecond_ParsesAll()
    {
        // Arrange
        var logContent = """
            2024-01-15 10:30:45.0001|INFO|First|Logger|1|1
            2024-01-15 10:30:45.0002|INFO|Second|Logger|1|1
            2024-01-15 10:30:45.0003|INFO|Third|Logger|1|1
            """;
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.That(entries.Select(e => e.Message), Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    // === Многострочный парсинг ===

    [Test]
    public async Task ParseAsync_MultilineWithStackTrace_ParsesCorrectly()
    {
        // Arrange
        var logContent = """
            2024-01-15 10:30:46.5678|ERROR|Unhandled exception: Object reference not set
               at MyApp.Service.Process() in C:\src\Service.cs:line 42
               at MyApp.Program.Main() in C:\src\Program.cs:line 15|MyApp.Service|1234|5
            2024-01-15 10:30:47.0000|INFO|Next log entry|MyApp.Program|1234|1
            """;
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(2));

        var errorEntry = entries[0];
        Assert.Multiple(() =>
        {
            Assert.That(errorEntry.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(errorEntry.Message, Does.Contain("Unhandled exception"));
            Assert.That(errorEntry.Message, Does.Contain("at MyApp.Service.Process()"));
            Assert.That(errorEntry.Message, Does.Contain("line 42"));
            Assert.That(errorEntry.Logger, Is.EqualTo("MyApp.Service"));
            Assert.That(errorEntry.ThreadId, Is.EqualTo(5));
        });

        var infoEntry = entries[1];
        Assert.That(infoEntry.Level, Is.EqualTo(LogLevel.Info));
        Assert.That(infoEntry.Message, Is.EqualTo("Next log entry"));
    }

    [Test]
    public async Task ParseAsync_MessageWithPipeCharacter_ParsesCorrectly()
    {
        // Arrange - сообщение содержит символ | внутри
        var logContent = """
            2024-01-15 10:30:45.1234|ERROR|Data: value1|value2|value3 failed|MyApp.Parser|1234|1
            2024-01-15 10:30:46.0000|INFO|Next|MyApp.Program|1234|1
            """;
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].Message, Does.Contain("value1|value2|value3"));
        Assert.That(entries[0].Logger, Is.EqualTo("MyApp.Parser"));
    }

    // === Определение границ записей ===

    [Test]
    public void CanParse_ValidDateFormat_ReturnsTrue()
    {
        // Arrange
        var line = "2024-01-15 10:30:45.1234|INFO|Test|Logger|1|1";

        // Act
        var result = _parser.CanParse(line);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanParse_InvalidLine_ReturnsFalse()
    {
        // Arrange
        var line = "   at MyApp.Service.Process() in C:\\src\\Service.cs:line 42";

        // Act
        var result = _parser.CanParse(line);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void CanParse_EmptyOrWhitespace_ReturnsFalse(string? line)
    {
        // Act
        var result = _parser.CanParse(line!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ParseAsync_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        using var stream = CreateStream("");

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task ParseAsync_OnlyWhitespace_ReturnsEmptyList()
    {
        // Arrange
        using var stream = CreateStream("   \n  \n   ");

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    // === ID assignment ===

    [Test]
    public async Task ParseAsync_AssignsSequentialIds()
    {
        // Arrange
        var logContent = """
            2024-01-15 10:00:00.0000|INFO|First|Logger|1|1
            2024-01-15 10:00:01.0000|INFO|Second|Logger|1|1
            2024-01-15 10:00:02.0000|INFO|Third|Logger|1|1
            """;
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries[0].Id, Is.EqualTo(1));
        Assert.That(entries[1].Id, Is.EqualTo(2));
        Assert.That(entries[2].Id, Is.EqualTo(3));
    }

    // === Exception detection ===

    [Test]
    public async Task ParseAsync_ErrorWithException_SetsExceptionField()
    {
        // Arrange
        var logContent = "2024-01-15 10:30:45.1234|ERROR|NullReferenceException: Object reference not set|MyApp.Service|1234|1";
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Exception, Is.Not.Null);
        Assert.That(entries[0].Exception, Does.Contain("Exception"));
    }

    [Test]
    public async Task ParseAsync_InfoWithExceptionWord_NoExceptionField()
    {
        // Arrange - INFO level не должен устанавливать Exception даже если слово есть
        var logContent = "2024-01-15 10:30:45.1234|INFO|Without any exception everything is fine|MyApp.Service|1234|1";
        using var stream = CreateStream(logContent);

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Exception, Is.Null);
    }

    // === Cancellation ===

    [Test]
    public void ParseAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var logContent = """
            2024-01-15 10:00:00.0000|INFO|First|Logger|1|1
            2024-01-15 10:00:01.0000|INFO|Second|Logger|1|1
            """;
        using var stream = CreateStream(logContent);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException наследуется от OperationCanceledException,
        // в .NET 10 StreamReader.ReadLineAsync выбрасывает TaskCanceledException
        // Используем CatchAsync который ловит и подтипы
        var exception = Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await _parser.ParseAsync(stream, cts.Token).ToListAsync();
        });
        Assert.That(exception, Is.Not.Null);
    }

    // === Large file simulation ===

    [Test]
    public async Task ParseAsync_LargeNumberOfEntries_ParsesAll()
    {
        // Arrange
        var sb = new StringBuilder();
        const int count = 1000;
        for (int i = 0; i < count; i++)
        {
            sb.AppendLine($"2024-01-15 10:00:{i / 60:D2}.{i % 60:D4}|INFO|Message {i}|Logger|1|1");
        }
        using var stream = CreateStream(sb.ToString());

        // Act
        var entries = await _parser.ParseAsync(stream).ToListAsync();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(count));
    }

    // === Helper methods ===

    private static MemoryStream CreateStream(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}

// Extension for IAsyncEnumerable
internal static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }
}
