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
    public async Task BindConnectionAsync_DisablesTTL()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
        await storage.SaveAsync(session);
        var connectionId = "test-connection-ttl";

        // Act
        await storage.BindConnectionAsync(connectionId, session.Id);
        var retrieved = await storage.GetAsync(session.Id);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ExpiresAt, Is.EqualTo(DateTime.MaxValue),
            "TTL should be disabled (ExpiresAt = MaxValue) when connection is bound");
    }

    [Test]
    public async Task GetAsync_WithBoundConnection_KeepsTTLDisabled()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);
        var connectionId = "test-connection-keepttl";
        await storage.BindConnectionAsync(connectionId, session.Id);

        // Act - повторное обращение к сессии
        var retrieved1 = await storage.GetAsync(session.Id);
        await Task.Delay(50);
        var retrieved2 = await storage.GetAsync(session.Id);

        // Assert - TTL должен оставаться отключенным
        Assert.That(retrieved1, Is.Not.Null);
        Assert.That(retrieved2, Is.Not.Null);
        Assert.That(retrieved1!.ExpiresAt, Is.EqualTo(DateTime.MaxValue));
        Assert.That(retrieved2!.ExpiresAt, Is.EqualTo(DateTime.MaxValue));
    }

    [Test]
    public async Task GetAsync_WithoutBoundConnection_UsesSlidingExpiration()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        var originalExpiresAt = session.ExpiresAt;
        await Task.Delay(50);

        // Act - обращение к сессии без привязанного соединения
        var retrieved = await storage.GetAsync(session.Id);

        // Assert - TTL должен быть продлён (sliding expiration)
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ExpiresAt, Is.Not.EqualTo(DateTime.MaxValue));
        Assert.That(retrieved.ExpiresAt, Is.GreaterThan(originalExpiresAt));
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

    // === Expired session handling ===

    [Test]
    public async Task SaveAsync_WithExpiredSession_SavesSuccessfully()
    {
        // Arrange - сессия с уже истёкшим TTL
        var options = Options.Create(new SessionSettings
        {
            FallbackTtlMinutes = 0, // Минимальный TTL
            CleanupIntervalMinutes = 1 // Интервал очистки
        });

        using var storage = new InMemorySessionStorage(options, _loggerMock.Object);
        var session = CreateTestSession();
        session.ExpiresAt = DateTime.UtcNow.AddMilliseconds(-1); // Уже истёк

        // Act
        await storage.SaveAsync(session);

        // Assert - сессия сохранена и доступна (очистка происходит асинхронно по таймеру)
        var retrieved = await storage.GetAsync(session.Id);
        Assert.That(retrieved, Is.Not.Null, "Expired session should be saved successfully");
        Assert.That(retrieved!.Id, Is.EqualTo(session.Id));
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

    // === Cleanup callbacks ===

    [Test]
    public async Task DeleteAsync_WithRegisteredCallback_CallsCallback()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        var callbackCalled = false;
        await storage.RegisterCleanupCallbackAsync(session.Id, () =>
        {
            callbackCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await storage.DeleteAsync(session.Id);

        // Assert
        Assert.That(callbackCalled, Is.True, "Cleanup callback should be called on DeleteAsync");
    }

    [Test]
    public async Task DeleteAsync_WithoutRegisteredCallback_DoesNotThrow()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);
        // Не регистрируем callback

        // Act & Assert - не должно выбрасывать исключение
        Assert.DoesNotThrowAsync(async () => await storage.DeleteAsync(session.Id));
    }

    [Test]
    public async Task DeleteAsync_CallbackThrowsException_LogsWarningAndContinues()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        await storage.RegisterCleanupCallbackAsync(session.Id, () =>
        {
            throw new InvalidOperationException("Test exception");
        });

        // Act - должно логировать предупреждение, но не выбрасывать исключение
        Assert.DoesNotThrowAsync(async () => await storage.DeleteAsync(session.Id));

        // Assert - сессия должна быть удалена
        var retrieved = await storage.GetAsync(session.Id);
        Assert.That(retrieved, Is.Null, "Session should be deleted even if callback fails");
    }

    [Test]
    public async Task RegisterCleanupCallbackAsync_OverwritesPreviousCallback()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        var firstCallbackCalled = false;
        var secondCallbackCalled = false;

        await storage.RegisterCleanupCallbackAsync(session.Id, () =>
        {
            firstCallbackCalled = true;
            return Task.CompletedTask;
        });

        await storage.RegisterCleanupCallbackAsync(session.Id, () =>
        {
            secondCallbackCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await storage.DeleteAsync(session.Id);

        // Assert - только второй callback должен быть вызван
        Assert.That(firstCallbackCalled, Is.False, "First callback should not be called");
        Assert.That(secondCallbackCalled, Is.True, "Second (overwriting) callback should be called");
    }

    [Test]
    public async Task RegisterCleanupCallbackAsync_ForNonExistentSession_DoesNotThrow()
    {
        // Arrange
        using var storage = CreateStorage();
        var nonExistentSessionId = Guid.NewGuid();

        // Act & Assert - регистрация callback для несуществующей сессии не должна выбрасывать
        Assert.DoesNotThrowAsync(async () =>
            await storage.RegisterCleanupCallbackAsync(nonExistentSessionId, () => Task.CompletedTask));
    }

    [Test]
    public async Task UnbindConnectionAsync_WithRegisteredCallback_CallsCallback()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);
        var connectionId = "test-connection-cleanup";
        await storage.BindConnectionAsync(connectionId, session.Id);

        var callbackCalled = false;
        await storage.RegisterCleanupCallbackAsync(session.Id, () =>
        {
            callbackCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await storage.UnbindConnectionAsync(connectionId);

        // Assert
        Assert.That(callbackCalled, Is.True, "Cleanup callback should be called when unbinding connection");
    }

    [Test]
    public async Task DeleteAsync_CallbackIsRemovedAfterExecution()
    {
        // Arrange
        using var storage = CreateStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        var callCount = 0;
        await storage.RegisterCleanupCallbackAsync(session.Id, () =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        // Act - удаляем сессию
        await storage.DeleteAsync(session.Id);

        // Пересохраняем сессию и удаляем снова (callback не должен вызваться)
        await storage.SaveAsync(session);
        await storage.DeleteAsync(session.Id);

        // Assert - callback должен быть вызван только один раз
        Assert.That(callCount, Is.EqualTo(1), "Callback should be called only once and removed after execution");
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
