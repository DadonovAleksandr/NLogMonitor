using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using nLogMonitor.Api.Controllers;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Api.Tests.Controllers;

[TestFixture]
public class LogsControllerTests
{
    private Mock<ILogService> _logServiceMock = null!;
    private Mock<IValidator<FilterOptionsDto>> _validatorMock = null!;
    private Mock<ILogger<LogsController>> _loggerMock = null!;
    private LogsController _controller = null!;

    private readonly Guid _testSessionId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _logServiceMock = new Mock<ILogService>();
        _validatorMock = new Mock<IValidator<FilterOptionsDto>>();
        _loggerMock = new Mock<ILogger<LogsController>>();

        _controller = new LogsController(
            _logServiceMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        // Set up HttpContext for TraceIdentifier
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Default: validation passes
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<FilterOptionsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    #region Successful Requests

    [Test]
    public async Task GetLogs_WhenSessionExistsAndValidRequest_ReturnsOkWithLogs()
    {
        // Arrange
        var session = CreateTestSession();
        var logEntries = CreateTestLogEntries(5);

        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                It.IsAny<string?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<IEnumerable<LogLevel>?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((logEntries, 5));

        // Act
        var result = await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 1, 50);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var pagedResult = okResult.Value as PagedResultDto<LogEntryDto>;
        Assert.That(pagedResult, Is.Not.Null);
        Assert.That(pagedResult.Items.Count(), Is.EqualTo(5));
        Assert.That(pagedResult.TotalCount, Is.EqualTo(5));
    }

