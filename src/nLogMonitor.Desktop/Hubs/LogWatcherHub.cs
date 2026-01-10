using Microsoft.AspNetCore.SignalR;
using nLogMonitor.Application.Interfaces;

namespace nLogMonitor.Desktop.Hubs;

/// <summary>
/// SignalR Hub для real-time обновлений логов.
/// Управляет подключениями клиентов, группами сессий и отправкой новых записей.
/// </summary>
public class LogWatcherHub : Hub
{
    private readonly ISessionStorage _sessionStorage;
    private readonly ILogger<LogWatcherHub> _logger;

    /// <summary>
    /// Создаёт новый экземпляр LogWatcherHub.
    /// </summary>
    /// <param name="sessionStorage">Хранилище сессий для управления привязкой connectionId к sessionId.</param>
    /// <param name="logger">Логгер для диагностики.</param>
    public LogWatcherHub(
        ISessionStorage sessionStorage,
        ILogger<LogWatcherHub> logger)
    {
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    /// <summary>
    /// Добавляет клиента в группу сессии и привязывает connectionId к sessionId.
    /// После вызова этого метода клиент будет получать все обновления для указанной сессии.
    /// </summary>
    /// <param name="sessionId">ID сессии логов.</param>
    /// <returns>Результат операции: success = true если сессия существует, иначе false с сообщением об ошибке.</returns>
    public async Task<JoinSessionResult> JoinSession(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            _logger.LogWarning(
                "Invalid sessionId format: {SessionId} from connection {ConnectionId}",
                sessionId,
                Context.ConnectionId);

            return new JoinSessionResult
            {
                Success = false,
                Error = "Invalid session ID format"
            };
        }

        // Проверяем существование сессии
        var session = await _sessionStorage.GetAsync(sessionGuid);
        if (session == null)
        {
            _logger.LogWarning(
                "Session {SessionId} not found for connection {ConnectionId}",
                sessionGuid,
                Context.ConnectionId);

            return new JoinSessionResult
            {
                Success = false,
                Error = "Session not found"
            };
        }

        // Привязываем connectionId к sessionId
        await _sessionStorage.BindConnectionAsync(Context.ConnectionId, sessionGuid);

        // Добавляем клиента в группу сессии для отправки broadcast сообщений
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        _logger.LogInformation(
            "Connection {ConnectionId} joined session {SessionId} (file: {FileName})",
            Context.ConnectionId,
            sessionGuid,
            session.FileName);

        return new JoinSessionResult
        {
            Success = true,
            SessionId = sessionGuid.ToString(),
            FileName = session.FileName
        };
    }

    /// <summary>
    /// Удаляет клиента из группы сессии и отвязывает connectionId от sessionId.
    /// После вызова этого метода клиент больше не будет получать обновления для указанной сессии.
    /// Сессия удаляется из хранилища только если это был последний подключённый клиент (multi-client support).
    /// </summary>
    /// <param name="sessionId">ID сессии логов.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task LeaveSession(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            _logger.LogWarning(
                "Invalid sessionId format: {SessionId} from connection {ConnectionId}",
                sessionId,
                Context.ConnectionId);
            return;
        }

        // Удаляем клиента из группы
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

        // Отвязываем connectionId (сессия удаляется только если это последний клиент)
        await _sessionStorage.UnbindConnectionAsync(Context.ConnectionId);

        _logger.LogInformation(
            "Connection {ConnectionId} left session {SessionId}",
            Context.ConnectionId,
            sessionGuid);
    }

    /// <summary>
    /// Вызывается при разрыве соединения (закрытие вкладки, потеря сети).
    /// Автоматически отвязывает connectionId от сессии.
    /// Сессия удаляется только если это был последний подключённый клиент (multi-client support).
    /// </summary>
    /// <param name="exception">Исключение, если разрыв был вызван ошибкой.</param>
    /// <returns>Асинхронная задача.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Получаем sessionId по connectionId
        var sessionId = await _sessionStorage.GetSessionByConnectionAsync(Context.ConnectionId);

        if (sessionId.HasValue)
        {
            // Отвязываем connectionId (сессия удаляется только если это последний клиент)
            await _sessionStorage.UnbindConnectionAsync(Context.ConnectionId);

            if (exception != null)
            {
                _logger.LogWarning(
                    exception,
                    "Connection {ConnectionId} disconnected with error from session {SessionId}",
                    Context.ConnectionId,
                    sessionId.Value);
            }
            else
            {
                _logger.LogInformation(
                    "Connection {ConnectionId} disconnected from session {SessionId}",
                    Context.ConnectionId,
                    sessionId.Value);
            }
        }
        else
        {
            _logger.LogDebug(
                "Connection {ConnectionId} disconnected without active session",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Методы для отправки событий клиентам (будут вызываться FileWatcherService в Фазе 6.1)

    /// <summary>
    /// Отправляет новые записи логов всем подписчикам группы сессии.
    /// Вызывается FileWatcherService при обнаружении изменений в файле.
    /// </summary>
    /// <param name="sessionId">ID сессии логов.</param>
    /// <param name="newLogs">Новые записи логов в формате JSON.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SendNewLogs(string sessionId, object newLogs)
    {
        await Clients.Group(sessionId).SendAsync("NewLogs", newLogs);

        _logger.LogDebug(
            "Sent new logs to session {SessionId} group",
            sessionId);
    }
}

/// <summary>
/// Результат операции JoinSession.
/// </summary>
public class JoinSessionResult
{
    /// <summary>
    /// Успешность операции.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID сессии (если успешно).
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Имя файла (если успешно).
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Сообщение об ошибке (если неуспешно).
    /// </summary>
    public string? Error { get; set; }
}
