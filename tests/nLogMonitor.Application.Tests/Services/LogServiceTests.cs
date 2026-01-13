using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.Exceptions;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Application.Services;
using nLogMonitor.Domain.Entities;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Application.Tests.Services;

[TestFixture]
public class LogServiceTests
{
    private Mock<ILogParser> _logParserMock = null!;
    private Mock<ISessionStorage> _sessionStorageMock = null!;
    private Mock<IDirectoryScanner> _directoryScannerMock = null!;
    private Mock<ILogger<LogService>> _loggerMock = null!;
    private IOptions<SessionSettings> _sessionSettings = null!;
    private LogService _logService = null!;

    [SetUp]
    public void Setup()
    {
        _logParserMock = new Mock<ILogParser>();
        _sessionStorageMock = new Mock<ISessionStorage>();
        _directoryScannerMock = new Mock<IDirectoryScanner>();
        _loggerMock = new Mock<ILogger<LogService>>();
        _sessionSettings = Options.Create(new SessionSettings { FallbackTtlMinutes = 5 });

        _logService = new LogService(
            _logParserMock.Object,
            _sessionStorageMock.Object,
            _directoryScannerMock.Object,
            _loggerMock.Object,
            _sessionSettings);
    }

    // === GetLogsAsync - server-side filtering ===

