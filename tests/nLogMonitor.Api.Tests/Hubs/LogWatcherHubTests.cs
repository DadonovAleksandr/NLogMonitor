using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using nLogMonitor.Api.Tests.Integration;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Api.Tests.Hubs;

/// <summary>
/// Integration тесты для LogWatcherHub.
/// Проверяет жизненный цикл сессий через SignalR: JoinSession, LeaveSession, OnDisconnectedAsync.
/// </summary>
[TestFixture]
public class LogWatcherHubTests : WebApplicationTestBase
{
    private const string HubUrl = "/hubs/logwatcher";

    [Test]
    public async Task JoinSession_ValidSession_ReturnsSuccess()
    {
        // Arrange
        var session = await CreateTestSessionAsync();
        var connection = await CreateHubConnectionAsync();

        try
        {
            await connection.StartAsync();

            // Act
            var result = await connection.InvokeAsync<JoinSessionResultDto>("JoinSession", session.Id.ToString());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.SessionId, Is.EqualTo(session.Id.ToString()));
            Assert.That(result.FileName, Is.EqualTo(session.FileName));
            Assert.That(result.Error, Is.Null);
        }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task JoinSession_NonExistentSession_ReturnsError()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid();
        var connection = await CreateHubConnectionAsync();

        try
        {
            await connection.StartAsync();

            // Act
            var result = await connection.InvokeAsync<JoinSessionResultDto>("JoinSession", nonExistentSessionId.ToString());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Session not found"));
        }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task JoinSession_InvalidSessionIdFormat_ReturnsError()
    {
        // Arrange
        var connection = await CreateHubConnectionAsync();

        try
        {
            await connection.StartAsync();

            // Act
            var result = await connection.InvokeAsync<JoinSessionResultDto>("JoinSession", "invalid-guid");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Invalid session ID format"));
        }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task JoinSession_DisablesTTL()
    {
        // Arrange
        var session = await CreateTestSessionAsync();
        var storage = GetSessionStorage();
        var connection = await CreateHubConnectionAsync();

        try
        {
            await connection.StartAsync();

            // Проверяем TTL до JoinSession (должен быть нормальный)
            var sessionBefore = await storage.GetAsync(session.Id);
            Assert.That(sessionBefore!.ExpiresAt, Is.Not.EqualTo(DateTime.MaxValue),
                "Session should have normal TTL before joining");

            // Act
            await connection.InvokeAsync<JoinSessionResultDto>("JoinSession", session.Id.ToString());

            // Assert - TTL должен быть отключён
            var sessionAfter = await storage.GetAsync(session.Id);
            Assert.That(sessionAfter!.ExpiresAt, Is.EqualTo(DateTime.MaxValue),
                "Session TTL should be disabled (MaxValue) after joining");
        }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task LeaveSession_ExistingSession_RemovesSession()
    {
        // Arrange
        var session = await CreateTestSessionAsync();
        var storage = GetSessionStorage();
        var connection = await CreateHubConnectionAsync();

        try
        {
            await connection.StartAsync();
            await connection.InvokeAsync<JoinSessionResultDto>("JoinSession", session.Id.ToString());

            // Проверяем что сессия существует
            var sessionBefore = await storage.GetAsync(session.Id);
            Assert.That(sessionBefore, Is.Not.Null);

            // Act
            await connection.InvokeAsync("LeaveSession", session.Id.ToString());

            // Даём время на обработку
            await Task.Delay(100);

            // Assert - сессия должна быть удалена
            var sessionAfter = await storage.GetAsync(session.Id);
            Assert.That(sessionAfter, Is.Null, "Session should be removed after LeaveSession");
        }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task OnDisconnectedAsync_RemovesSession()
    {
        // Arrange
        var session = await CreateTestSessionAsync();
        var storage = GetSessionStorage();
        var connection = await CreateHubConnectionAsync();

        await connection.StartAsync();
        await connection.InvokeAsync<JoinSessionResultDto>("JoinSession", session.Id.ToString());

        // Проверяем что сессия существует
        var sessionBefore = await storage.GetAsync(session.Id);
        Assert.That(sessionBefore, Is.Not.Null);

        // Act - закрываем соединение (имитирует закрытие вкладки браузера)
        await connection.StopAsync();
        await connection.DisposeAsync();

        // Даём время на обработку OnDisconnectedAsync
        await Task.Delay(200);

        // Assert - сессия должна быть удалена
        var sessionAfter = await storage.GetAsync(session.Id);
        Assert.That(sessionAfter, Is.Null, "Session should be removed when connection is disconnected");
    }

    [Test]
    public async Task MultipleConnections_DifferentSessions_ManagedIndependently()
    {
        // Arrange
        var session1 = await CreateTestSessionAsync();
        var session2 = await CreateTestSessionAsync();
        var storage = GetSessionStorage();

        var connection1 = await CreateHubConnectionAsync();
        var connection2 = await CreateHubConnectionAsync();

        try
        {
            await connection1.StartAsync();
            await connection2.StartAsync();

            await connection1.InvokeAsync<JoinSessionResultDto>("JoinSession", session1.Id.ToString());
            await connection2.InvokeAsync<JoinSessionResultDto>("JoinSession", session2.Id.ToString());

            // Проверяем что обе сессии существуют
            Assert.That(await storage.GetAsync(session1.Id), Is.Not.Null);
            Assert.That(await storage.GetAsync(session2.Id), Is.Not.Null);

            // Act - закрываем только первое соединение
            await connection1.StopAsync();
            await connection1.DisposeAsync();
            await Task.Delay(200);

            // Assert - первая сессия удалена, вторая остаётся
            Assert.That(await storage.GetAsync(session1.Id), Is.Null,
                "Session 1 should be removed");
            Assert.That(await storage.GetAsync(session2.Id), Is.Not.Null,
                "Session 2 should still exist");
        }
        finally
        {
            await connection2.StopAsync();
            await connection2.DisposeAsync();
        }
    }

    [Test]
    public async Task JoinSession_SameSessionTwice_UpdatesBinding()
    {
        // Arrange
        var session = await CreateTestSessionAsync();
        var storage = GetSessionStorage();

        var connection1 = await CreateHubConnectionAsync();
        var connection2 = await CreateHubConnectionAsync();

        try
        {
            await connection1.StartAsync();
            await connection2.StartAsync();

            // Act - оба соединения присоединяются к одной сессии
            var result1 = await connection1.InvokeAsync<JoinSessionResultDto>("JoinSession", session.Id.ToString());
            var result2 = await connection2.InvokeAsync<JoinSessionResultDto>("JoinSession", session.Id.ToString());

            // Assert - оба должны успешно присоединиться
            Assert.That(result1.Success, Is.True);
            Assert.That(result2.Success, Is.True);

            // Сессия должна существовать
            var currentSession = await storage.GetAsync(session.Id);
            Assert.That(currentSession, Is.Not.Null);
            Assert.That(currentSession!.ExpiresAt, Is.EqualTo(DateTime.MaxValue));
        }
        finally
        {
            await connection1.StopAsync();
            await connection1.DisposeAsync();
            await connection2.StopAsync();
            await connection2.DisposeAsync();
        }
    }

    [Test]
    public async Task SessionWithoutConnection_UsesFallbackTTL()
    {
        // Arrange
        var session = await CreateTestSessionAsync();
        var storage = GetSessionStorage();

        // Проверяем что сессия без SignalR соединения имеет нормальный TTL
        var currentSession = await storage.GetAsync(session.Id);
        Assert.That(currentSession, Is.Not.Null);
        Assert.That(currentSession!.ExpiresAt, Is.Not.EqualTo(DateTime.MaxValue),
            "Session without SignalR connection should use fallback TTL");
        Assert.That(currentSession.ExpiresAt, Is.GreaterThan(DateTime.UtcNow),
            "TTL should be in the future");
    }

    #region Helper Methods

    /// <summary>
    /// Создаёт тестовую сессию в хранилище.
    /// </summary>
    private async Task<LogSession> CreateTestSessionAsync()
    {
        var storage = GetSessionStorage();
        var session = new LogSession
        {
            Id = Guid.NewGuid(),
            FileName = $"test-{Guid.NewGuid():N}.log",
            FilePath = Path.Combine(TestTempDirectory, $"test-{Guid.NewGuid():N}.log"),
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        await storage.SaveAsync(session);
        return session;
    }

    /// <summary>
    /// Получает ISessionStorage из DI контейнера приложения.
    /// </summary>
    private ISessionStorage GetSessionStorage()
    {
        return Factory.Services.GetRequiredService<ISessionStorage>();
    }

    /// <summary>
    /// Создаёт SignalR HubConnection к LogWatcherHub.
    /// </summary>
    private async Task<HubConnection> CreateHubConnectionAsync()
    {
        var hubUrl = Client.BaseAddress + HubUrl.TrimStart('/');

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Используем HttpMessageHandler из Factory для тестов
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .Build();

        return await Task.FromResult(connection);
    }

    #endregion

    #region DTOs

    /// <summary>
    /// DTO для результата JoinSession (должен соответствовать JoinSessionResult в Hub).
    /// </summary>
    private class JoinSessionResultDto
    {
        public bool Success { get; set; }
        public string? SessionId { get; set; }
        public string? FileName { get; set; }
        public string? Error { get; set; }
    }

    #endregion
}
