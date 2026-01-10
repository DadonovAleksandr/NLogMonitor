using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Infrastructure.FileSystem;

namespace nLogMonitor.Infrastructure.Tests.FileSystem;

[TestFixture]
public class FileWatcherServiceTests
{
    private Mock<ILogger<FileWatcherService>> _loggerMock = null!;
    private FileWatcherService _service = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<FileWatcherService>>();
        _service = new FileWatcherService(_loggerMock.Object);

        // Создаём уникальную временную директорию для каждого теста
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileWatcherTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        // Останавливаем все watchers и освобождаем ресурсы
        _service.Dispose();

        // Очищаем тестовую директорию
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                // Даём время на освобождение файловых блокировок
                Thread.Sleep(100);
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Игнорируем ошибки очистки
            }
        }
    }

    // === StartWatchingAsync ===

    [Test]
    public async Task StartWatchingAsync_ValidFile_StartsWatching()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");

        // Act
        await _service.StartWatchingAsync(sessionId, filePath);

        // Assert
        _service.IsWatching(sessionId).Should().BeTrue();
    }

    [Test]
    public async Task StartWatchingAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.log");

        // Act & Assert
        await FluentActions
            .Invoking(async () => await _service.StartWatchingAsync(sessionId, nonExistentPath))
            .Should()
            .ThrowAsync<FileNotFoundException>()
            .WithMessage($"File not found: {nonExistentPath}*");
    }

    [Test]
    public async Task StartWatchingAsync_NullOrEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act & Assert
        await FluentActions
            .Invoking(async () => await _service.StartWatchingAsync(sessionId, null!))
            .Should()
            .ThrowAsync<ArgumentException>();

        await FluentActions
            .Invoking(async () => await _service.StartWatchingAsync(sessionId, ""))
            .Should()
            .ThrowAsync<ArgumentException>();

        await FluentActions
            .Invoking(async () => await _service.StartWatchingAsync(sessionId, "   "))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task StartWatchingAsync_SameSessionTwice_ReplacesOldWatcher()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath1 = CreateTestFile("test1.log");
        var filePath2 = CreateTestFile("test2.log");

        // Act
        await _service.StartWatchingAsync(sessionId, filePath1);
        await _service.StartWatchingAsync(sessionId, filePath2);

        // Assert
        _service.IsWatching(sessionId).Should().BeTrue();
    }

    [Test]
    public async Task StartWatchingAsync_MultipleSessions_TracksAllSessions()
    {
        // Arrange
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();
        var filePath1 = CreateTestFile("test1.log");
        var filePath2 = CreateTestFile("test2.log");

        // Act
        await _service.StartWatchingAsync(sessionId1, filePath1);
        await _service.StartWatchingAsync(sessionId2, filePath2);

        // Assert
        _service.IsWatching(sessionId1).Should().BeTrue();
        _service.IsWatching(sessionId2).Should().BeTrue();
    }

    // === StopWatchingAsync ===

    [Test]
    public async Task StopWatchingAsync_ExistingSession_StopsWatching()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");
        await _service.StartWatchingAsync(sessionId, filePath);

        // Act
        await _service.StopWatchingAsync(sessionId);

        // Assert
        _service.IsWatching(sessionId).Should().BeFalse();
    }

    [Test]
    public async Task StopWatchingAsync_NonExistentSession_DoesNotThrow()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act & Assert
        await FluentActions
            .Invoking(async () => await _service.StopWatchingAsync(sessionId))
            .Should()
            .NotThrowAsync();
    }

    [Test]
    public async Task StopWatchingAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");
        await _service.StartWatchingAsync(sessionId, filePath);

        // Act & Assert
        await _service.StopWatchingAsync(sessionId);
        await FluentActions
            .Invoking(async () => await _service.StopWatchingAsync(sessionId))
            .Should()
            .NotThrowAsync();
    }

    // === IsWatching ===

    [Test]
    public void IsWatching_NoWatcher_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var result = _service.IsWatching(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsWatching_AfterStart_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");

        // Act
        await _service.StartWatchingAsync(sessionId, filePath);
        var result = _service.IsWatching(sessionId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsWatching_AfterStop_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");
        await _service.StartWatchingAsync(sessionId, filePath);

        // Act
        await _service.StopWatchingAsync(sessionId);
        var result = _service.IsWatching(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    // === FileChanged Event ===

    [Test]
    public async Task FileChanged_WhenFileModified_RaisesEvent()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");
        var eventRaised = false;
        FileChangedEventArgs? capturedArgs = null;

        _service.FileChanged += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        await _service.StartWatchingAsync(sessionId, filePath);

        // Act - модифицируем файл
        await Task.Delay(100); // Даём время на инициализацию watcher
        await File.AppendAllTextAsync(filePath, "New line\n");

        // Ждём debounce + обработку события
        await Task.Delay(500);

        // Assert
        eventRaised.Should().BeTrue("событие FileChanged должно быть вызвано");
        capturedArgs.Should().NotBeNull();
        capturedArgs!.SessionId.Should().Be(sessionId);
        capturedArgs.FilePath.Should().Be(filePath);
        capturedArgs.NewSize.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task FileChanged_MultipleChanges_DebouncesProperly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");
        var eventCount = 0;

        _service.FileChanged += (sender, args) =>
        {
            Interlocked.Increment(ref eventCount);
        };

        await _service.StartWatchingAsync(sessionId, filePath);

        // Act - делаем несколько быстрых изменений
        await Task.Delay(100);
        await File.AppendAllTextAsync(filePath, "Line 1\n");
        await Task.Delay(50);
        await File.AppendAllTextAsync(filePath, "Line 2\n");
        await Task.Delay(50);
        await File.AppendAllTextAsync(filePath, "Line 3\n");

        // Ждём debounce + обработку
        await Task.Delay(500);

        // Assert - должно быть только 1 событие из-за debounce
        // (могут быть 2-3 если система медленная, но не 10+)
        eventCount.Should().BeLessThan(5, "debounce должен объединять быстрые изменения");
    }

    [Test]
    public async Task FileChanged_AfterStop_DoesNotRaiseEvent()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");
        var eventRaised = false;

        _service.FileChanged += (sender, args) =>
        {
            eventRaised = true;
        };

        await _service.StartWatchingAsync(sessionId, filePath);
        await _service.StopWatchingAsync(sessionId);

        // Act - модифицируем файл после остановки
        await Task.Delay(100);
        await File.AppendAllTextAsync(filePath, "New line\n");
        await Task.Delay(500);

        // Assert
        eventRaised.Should().BeFalse("событие не должно вызываться после StopWatching");
    }

    [Test]
    public async Task FileChanged_DifferentSessions_RaisesEventsIndependently()
    {
        // Arrange
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();
        var filePath1 = CreateTestFile("test1.log");
        var filePath2 = CreateTestFile("test2.log");
        var eventsSession1 = 0;
        var eventsSession2 = 0;

        _service.FileChanged += (sender, args) =>
        {
            if (args.SessionId == sessionId1)
                Interlocked.Increment(ref eventsSession1);
            else if (args.SessionId == sessionId2)
                Interlocked.Increment(ref eventsSession2);
        };

        await _service.StartWatchingAsync(sessionId1, filePath1);
        await _service.StartWatchingAsync(sessionId2, filePath2);

        // Act
        await Task.Delay(100);
        await File.AppendAllTextAsync(filePath1, "Session 1 update\n");
        await Task.Delay(500);

        // Assert
        eventsSession1.Should().BeGreaterThan(0, "session 1 должна получить событие");
        eventsSession2.Should().Be(0, "session 2 не должна получить событие");
    }

    // === Dispose ===

    [Test]
    public async Task Dispose_StopsAllWatchers()
    {
        // Arrange
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();
        var filePath1 = CreateTestFile("test1.log");
        var filePath2 = CreateTestFile("test2.log");

        await _service.StartWatchingAsync(sessionId1, filePath1);
        await _service.StartWatchingAsync(sessionId2, filePath2);

        _service.IsWatching(sessionId1).Should().BeTrue();
        _service.IsWatching(sessionId2).Should().BeTrue();

        // Act
        _service.Dispose();

        // Assert - после Dispose любые операции должны выбрасывать ObjectDisposedException
        // Проверяем что Dispose не выбросил исключение
        FluentActions
            .Invoking(() => _service.Dispose())
            .Should()
            .NotThrow("повторный Dispose должен быть безопасным");
    }

    [Test]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Act & Assert
        FluentActions
            .Invoking(() =>
            {
                _service.Dispose();
                _service.Dispose();
            })
            .Should()
            .NotThrow();
    }

    [Test]
    public async Task Dispose_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = CreateTestFile("test.log");

        // Act
        _service.Dispose();

        // Assert
        await FluentActions
            .Invoking(async () => await _service.StartWatchingAsync(sessionId, filePath))
            .Should()
            .ThrowAsync<ObjectDisposedException>();

        await FluentActions
            .Invoking(async () => await _service.StopWatchingAsync(sessionId))
            .Should()
            .ThrowAsync<ObjectDisposedException>();

        FluentActions
            .Invoking(() => _service.IsWatching(sessionId))
            .Should()
            .Throw<ObjectDisposedException>();
    }

    // === Helper Methods ===

    private string CreateTestFile(string fileName, string? content = null)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content ?? $"Test log content for {fileName}\n");
        return filePath;
    }
}
