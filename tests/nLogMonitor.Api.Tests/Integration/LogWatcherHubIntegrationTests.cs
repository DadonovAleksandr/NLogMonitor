using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using nLogMonitor.Application.Interfaces;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

/// <summary>
/// Интеграционные тесты для LogWatcherHub.
/// Проверяют работу SignalR соединений, привязку к сессиям и lifecycle management.
/// </summary>
[TestFixture]
public class LogWatcherHubIntegrationTests : WebApplicationTestBase
{
    private HubConnection? _hubConnection;

    [TearDown]
    public async Task TearDown()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    /// <summary>
    /// Создаёт SignalR HubConnection для тестов.
    /// </summary>
    private HubConnection CreateHubConnection()
    {
        var hubUrl = $"{Client.BaseAddress}hubs/logwatcher";

        return new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .Build();
    }

    /// <summary>
    /// Создаёт тестовую сессию с логами для использования в тестах Hub.
    /// </summary>
    private async Task<Guid> CreateTestSessionAsync()
    {
        // Создаём временный лог-файл
        var logContent = @"2024-01-15 10:30:45.1234|INFO|Test message 1|TestLogger|1234|1
2024-01-15 10:30:46.5678|ERROR|Test error|TestLogger|1234|1";

        var logFileName = $"{Guid.NewGuid()}.log";
        var logFilePath = Path.Combine(TestTempDirectory, logFileName);
        await File.WriteAllTextAsync(logFilePath, logContent);

        // Загружаем файл через API для создания сессии
        using var fileStream = File.OpenRead(logFilePath);
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", logFileName);

        var response = await Client.PostAsync("/api/upload", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = System.Text.Json.JsonSerializer.Deserialize<UploadResponse>(
            responseContent,
            DefaultJsonOptions);

        result.Should().NotBeNull();
        result!.SessionId.Should().NotBeEmpty();

        return Guid.Parse(result.SessionId);
    }

    [Test]
    public async Task JoinSession_WithValidSessionId_ShouldSucceed()
    {
        // Arrange
        var sessionId = await CreateTestSessionAsync();
        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        // Act
        var result = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId.ToString());
        result.FileName.Should().NotBeNullOrEmpty();
        result.Error.Should().BeNull();
    }

    [Test]
    public async Task JoinSession_WithInvalidSessionId_ShouldReturnError()
    {
        // Arrange
        var invalidSessionId = Guid.NewGuid();
        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        // Act
        var result = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            invalidSessionId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Session not found");
        result.SessionId.Should().BeNull();
    }

    [Test]
    public async Task JoinSession_WithInvalidFormat_ShouldReturnError()
    {
        // Arrange
        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        // Act
        var result = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            "not-a-guid");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid session ID format");
    }

    [Test]
    public async Task LeaveSession_ShouldRemoveSessionAndUnbindConnection()
    {
        // Arrange
        var sessionId = await CreateTestSessionAsync();
        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        // Join session first
        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - Leave session
        await _hubConnection.InvokeAsync("LeaveSession", sessionId.ToString());

        // Assert - Session should be deleted
        var response = await Client.GetAsync($"/api/logs/{sessionId}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task OnDisconnectedAsync_ShouldRemoveSessionAutomatically()
    {
        // Arrange
        var sessionId = await CreateTestSessionAsync();
        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        // Join session
        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - Disconnect (simulate closing browser tab)
        await _hubConnection.StopAsync();
        await _hubConnection.DisposeAsync();
        _hubConnection = null;

        // Wait a bit for cleanup to complete
        await Task.Delay(500);

        // Assert - Session should be deleted
        var response = await Client.GetAsync($"/api/logs/{sessionId}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task MultipleClients_CanJoinSameSession()
    {
        // Arrange
        var sessionId = await CreateTestSessionAsync();
        var connection1 = CreateHubConnection();
        var connection2 = CreateHubConnection();

        try
        {
            await connection1.StartAsync();
            await connection2.StartAsync();

            // Act - Both clients join the same session
            var result1 = await connection1.InvokeAsync<JoinSessionResult>(
                "JoinSession",
                sessionId.ToString());

            var result2 = await connection2.InvokeAsync<JoinSessionResult>(
                "JoinSession",
                sessionId.ToString());

            // Assert
            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            result1.SessionId.Should().Be(sessionId.ToString());
            result2.SessionId.Should().Be(sessionId.ToString());
        }
        finally
        {
            await connection1.DisposeAsync();
            await connection2.DisposeAsync();
        }
    }

    [Test]
    public async Task JoinSession_ShouldBindConnectionToSession()
    {
        // Arrange
        var sessionId = await CreateTestSessionAsync();
        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        // Act
        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());

        // Assert - Session should still exist while connection is active
        joinResult.Success.Should().BeTrue();

        var response = await Client.GetAsync($"/api/logs/{sessionId}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Test]
    public async Task ConnectionLifecycle_ShouldWorkCorrectly()
    {
        // Arrange
        var sessionId = await CreateTestSessionAsync();
        _hubConnection = CreateHubConnection();

        // Act & Assert - Connect
        await _hubConnection.StartAsync();
        _hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Join session
        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Verify session exists
        var getResponse = await Client.GetAsync($"/api/logs/{sessionId}");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Leave session
        await _hubConnection.InvokeAsync("LeaveSession", sessionId.ToString());

        // Verify session is deleted
        var checkResponse = await Client.GetAsync($"/api/logs/{sessionId}");
        checkResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        // Disconnect
        await _hubConnection.StopAsync();
        _hubConnection.State.Should().Be(HubConnectionState.Disconnected);
    }

    /// <summary>
    /// DTO для десериализации ответа от /api/upload.
    /// </summary>
    private class UploadResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int TotalEntries { get; set; }
    }
}

/// <summary>
/// DTO для результата JoinSession (должен совпадать с Hubs/LogWatcherHub.cs).
/// </summary>
public class JoinSessionResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? FileName { get; set; }
    public string? Error { get; set; }
}
