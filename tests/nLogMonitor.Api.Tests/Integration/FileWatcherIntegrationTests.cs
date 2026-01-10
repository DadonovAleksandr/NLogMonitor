using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.DTOs;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

/// <summary>
/// Интеграционные тесты для FileWatcherService + SignalR интеграции.
/// Проверяют полный цикл: изменение файла → FileWatcher → SignalR → клиенты.
/// </summary>
[TestFixture]
public class FileWatcherIntegrationTests : DesktopModeTestBase
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
    /// Создаёт тестовый лог-файл в изолированной директории.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <param name="content">Содержимое файла.</param>
    /// <returns>Полный путь к созданному файлу.</returns>
    private async Task<string> CreateTestLogFileAsync(string fileName, string content)
    {
        var filePath = Path.Combine(TestTempDirectory, fileName);
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Добавляет новые строки в конец файла (симуляция записи логов).
    /// </summary>
    private async Task AppendToFileAsync(string filePath, string content)
    {
        await File.AppendAllTextAsync(filePath, content);
    }

    [Test]
    public async Task FileChange_ShouldTriggerSignalRUpdate()
    {
        // Arrange - создаём файл с начальными логами
        var initialContent = @"2024-01-15 10:30:45.1234|INFO|Initial log entry|TestLogger|1234|1
";
        var logFilePath = await CreateTestLogFileAsync("test.log", initialContent);

        // Открываем файл через API (Desktop mode)
        var openRequest = new { FilePath = logFilePath };
        var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
        response.EnsureSuccessStatusCode();

        var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
        openResult.Should().NotBeNull();
        var sessionId = openResult!.SessionId;

        // Подключаемся к SignalR Hub
        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        // Подписываемся на события NewLogs
        var receivedLogs = new List<LogEntryDto>();
        var logsReceivedEvent = new TaskCompletionSource<bool>();

        _hubConnection.On<List<LogEntryDto>>("NewLogs", logs =>
        {
            receivedLogs.AddRange(logs);
            logsReceivedEvent.TrySetResult(true);
        });

        // Присоединяемся к сессии
        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - добавляем новые записи в файл (симуляция записи логов)
        var newContent = @"2024-01-15 10:30:46.5678|ERROR|Error occurred|TestLogger|1234|1
2024-01-15 10:30:47.0000|WARN|Warning message|TestLogger|1234|1
";
        await AppendToFileAsync(logFilePath, newContent);

        // Ждём получения обновлений через SignalR (максимум 5 секунд)
        var receivedInTime = await Task.WhenAny(
            logsReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == logsReceivedEvent.Task;

        // Assert
        receivedInTime.Should().BeTrue("SignalR должен отправить обновления в течение 5 секунд");
        receivedLogs.Should().NotBeEmpty("Должны быть получены новые записи логов");

        // Проверяем, что получены все записи (включая начальные)
        // FileWatcher отправляет все записи файла, так как нет отслеживания позиции чтения
        receivedLogs.Should().HaveCountGreaterThanOrEqualTo(3, "Должны быть получены минимум 3 записи");
        receivedLogs.Should().Contain(l => l.Level == "Error" && l.Message == "Error occurred");
        receivedLogs.Should().Contain(l => l.Level == "Warn" && l.Message == "Warning message");
    }

    [Test]
    public async Task MultipleChanges_ShouldDebounce()
    {
        // Arrange
        var initialContent = @"2024-01-15 10:30:45.1234|INFO|Initial log|TestLogger|1234|1
";
        var logFilePath = await CreateTestLogFileAsync("debounce-test.log", initialContent);

        var openRequest = new { FilePath = logFilePath };
        var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
        response.EnsureSuccessStatusCode();

        var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
        var sessionId = openResult!.SessionId;

        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        var updateCount = 0;
        var updatesReceivedEvent = new TaskCompletionSource<bool>();

        _hubConnection.On<List<LogEntryDto>>("NewLogs", logs =>
        {
            updateCount++;
            if (updateCount >= 1) // Ожидаем минимум одно обновление после debounce
            {
                updatesReceivedEvent.TrySetResult(true);
            }
        });

        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - быстрые изменения файла (должны быть debounced до одного события)
        for (int i = 0; i < 5; i++)
        {
            await AppendToFileAsync(logFilePath, $"2024-01-15 10:30:{46 + i}.0000|INFO|Log {i}|TestLogger|1234|1\n");
            await Task.Delay(50); // Быстрые изменения (быстрее чем debounce 200ms)
        }

        // Ждём получения обновлений
        var receivedInTime = await Task.WhenAny(
            updatesReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(3))) == updatesReceivedEvent.Task;

        // Assert
        receivedInTime.Should().BeTrue("Должно прийти обновление после debounce");

        // Ждём дополнительное время для потенциальных дублирующих событий
        await Task.Delay(1000);

        // Из-за debounce 200ms должно быть значительно меньше событий чем изменений
        // Но точное количество предсказать сложно из-за тайминга
        updateCount.Should().BeLessThan(5, "Debounce должен уменьшить количество событий");
    }

    [Test]
    public async Task SessionDisconnect_ShouldStopWatching()
    {
        // Arrange
        var initialContent = @"2024-01-15 10:30:45.1234|INFO|Initial log|TestLogger|1234|1
";
        var logFilePath = await CreateTestLogFileAsync("disconnect-test.log", initialContent);

        var openRequest = new { FilePath = logFilePath };
        var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
        response.EnsureSuccessStatusCode();

        var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
        var sessionId = openResult!.SessionId;

        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - отключаемся (симуляция закрытия вкладки)
        await _hubConnection.StopAsync();
        await _hubConnection.DisposeAsync();
        _hubConnection = null;

        // Ждём обработки disconnect
        await Task.Delay(1000);

        // Assert - сессия должна быть удалена
        var checkResponse = await Client.GetAsync($"/api/logs/{sessionId}");
        checkResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound,
            "Сессия должна быть удалена после disconnect");
    }

    [Test]
    public async Task MultipleClients_ShouldReceiveSameUpdates()
    {
        // Arrange
        var initialContent = @"2024-01-15 10:30:45.1234|INFO|Initial log|TestLogger|1234|1
";
        var logFilePath = await CreateTestLogFileAsync("multi-client-test.log", initialContent);

        var openRequest = new { FilePath = logFilePath };
        var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
        response.EnsureSuccessStatusCode();

        var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
        var sessionId = openResult!.SessionId;

        // Создаём два клиента
        var connection1 = CreateHubConnection();
        var connection2 = CreateHubConnection();

        try
        {
            await connection1.StartAsync();
            await connection2.StartAsync();

            var receivedLogs1 = new List<LogEntryDto>();
            var receivedLogs2 = new List<LogEntryDto>();

            var client1Event = new TaskCompletionSource<bool>();
            var client2Event = new TaskCompletionSource<bool>();

            connection1.On<List<LogEntryDto>>("NewLogs", logs =>
            {
                receivedLogs1.AddRange(logs);
                client1Event.TrySetResult(true);
            });

            connection2.On<List<LogEntryDto>>("NewLogs", logs =>
            {
                receivedLogs2.AddRange(logs);
                client2Event.TrySetResult(true);
            });

            // Оба клиента присоединяются к одной сессии
            var joinResult1 = await connection1.InvokeAsync<JoinSessionResult>(
                "JoinSession",
                sessionId.ToString());
            joinResult1.Success.Should().BeTrue();

            var joinResult2 = await connection2.InvokeAsync<JoinSessionResult>(
                "JoinSession",
                sessionId.ToString());
            joinResult2.Success.Should().BeTrue();

            // Act - изменяем файл
            var newContent = @"2024-01-15 10:30:46.5678|ERROR|Shared error|TestLogger|1234|1
";
            await AppendToFileAsync(logFilePath, newContent);

            // Ждём обновления для обоих клиентов
            var client1Received = await Task.WhenAny(
                client1Event.Task,
                Task.Delay(TimeSpan.FromSeconds(5))) == client1Event.Task;

            var client2Received = await Task.WhenAny(
                client2Event.Task,
                Task.Delay(TimeSpan.FromSeconds(5))) == client2Event.Task;

            // Assert
            client1Received.Should().BeTrue("Клиент 1 должен получить обновление");
            client2Received.Should().BeTrue("Клиент 2 должен получить обновление");

            receivedLogs1.Should().NotBeEmpty();
            receivedLogs2.Should().NotBeEmpty();

            // Оба клиента должны получить одинаковые данные
            receivedLogs1.Should().Contain(l => l.Level == "Error" && l.Message == "Shared error");
            receivedLogs2.Should().Contain(l => l.Level == "Error" && l.Message == "Shared error");
        }
        finally
        {
            await connection1.DisposeAsync();
            await connection2.DisposeAsync();
        }
    }

    [Test]
    public async Task StopWatching_ShouldStopFileMonitoring()
    {
        // Arrange
        var initialContent = @"2024-01-15 10:30:45.1234|INFO|Initial log|TestLogger|1234|1
";
        var logFilePath = await CreateTestLogFileAsync("stop-watch-test.log", initialContent);

        var openRequest = new { FilePath = logFilePath };
        var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
        response.EnsureSuccessStatusCode();

        var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
        var sessionId = openResult!.SessionId;

        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        var receivedLogs = new List<LogEntryDto>();

        _hubConnection.On<List<LogEntryDto>>("NewLogs", logs =>
        {
            receivedLogs.AddRange(logs);
        });

        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - останавливаем мониторинг
        var stopResponse = await Client.PostAsync($"/api/files/{sessionId}/stop-watching", null);
        stopResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        // Изменяем файл после остановки мониторинга
        var newContent = @"2024-01-15 10:30:46.5678|ERROR|Should not be received|TestLogger|1234|1
";
        await AppendToFileAsync(logFilePath, newContent);

        // Ждём возможного обновления
        await Task.Delay(2000);

        // Assert - обновления не должны прийти
        receivedLogs.Should().BeEmpty("После остановки мониторинга обновления не должны приходить");
    }

    /// <summary>
    /// DTO для десериализации ответа от /api/files/open.
    /// </summary>
    private class OpenFileResultDto
    {
        public Guid SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int TotalEntries { get; set; }
        public Dictionary<string, int> LevelCounts { get; set; } = new();
    }
}