    [Test]
    public async Task GetLogs_WithFilterParameters_PassesParametersToService()
    {
        // Arrange
        var session = CreateTestSession();
        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                "test search",
                LogLevel.Info,
                LogLevel.Error,
                null,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                "TestLogger",
                2,
                100))
            .ReturnsAsync((Enumerable.Empty<LogEntry>(), 0));

        // Act
        await _controller.GetLogs(
            _testSessionId,
            search: "test search",
            minLevel: "Info",
            maxLevel: "Error",
            levels: null,
            fromDate: null,
            toDate: null,
            logger: "TestLogger",
            page: 2,
            pageSize: 100);

        // Assert
        _logServiceMock.Verify(s => s.GetLogsAsync(
            _testSessionId,
            "test search",
            LogLevel.Info,
            LogLevel.Error,
            null,
            null,
            null,
            "TestLogger",
            2,
            100), Times.Once);
    }

    [TestCase("trace", LogLevel.Trace)]
    [TestCase("TRACE", LogLevel.Trace)]
    [TestCase("Debug", LogLevel.Debug)]
    [TestCase("info", LogLevel.Info)]
    [TestCase("WARN", LogLevel.Warn)]
    [TestCase("Error", LogLevel.Error)]
    [TestCase("FATAL", LogLevel.Fatal)]
    public async Task GetLogs_WithCaseInsensitiveLevel_ParsesCorrectly(string levelString, LogLevel expectedLevel)
    {
        // Arrange
        var session = CreateTestSession();
        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        LogLevel? capturedMinLevel = null;
        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                It.IsAny<string?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<IEnumerable<LogLevel>?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Callback<Guid, string?, LogLevel?, LogLevel?, IEnumerable<LogLevel>?, DateTime?, DateTime?, string?, int, int>(
                (_, _, minLevel, _, _, _, _, _, _, _) => capturedMinLevel = minLevel)
            .ReturnsAsync((Enumerable.Empty<LogEntry>(), 0));

        // Act
        await _controller.GetLogs(
            _testSessionId, null, levelString, null, null, null, null, null, 1, 50);

        // Assert
        Assert.That(capturedMinLevel, Is.EqualTo(expectedLevel));
    }

    [Test]
    public async Task GetLogs_WithNullLevel_PassesNullToService()
    {
        // Arrange
        var session = CreateTestSession();
        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        LogLevel? capturedMinLevel = LogLevel.Info; // Set non-null to verify it becomes null
        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                It.IsAny<string?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<IEnumerable<LogLevel>?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Callback<Guid, string?, LogLevel?, LogLevel?, IEnumerable<LogLevel>?, DateTime?, DateTime?, string?, int, int>(
                (_, _, minLevel, _, _, _, _, _, _, _) => capturedMinLevel = minLevel)
            .ReturnsAsync((Enumerable.Empty<LogEntry>(), 0));

        // Act
        await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 1, 50);

        // Assert
        Assert.That(capturedMinLevel, Is.Null);
    }

    [Test]
    public async Task GetLogs_MapsLogEntriesToDtos()
    {
        // Arrange
        var session = CreateTestSession();
        var logEntry = new LogEntry
        {
            Id = 123,
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0),
            Level = LogLevel.Error,
            Message = "Test message",
            Logger = "TestNamespace.TestClass",
            ProcessId = 1234,
            ThreadId = 5,
            Exception = "System.Exception: Test exception"
        };

        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                It.IsAny<string?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<IEnumerable<LogLevel>?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((new[] { logEntry }, 1));

        // Act
        var result = await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 1, 50);

        // Assert
        var okResult = result.Result as OkObjectResult;
        var pagedResult = okResult!.Value as PagedResultDto<LogEntryDto>;
        var dto = pagedResult!.Items.First();

        Assert.That(dto.Id, Is.EqualTo(123));
        Assert.That(dto.Timestamp, Is.EqualTo(new DateTime(2024, 1, 15, 10, 30, 0)));
        Assert.That(dto.Level, Is.EqualTo("Error"));
        Assert.That(dto.Message, Is.EqualTo("Test message"));
        Assert.That(dto.Logger, Is.EqualTo("TestNamespace.TestClass"));
        Assert.That(dto.ProcessId, Is.EqualTo(1234));
        Assert.That(dto.ThreadId, Is.EqualTo(5));
        Assert.That(dto.Exception, Is.EqualTo("System.Exception: Test exception"));
    }

    [Test]
    public async Task GetLogs_WithDateFilters_PassesDatesToService()
    {
        // Arrange
        var session = CreateTestSession();
        var fromDate = new DateTime(2024, 1, 10);
        var toDate = new DateTime(2024, 1, 20);

        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                It.IsAny<string?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<IEnumerable<LogLevel>?>(),
                fromDate,
                toDate,
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((Enumerable.Empty<LogEntry>(), 0));

        // Act
        await _controller.GetLogs(
            _testSessionId, null, null, null, null, fromDate, toDate, null, 1, 50);

        // Assert
        _logServiceMock.Verify(s => s.GetLogsAsync(
            _testSessionId,
            null,
            null,
            null,
            null,
            fromDate,
            toDate,
            null,
            1,
            50), Times.Once);
    }

    #endregion

    #region Session Not Found

    [Test]
    public async Task GetLogs_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync((LogSession?)null);

        // Act
        var result = await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 1, 50);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));

        var errorResponse = notFoundResult.Value as ApiErrorResponse;
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse.Error, Is.EqualTo("NotFound"));
        Assert.That(errorResponse.Message, Does.Contain(_testSessionId.ToString()));
    }

    [Test]
    public async Task GetLogs_WhenSessionNotFound_DoesNotCallGetLogsAsync()
    {
        // Arrange
        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync((LogSession?)null);

        // Act
        await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 1, 50);

        // Assert
        _logServiceMock.Verify(s => s.GetLogsAsync(
            It.IsAny<Guid>(),
            It.IsAny<string?>(),
            It.IsAny<LogLevel?>(),
            It.IsAny<LogLevel?>(),
            It.IsAny<IEnumerable<LogLevel>?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Validation Errors

    [Test]
    public async Task GetLogs_WhenValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var validationFailures = new List<ValidationFailure>
        {
            new("Page", "Page must be at least 1"),
            new("PageSize", "Page size must be between 1 and 500")
        };
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<FilterOptionsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 0, 1000);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));

        var errorResponse = badRequestResult.Value as ApiErrorResponse;
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse.Error, Is.EqualTo("BadRequest"));
        Assert.That(errorResponse.Message, Does.Contain("Page must be at least 1"));
        Assert.That(errorResponse.Message, Does.Contain("Page size must be between 1 and 500"));
    }

    [Test]
    public async Task GetLogs_WhenValidationFails_DoesNotCallService()
    {
        // Arrange
        var validationFailures = new List<ValidationFailure>
        {
            new("MinLevel", "Invalid log level")
        };
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<FilterOptionsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 1, 50);

        // Assert
        _logServiceMock.Verify(s => s.GetSessionAsync(It.IsAny<Guid>()), Times.Never);
        _logServiceMock.Verify(s => s.GetLogsAsync(
            It.IsAny<Guid>(),
            It.IsAny<string?>(),
            It.IsAny<LogLevel?>(),
            It.IsAny<LogLevel?>(),
            It.IsAny<IEnumerable<LogLevel>?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Paging Information

    [Test]
    public async Task GetLogs_ReturnsCorrectPagingInfo()
    {
        // Arrange
        var session = CreateTestSession();
        var entries = CreateTestLogEntries(10);

        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                It.IsAny<string?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<LogLevel?>(),
                It.IsAny<IEnumerable<LogLevel>?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                2,
                25))
            .ReturnsAsync((entries, 100)); // 100 total, 10 returned

        // Act
        var result = await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 2, 25);

        // Assert
        var okResult = result.Result as OkObjectResult;
        var pagedResult = okResult!.Value as PagedResultDto<LogEntryDto>;

        Assert.That(pagedResult!.Page, Is.EqualTo(2));
        Assert.That(pagedResult.PageSize, Is.EqualTo(25));
        Assert.That(pagedResult.TotalCount, Is.EqualTo(100));
        Assert.That(pagedResult.TotalPages, Is.EqualTo(4)); // 100 / 25 = 4
        Assert.That(pagedResult.HasPreviousPage, Is.True);
        Assert.That(pagedResult.HasNextPage, Is.True);
    }

    #endregion

    #region Default Parameters

    [Test]
    public async Task GetLogs_WithDefaultParameters_UsesCorrectDefaults()
    {
        // Arrange
        var session = CreateTestSession();
        _logServiceMock.Setup(s => s.GetSessionAsync(_testSessionId))
            .ReturnsAsync(session);

        _logServiceMock.Setup(s => s.GetLogsAsync(
                _testSessionId,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                1,
                50))
            .ReturnsAsync((Enumerable.Empty<LogEntry>(), 0));

        // Act
        await _controller.GetLogs(
            _testSessionId, null, null, null, null, null, null, null, 1, 50);

        // Assert
        _logServiceMock.Verify(s => s.GetLogsAsync(
            _testSessionId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            50), Times.Once);
    }

    #endregion

    #region Helper Methods

    private LogSession CreateTestSession()
    {
        return new LogSession
        {
            Id = _testSessionId,
            CreatedAt = DateTime.UtcNow,
            FilePath = "/test/path/file.log"
        };
    }

    private IEnumerable<LogEntry> CreateTestLogEntries(int count)
    {
        return Enumerable.Range(1, count).Select(i => new LogEntry
        {
            Id = i,
            Timestamp = DateTime.UtcNow.AddMinutes(-i),
            Level = LogLevel.Info,
            Message = $"Test message {i}",
            Logger = "TestLogger",
            ProcessId = 1000,
            ThreadId = 1
        });
    }

    #endregion
}
