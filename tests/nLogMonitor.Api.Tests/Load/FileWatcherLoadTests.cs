using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using nLogMonitor.Api.Tests.Integration;
using nLogMonitor.Application.DTOs;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Load;

/// <summary>
/// Нагрузочные тесты для FileWatcher + SignalR интеграции.
/// Проверяют производительность и стабильность при высокой нагрузке.
/// </summary>
/// <remarks>
/// Эти тесты запускаются вручную через [Explicit] атрибут.
/// Для запуска: dotnet test --filter "FullyQualifiedName~FileWatcherLoadTests"
/// </remarks>
[TestFixture]
[Explicit("Нагрузочные тесты запускаются вручную")]
public class FileWatcherLoadTests : DesktopModeTestBase
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
    private async Task<string> CreateTestLogFileAsync(string fileName, string content)
    {
        var filePath = Path.Combine(TestTempDirectory, fileName);
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Добавляет новые строки в конец файла.
    /// </summary>
    private async Task AppendToFileAsync(string filePath, string content)
    {
        await File.AppendAllTextAsync(filePath, content);
    }

    [Test]
    [Explicit("Нагрузочный тест - запускать вручную")]
    public async Task HighFrequencyChanges_ShouldHandleCorrectly()
    {
        // Arrange
        var initialContent = @"2024-01-15 10:30:45.1234|INFO|Initial log|TestLogger|1234|1
";
        var logFilePath = await CreateTestLogFileAsync("high-frequency-test.log", initialContent);

        var openRequest = new { FilePath = logFilePath };
        var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
        response.EnsureSuccessStatusCode();

        var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
        var sessionId = openResult!.SessionId;

        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        var updateCount = 0;
        var totalLogsReceived = 0;

        _hubConnection.On<List<LogEntryDto>>("NewLogs", logs =>
        {
            updateCount++;
            totalLogsReceived += logs.Count;
        });

        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - 20 изменений в секунду (50ms интервал)
        var stopwatch = Stopwatch.StartNew();
        var changesCount = 20;

        for (int i = 0; i < changesCount; i++)
        {
            var timestamp = DateTime.Now.AddSeconds(i);
            var logEntry = $"{timestamp:yyyy-MM-dd HH:mm:ss.ffff}|INFO|High frequency log {i}|TestLogger|1234|1\n";
            await AppendToFileAsync(logFilePath, logEntry);
            await Task.Delay(50); // 50ms между изменениями = 20 изменений в секунду
        }

        stopwatch.Stop();

        // Ждём завершения обработки (debounce 200ms + обработка)
        await Task.Delay(1500);

        // Assert
        Console.WriteLine($"Выполнено изменений: {changesCount}");
        Console.WriteLine($"Получено обновлений SignalR: {updateCount}");
        Console.WriteLine($"Всего записей получено: {totalLogsReceived}");
        Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds}ms");

        // Debounce должен уменьшить количество событий
        updateCount.Should().BeLessThan(changesCount,
            "Debounce должен объединить несколько быстрых изменений в одно событие");

        updateCount.Should().BeGreaterThan(0,
            "Должно быть хотя бы одно обновление");

        // Проверяем, что не было потери данных
        totalLogsReceived.Should().BeGreaterThanOrEqualTo(changesCount,
            "Все новые записи должны быть доставлены клиенту");
    }

    [Test]
    [Explicit("Нагрузочный тест - запускать вручную")]
    public async Task LargeNumberOfSessions_ShouldNotDegrade()
    {
        // Arrange - создаём 10 сессий с отдельными файлами
        var sessionCount = 10;
        var sessions = new List<SessionContext>();

        try
        {
            for (int i = 0; i < sessionCount; i++)
            {
                var content = $"2024-01-15 10:30:45.1234|INFO|Initial log for session {i}|TestLogger|1234|1\n";
                var logFilePath = await CreateTestLogFileAsync($"session-{i}.log", content);

                var openRequest = new { FilePath = logFilePath };
                var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
                response.EnsureSuccessStatusCode();

                var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
                var sessionId = openResult!.SessionId;

                var connection = CreateHubConnection();
                await connection.StartAsync();

                var updateCount = 0;
                connection.On<List<LogEntryDto>>("NewLogs", logs =>
                {
                    updateCount++;
                });

                var joinResult = await connection.InvokeAsync<JoinSessionResult>(
                    "JoinSession",
                    sessionId.ToString());
                joinResult.Success.Should().BeTrue();

                sessions.Add(new SessionContext
                {
                    SessionId = sessionId,
                    FilePath = logFilePath,
                    Connection = connection,
                    UpdateCount = () => updateCount
                });
            }

            // Act - обновляем все файлы одновременно
            var stopwatch = Stopwatch.StartNew();

            var updateTasks = sessions.Select(async session =>
            {
                var newContent = $"2024-01-15 10:30:46.5678|ERROR|Error in session|TestLogger|1234|1\n";
                await AppendToFileAsync(session.FilePath, newContent);
            });

            await Task.WhenAll(updateTasks);
            stopwatch.Stop();

            // Ждём обработки всех обновлений
            await Task.Delay(3000);

            // Assert
            Console.WriteLine($"Количество активных сессий: {sessionCount}");
            Console.WriteLine($"Время обновления всех файлов: {stopwatch.ElapsedMilliseconds}ms");

            foreach (var session in sessions.Select((s, i) => new { Session = s, Index = i }))
            {
                var updates = session.Session.UpdateCount();
                Console.WriteLine($"Сессия {session.Index}: получено {updates} обновлений");

                updates.Should().BeGreaterThan(0,
                    $"Сессия {session.Index} должна получить хотя бы одно обновление");
            }

            // Проверяем, что все сессии получили обновления
            sessions.Should().AllSatisfy(s =>
                s.UpdateCount().Should().BeGreaterThan(0, "Каждая сессия должна получить обновления"));
        }
        finally
        {
            // Cleanup
            foreach (var session in sessions)
            {
                await session.Connection.DisposeAsync();
            }
        }
    }

    [Test]
    [Explicit("Нагрузочный тест - запускать вручную")]
    public async Task LargeFileChanges_ShouldHandleEfficiently()
    {
        // Arrange
        var initialContent = @"2024-01-15 10:30:45.1234|INFO|Initial log|TestLogger|1234|1
";
        var logFilePath = await CreateTestLogFileAsync("large-file-test.log", initialContent);

        var openRequest = new { FilePath = logFilePath };
        var response = await Client.PostAsJsonAsync("/api/files/open", openRequest);
        response.EnsureSuccessStatusCode();

        var openResult = await response.Content.ReadFromJsonAsync<OpenFileResultDto>(DefaultJsonOptions);
        var sessionId = openResult!.SessionId;

        _hubConnection = CreateHubConnection();
        await _hubConnection.StartAsync();

        var updateCount = 0;
        var totalLogsReceived = 0;
        var updateStopwatch = new Stopwatch();

        _hubConnection.On<List<LogEntryDto>>("NewLogs", logs =>
        {
            updateCount++;
            totalLogsReceived += logs.Count;
            updateStopwatch.Stop();
        });

        var joinResult = await _hubConnection.InvokeAsync<JoinSessionResult>(
            "JoinSession",
            sessionId.ToString());
        joinResult.Success.Should().BeTrue();

        // Act - добавляем 1000 записей за раз
        var largeContent = new StringBuilder();
        var entryCount = 1000;

        for (int i = 0; i < entryCount; i++)
        {
            var timestamp = DateTime.Now.AddSeconds(i);
            largeContent.AppendLine(
                $"{timestamp:yyyy-MM-dd HH:mm:ss.ffff}|INFO|Large batch entry {i}|TestLogger|1234|1");
        }

        updateStopwatch.Start();
        await AppendToFileAsync(logFilePath, largeContent.ToString());

        // Ждём обработки
        await Task.Delay(5000);

        // Assert
        Console.WriteLine($"Добавлено записей: {entryCount}");
        Console.WriteLine($"Получено обновлений SignalR: {updateCount}");
        Console.WriteLine($"Всего записей получено: {totalLogsReceived}");
        Console.WriteLine($"Время от изменения до получения: {updateStopwatch.ElapsedMilliseconds}ms");

        updateCount.Should().BeGreaterThan(0, "Должно быть хотя бы одно обновление");

        totalLogsReceived.Should().BeGreaterThanOrEqualTo(entryCount,
            "Все новые записи должны быть доставлены");

        // Проверяем производительность - обновление должно прийти в разумное время
        updateStopwatch.ElapsedMilliseconds.Should().BeLessThan(10000,
            "Обновление должно прийти в течение 10 секунд");
    }

    /// <summary>
    /// Контекст тестовой сессии для нагрузочных тестов.
    /// </summary>
    private class SessionContext
    {
        public Guid SessionId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public HubConnection Connection { get; set; } = null!;
        public Func<int> UpdateCount { get; set; } = null!;
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
