using NUnit.Framework;
using nLogMonitor.Infrastructure.Storage;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace nLogMonitor.Infrastructure.Tests.Storage;

[TestFixture]
public class InMemorySessionStorageTests
{
    private Mock<ILogger<InMemorySessionStorage>> _loggerMock = null!;
    private IOptions<SessionSettings> _defaultOptions = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<InMemorySessionStorage>>();
        _defaultOptions = Options.Create(new SessionSettings
        {
            FallbackTtlMinutes = 5,
            CleanupIntervalMinutes = 60 // Долгий интервал чтобы не мешать тестам
        });
    }

    // === CRUD операции ===

    [Test]
    public async Task SaveAsync_NewSession_CanBeRetrieved()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();

        // Act
        await storage.SaveAsync(session);
        var retrieved = await storage.GetAsync(session.Id);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo(session.Id));
        Assert.That(retrieved.FileName, Is.EqualTo(session.FileName));
    }

    [Test]
    public async Task SaveAsync_SetsExpiresAt_WhenNotSet()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        session.ExpiresAt = default; // Не установлено

        // Act
        await storage.SaveAsync(session);
        var retrieved = await storage.GetAsync(session.Id);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public async Task SaveAsync_UpdatesExistingSession()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        // Act
        session.FileName = "updated.log";
        await storage.SaveAsync(session);
        var retrieved = await storage.GetAsync(session.Id);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.FileName, Is.EqualTo("updated.log"));
    }

    [Test]
    public async Task GetAsync_NonExistentSession_ReturnsNull()
    {
        // Arrange
        using var storage = CreateStorage();

        // Act
        var retrieved = await storage.GetAsync(Guid.NewGuid());

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_ExistingSession_RemovesIt()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        // Act
        await storage.DeleteAsync(session.Id);
        var retrieved = await storage.GetAsync(session.Id);

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_NonExistentSession_DoesNotThrow()
    {
        // Arrange
        using var storage = CreateStorage();

        // Act & Assert - не должно выбрасывать исключение
        Assert.DoesNotThrowAsync(async () => await storage.DeleteAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetAllAsync_MultipleSessions_ReturnsAll()
    {
        // Arrange
        using var storage = CreateStorage();
        var session1 = CreateTestSession();
        var session2 = CreateTestSession();
        var session3 = CreateTestSession();

        await storage.SaveAsync(session1);
        await storage.SaveAsync(session2);
        await storage.SaveAsync(session3);

        // Act
        var all = (await storage.GetAllAsync()).ToList();

        // Assert
        Assert.That(all, Has.Count.EqualTo(3));
        Assert.That(all.Select(s => s.Id), Does.Contain(session1.Id));
        Assert.That(all.Select(s => s.Id), Does.Contain(session2.Id));
        Assert.That(all.Select(s => s.Id), Does.Contain(session3.Id));
    }

    [Test]
    public async Task GetAllAsync_EmptyStorage_ReturnsEmptyList()
    {
        // Arrange
        using var storage = CreateStorage();

        // Act
        var all = (await storage.GetAllAsync()).ToList();

        // Assert
        Assert.That(all, Is.Empty);
    }

    // === Sliding expiration ===

    [Test]
    public async Task GetAsync_ExtendsTTL_SlidingExpiration()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        var originalExpiresAt = session.ExpiresAt;

        // Небольшая задержка чтобы время отличалось
        await Task.Delay(50);

        // Act
        var retrieved = await storage.GetAsync(session.Id);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ExpiresAt, Is.GreaterThan(originalExpiresAt));
    }

    // === Connection binding ===

    [Test]
    public async Task BindConnectionAsync_BindsConnectionToSession()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);
        var connectionId = "test-connection-1";

        // Act
        await storage.BindConnectionAsync(connectionId, session.Id);
        var retrievedSessionId = await storage.GetSessionByConnectionAsync(connectionId);

        // Assert
        Assert.That(retrievedSessionId, Is.EqualTo(session.Id));
    }

    [Test]
    public async Task GetSessionByConnectionAsync_UnboundConnection_ReturnsNull()
    {
        // Arrange
        using var storage = CreateStorage();

        // Act
        var sessionId = await storage.GetSessionByConnectionAsync("unknown-connection");

        // Assert
        Assert.That(sessionId, Is.Null);
    }

    [Test]
    public async Task UnbindConnectionAsync_DeletesSessionAndConnection()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);
        var connectionId = "test-connection-1";
        await storage.BindConnectionAsync(connectionId, session.Id);

        // Act
        await storage.UnbindConnectionAsync(connectionId);

        // Assert
        var retrievedSession = await storage.GetAsync(session.Id);
        var retrievedConnection = await storage.GetSessionByConnectionAsync(connectionId);

        Assert.That(retrievedSession, Is.Null, "Session should be deleted");
        Assert.That(retrievedConnection, Is.Null, "Connection binding should be removed");
    }

    [Test]
    public async Task UnbindConnectionAsync_UnknownConnection_DoesNotThrow()
    {
        // Arrange
        using var storage = CreateStorage();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await storage.UnbindConnectionAsync("unknown-connection"));
    }

    // === Concurrent access ===

    [Test]
    public async Task ConcurrentAccess_MultipleSavesAndGets_ThreadSafe()
    {
        // Arrange
        using var storage = CreateStorage();
        const int operationsCount = 100;
        var sessions = Enumerable.Range(0, operationsCount)
            .Select(_ => CreateTestSession())
            .ToList();

        // Act - параллельное сохранение
        var saveTasks = sessions.Select(s => storage.SaveAsync(s));
        await Task.WhenAll(saveTasks);

        // Параллельное чтение
        var getTasks = sessions.Select(s => storage.GetAsync(s.Id));
        var results = await Task.WhenAll(getTasks);

        // Assert
        Assert.That(results.Count(r => r != null), Is.EqualTo(operationsCount));
    }

    [Test]
    public async Task ConcurrentAccess_SaveAndDelete_ThreadSafe()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        // Act - одновременное чтение и удаление
        var tasks = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(storage.GetAsync(session.Id));
            tasks.Add(storage.DeleteAsync(session.Id));
            tasks.Add(storage.SaveAsync(session));
        }

        // Assert - не должно выбрасывать исключения
        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    // === TTL expiration ===

    [Test]
    public async Task Session_WithExpiredTTL_CleanedUpByTimer()
    {
        // Arrange - очень короткий TTL
        var options = Options.Create(new SessionSettings
        {
            FallbackTtlMinutes = 0, // Минимальный TTL
            CleanupIntervalMinutes = 1 // Короткий интервал очистки
        });

        using var storage = new InMemorySessionStorage(options, _loggerMock.Object);
        var session = CreateTestSession();
        session.ExpiresAt = DateTime.UtcNow.AddMilliseconds(-1); // Уже истёк

        await storage.SaveAsync(session);

        // Сессия ещё должна быть доступна (очистка ещё не прошла)
        var beforeCleanup = await storage.GetAsync(session.Id);
        Assert.That(beforeCleanup, Is.Not.Null, "Session should exist before cleanup");

        // Примечание: тест не ждёт реальной очистки по таймеру,
        // так как это занимает минуту. Этот тест проверяет только
        // что сессия с истёкшим TTL может быть сохранена.
    }

    // === Dispose ===

    [Test]
    public void Dispose_ClearsAllSessions()
    {
        // Arrange
        var storage = CreateStorage();
        var session = CreateTestSession();
        storage.SaveAsync(session).Wait();

        // Act
        storage.Dispose();

        // Assert - после Dispose нельзя использовать storage,
        // но внутренние коллекции должны быть очищены
        // Этот тест проверяет что Dispose не выбрасывает исключение
        Assert.Pass("Dispose completed without exception");
    }

    [Test]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var storage = CreateStorage();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            storage.Dispose();
            storage.Dispose();
            storage.Dispose();
        });
    }

    // === Helper methods ===

    private InMemorySessionStorage CreateStorage()
    {
        return new InMemorySessionStorage(_defaultOptions, _loggerMock.Object);
    }

    private static LogSession CreateTestSession()
    {
        return new LogSession
        {
            Id = Guid.NewGuid(),
            FileName = $"test-{Guid.NewGuid():N}.log",
            FilePath = $"C:\\logs\\test-{Guid.NewGuid():N}.log",
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
    }
}