    [Test]
    public async Task GetLogsAsync_FilterBySearchText_ReturnsMatchingEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - "error" matches "Error occurred" and "Fatal error!"
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, searchText: "error");

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(2)); // "Error occurred" and "Fatal error!"
        Assert.That(resultList.All(e => e.Message.Contains("error", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(totalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetLogsAsync_FilterBySearchText_CaseInsensitive()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - "ERROR" should match "Error occurred" and "Fatal error!" (case insensitive)
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, searchText: "ERROR");

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(2)); // Case insensitive search
        Assert.That(resultList.All(e => e.Message.Contains("error", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public async Task GetLogsAsync_FilterByMinLevel_ReturnsEntriesAboveLevel()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, minLevel: LogLevel.Warn);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(3)); // Warn, Error, Fatal
        Assert.That(resultList.All(e => e.Level >= LogLevel.Warn), Is.True);
        Assert.That(totalCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetLogsAsync_FilterByMaxLevel_ReturnsEntriesBelowLevel()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, maxLevel: LogLevel.Info);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(2)); // Info, Debug
        Assert.That(resultList.All(e => e.Level <= LogLevel.Info), Is.True);
        Assert.That(totalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetLogsAsync_FilterByLevelRange_ReturnsEntriesInRange()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(
            sessionId,
            minLevel: LogLevel.Debug,
            maxLevel: LogLevel.Warn);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(3)); // Debug, Info, Warn
        Assert.That(resultList.All(e => e.Level >= LogLevel.Debug && e.Level <= LogLevel.Warn), Is.True);
    }

    [Test]
    public async Task GetLogsAsync_FilterByDateRange_ReturnsEntriesInRange()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        var fromDate = new DateTime(2024, 1, 15, 10, 0, 1);
        var toDate = new DateTime(2024, 1, 15, 10, 0, 3);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, fromDate: fromDate, toDate: toDate);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(3)); // Entries with timestamps 1, 2, 3
        Assert.That(resultList.All(e => e.Timestamp >= fromDate && e.Timestamp <= toDate), Is.True);
        Assert.That(totalCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetLogsAsync_FilterByFromDate_ReturnsEntriesAfterDate()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Test entries have timestamps: 0, 1, 2, 3, 4 seconds (5 entries total)
        var fromDate = new DateTime(2024, 1, 15, 10, 0, 3);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, fromDate: fromDate);

        // Assert - entries at 3 and 4 seconds match (>= 3)
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(2)); // Entries at seconds 3 and 4
        Assert.That(resultList.All(e => e.Timestamp >= fromDate), Is.True);
    }

    [Test]
    public async Task GetLogsAsync_FilterByToDate_ReturnsEntriesBeforeDate()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        var toDate = new DateTime(2024, 1, 15, 10, 0, 2);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, toDate: toDate);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(3)); // Entries at 0, 1, 2
        Assert.That(resultList.All(e => e.Timestamp <= toDate), Is.True);
    }

    [Test]
    public async Task GetLogsAsync_FilterByLogger_ReturnsMatchingEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, logger: "Service");

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(2)); // MyApp.Service appears twice
        Assert.That(resultList.All(e => e.Logger.Contains("Service", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(totalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetLogsAsync_FilterByLogger_CaseInsensitive()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, logger: "service");

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(2));
        Assert.That(resultList.All(e => e.Logger.Contains("Service")), Is.True);
    }

    [Test]
    public async Task GetLogsAsync_CombinedFilters_ReturnsCorrectEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - filter by level >= Warn AND logger contains "Service"
        var (result, totalCount) = await _logService.GetLogsAsync(
            sessionId,
            minLevel: LogLevel.Warn,
            logger: "Service");

        // Assert - only Error from MyApp.Service matches
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(1));
        Assert.That(resultList.First().Level, Is.EqualTo(LogLevel.Error));
        Assert.That(resultList.First().Logger, Is.EqualTo("MyApp.Service"));
    }

    [Test]
    public async Task GetLogsAsync_NoMatchingFilters_ReturnsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = CreateTestEntries();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, searchText: "nonexistent");

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Is.Empty);
        Assert.That(totalCount, Is.EqualTo(0));
    }

    // === Pagination ===

    [Test]
    public async Task GetLogsAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = Enumerable.Range(0, 100)
            .Select(i => new LogEntry
            {
                Id = i,
                Message = $"Message {i}",
                Level = LogLevel.Info,
                Timestamp = DateTime.UtcNow.AddMinutes(i),
                Logger = "Test"
            })
            .ToList();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - page 2, pageSize 10
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, page: 2, pageSize: 10);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(10));
        Assert.That(totalCount, Is.EqualTo(100));
        Assert.That(resultList.First().Message, Is.EqualTo("Message 10")); // Skip 10, take 10
        Assert.That(resultList.Last().Message, Is.EqualTo("Message 19"));
    }

    [Test]
    public async Task GetLogsAsync_FirstPage_ReturnsFirstEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = Enumerable.Range(0, 50)
            .Select(i => new LogEntry
            {
                Id = i,
                Message = $"Message {i}",
                Level = LogLevel.Info,
                Logger = "Test"
            })
            .ToList();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - page 1, pageSize 10
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, page: 1, pageSize: 10);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(10));
        Assert.That(resultList.First().Message, Is.EqualTo("Message 0"));
        Assert.That(resultList.Last().Message, Is.EqualTo("Message 9"));
    }

    [Test]
    public async Task GetLogsAsync_LastPage_ReturnsRemainingEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = Enumerable.Range(0, 25)
            .Select(i => new LogEntry
            {
                Id = i,
                Message = $"Message {i}",
                Level = LogLevel.Info,
                Logger = "Test"
            })
            .ToList();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - page 3, pageSize 10 -> should return 5 entries (20-24)
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, page: 3, pageSize: 10);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(5));
        Assert.That(totalCount, Is.EqualTo(25));
        Assert.That(resultList.First().Message, Is.EqualTo("Message 20"));
        Assert.That(resultList.Last().Message, Is.EqualTo("Message 24"));
    }

    [Test]
    public async Task GetLogsAsync_PageBeyondData_ReturnsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = Enumerable.Range(0, 10)
            .Select(i => new LogEntry
            {
                Id = i,
                Message = $"Message {i}",
                Level = LogLevel.Info,
                Logger = "Test"
            })
            .ToList();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - page 5, pageSize 10 -> beyond available data
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId, page: 5, pageSize: 10);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Is.Empty);
        Assert.That(totalCount, Is.EqualTo(10)); // Total is still 10
    }

    [Test]
    public async Task GetLogsAsync_FilterWithPagination_ReturnsCorrectFilteredPage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var entries = Enumerable.Range(0, 100)
            .Select(i => new LogEntry
            {
                Id = i,
                Message = i % 2 == 0 ? $"Even {i}" : $"Odd {i}",
                Level = LogLevel.Info,
                Logger = "Test"
            })
            .ToList();
        var session = new LogSession { Id = sessionId, Entries = entries };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act - search for "Even", page 2, pageSize 10
        var (result, totalCount) = await _logService.GetLogsAsync(
            sessionId,
            searchText: "Even",
            page: 2,
            pageSize: 10);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(10));
        Assert.That(totalCount, Is.EqualTo(50)); // 50 even entries
        Assert.That(resultList.All(e => e.Message.Contains("Even")), Is.True);
    }

    // === Session not found ===

    [Test]
    public async Task GetLogsAsync_SessionNotFound_ReturnsEmptyResult()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync((LogSession?)null);

        // Act
        var (result, totalCount) = await _logService.GetLogsAsync(sessionId);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList, Is.Empty);
        Assert.That(totalCount, Is.EqualTo(0));
    }

    // === OpenDirectoryAsync ===

    [Test]
    public void OpenDirectoryAsync_EmptyDirectory_ThrowsNoLogFilesFoundException()
    {
        // Arrange
        var dirPath = @"C:\logs";
        _directoryScannerMock
            .Setup(x => x.FindLastLogFileByNameAsync(dirPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<NoLogFilesFoundException>(
            async () => await _logService.OpenDirectoryAsync(dirPath));

        Assert.That(exception!.DirectoryPath, Is.EqualTo(dirPath));
    }

    [Test]
    public void OpenDirectoryAsync_EmptyStringReturned_ThrowsNoLogFilesFoundException()
    {
        // Arrange
        var dirPath = @"C:\logs";
        _directoryScannerMock
            .Setup(x => x.FindLastLogFileByNameAsync(dirPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act & Assert
        Assert.ThrowsAsync<NoLogFilesFoundException>(
            async () => await _logService.OpenDirectoryAsync(dirPath));
    }

    // === GetSessionAsync ===

    [Test]
    public async Task GetSessionAsync_ExistingSession_ReturnsSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new LogSession
        {
            Id = sessionId,
            FileName = "test.log",
            FilePath = @"C:\logs\test.log",
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow
        };
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync(session);

        // Act
        var result = await _logService.GetSessionAsync(sessionId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(sessionId));
        Assert.That(result.FileName, Is.EqualTo("test.log"));
        Assert.That(result.FilePath, Is.EqualTo(@"C:\logs\test.log"));
    }

    [Test]
    public async Task GetSessionAsync_NonExistingSession_ReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync((LogSession?)null);

        // Act
        var result = await _logService.GetSessionAsync(sessionId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSessionAsync_CallsStorageWithCorrectId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _sessionStorageMock.Setup(x => x.GetAsync(sessionId)).ReturnsAsync((LogSession?)null);

        // Act
        await _logService.GetSessionAsync(sessionId);

        // Assert
        _sessionStorageMock.Verify(x => x.GetAsync(sessionId), Times.Once);
    }

    // === Constructor validation ===

    [Test]
    public void Constructor_NullLogParser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LogService(
            null!,
            _sessionStorageMock.Object,
            _directoryScannerMock.Object,
            _loggerMock.Object,
            _sessionSettings));
    }

    [Test]
    public void Constructor_NullSessionStorage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LogService(
            _logParserMock.Object,
            null!,
            _directoryScannerMock.Object,
            _loggerMock.Object,
            _sessionSettings));
    }

    [Test]
    public void Constructor_NullDirectoryScanner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LogService(
            _logParserMock.Object,
            _sessionStorageMock.Object,
            null!,
            _loggerMock.Object,
            _sessionSettings));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LogService(
            _logParserMock.Object,
            _sessionStorageMock.Object,
            _directoryScannerMock.Object,
            null!,
            _sessionSettings));
    }

    [Test]
    public void Constructor_NullSessionSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LogService(
            _logParserMock.Object,
            _sessionStorageMock.Object,
            _directoryScannerMock.Object,
            _loggerMock.Object,
            null!));
    }

    // === Helper methods ===

    private static List<LogEntry> CreateTestEntries()
    {
        return new List<LogEntry>
        {
            new() { Id = 1, Timestamp = new DateTime(2024, 1, 15, 10, 0, 0), Level = LogLevel.Info, Message = "Info message", Logger = "MyApp.Program", ProcessId = 1234, ThreadId = 1 },
            new() { Id = 2, Timestamp = new DateTime(2024, 1, 15, 10, 0, 1), Level = LogLevel.Debug, Message = "Debug message", Logger = "MyApp.Service", ProcessId = 1234, ThreadId = 2 },
            new() { Id = 3, Timestamp = new DateTime(2024, 1, 15, 10, 0, 2), Level = LogLevel.Warn, Message = "Warning message", Logger = "MyApp.Handler", ProcessId = 1234, ThreadId = 1 },
            new() { Id = 4, Timestamp = new DateTime(2024, 1, 15, 10, 0, 3), Level = LogLevel.Error, Message = "Error occurred", Logger = "MyApp.Service", ProcessId = 1234, ThreadId = 3 },
            new() { Id = 5, Timestamp = new DateTime(2024, 1, 15, 10, 0, 4), Level = LogLevel.Fatal, Message = "Fatal error!", Logger = "MyApp.Core", ProcessId = 1234, ThreadId = 1 },
        };
    }
}
