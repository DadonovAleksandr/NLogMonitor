# NLogMonitor - План разработки Web-приложения

## Содержание
1. [Обзор проекта](#обзор-проекта)
2. [Архитектура системы](#архитектура-системы)
3. [Технологический стек](#технологический-стек)
4. [Структура проекта](#структура-проекта)
5. [Примеры кода](#примеры-кода)
6. [Фазы разработки](#фазы-разработки)
7. [Сравнение альтернатив](#сравнение-альтернатив)
8. [Чек-листы](#чек-листы)

---

## Обзор проекта

**NLogMonitor** — web-приложение для просмотра и анализа NLog-логов с возможностью загрузки файлов, фильтрации, поиска и экспорта.

### Ключевые возможности
- Загрузка лог-файлов через браузер
- Парсинг стандартного формата NLog
- Фильтрация по уровню логирования (Trace, Debug, Info, Warn, Error, Fatal)
- Полнотекстовый поиск по сообщениям
- Пагинация и виртуализация для больших файлов
- Экспорт в JSON/CSV
- Адаптивный интерфейс

---

## Архитектура системы

### Высокоуровневая архитектура

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              БРАУЗЕР                                     │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                     React/Vue SPA                                  │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐│  │
│  │  │  LogTable   │  │  Filters    │  │  Search     │  │  Export   ││  │
│  │  │  Component  │  │  Panel      │  │  Bar        │  │  Button   ││  │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────┬─────┘│  │
│  │         │                │                │               │       │  │
│  │  ┌──────┴────────────────┴────────────────┴───────────────┴─────┐│  │
│  │  │                    State Management                           ││  │
│  │  │              (Zustand / Redux / Pinia)                        ││  │
│  │  └───────────────────────────┬───────────────────────────────────┘│  │
│  └──────────────────────────────┼────────────────────────────────────┘  │
│                                 │ HTTP/REST                              │
└─────────────────────────────────┼────────────────────────────────────────┘
                                  │
┌─────────────────────────────────┼────────────────────────────────────────┐
│                      ASP.NET Core Web API                                │
│  ┌──────────────────────────────┴───────────────────────────────────┐   │
│  │                     API Controllers                               │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐               │   │
│  │  │  /api/logs  │  │ /api/upload │  │ /api/export │               │   │
│  │  │   GET/POST  │  │    POST     │  │    GET      │               │   │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘               │   │
│  └─────────┼────────────────┼────────────────┼───────────────────────┘   │
│            │                │                │                           │
│  ┌─────────┴────────────────┴────────────────┴───────────────────────┐   │
│  │                    Application Services                            │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐               │   │
│  │  │ LogParser   │  │ LogFilter   │  │ LogExporter │               │   │
│  │  │  Service    │  │  Service    │  │  Service    │               │   │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘               │   │
│  └─────────┼────────────────┼────────────────┼───────────────────────┘   │
│            │                │                │                           │
│  ┌─────────┴────────────────┴────────────────┴───────────────────────┐   │
│  │                         Domain                                     │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐               │   │
│  │  │  LogEntry   │  │  LogLevel   │  │ LogSession  │               │   │
│  │  │   Entity    │  │    Enum     │  │   Entity    │               │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘               │   │
│  └───────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  ┌───────────────────────────────────────────────────────────────────┐   │
│  │                     Infrastructure                                 │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐               │   │
│  │  │ FileStorage │  │ TempStorage │  │  Caching    │               │   │
│  │  │  (Upload)   │  │  (Session)  │  │ (In-Memory) │               │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘               │   │
│  └───────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────┘
```

### Слоистая архитектура (Clean Architecture)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                                                                          │
│    ┌─────────────────────────────────────────────────────────────────┐   │
│    │                      Presentation Layer                          │   │
│    │                                                                  │   │
│    │   ┌────────────────────┐    ┌────────────────────┐              │   │
│    │   │   React/Vue SPA    │    │  ASP.NET API       │              │   │
│    │   │   (Frontend)       │    │  Controllers       │              │   │
│    │   └────────────────────┘    └────────────────────┘              │   │
│    │                                                                  │   │
│    └──────────────────────────────┬──────────────────────────────────┘   │
│                                   │                                      │
│    ┌──────────────────────────────▼──────────────────────────────────┐   │
│    │                      Application Layer                           │   │
│    │                                                                  │   │
│    │   ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │   │
│    │   │  ILogParser    │  │  ILogFilter    │  │  ILogExporter  │    │   │
│    │   │  Interface     │  │  Interface     │  │  Interface     │    │   │
│    │   └────────────────┘  └────────────────┘  └────────────────┘    │   │
│    │                                                                  │   │
│    │   ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │   │
│    │   │  DTOs:         │  │  Commands:     │  │  Queries:      │    │   │
│    │   │  LogEntryDto   │  │  UploadLog     │  │  GetLogs       │    │   │
│    │   │  FilterDto     │  │  ExportLogs    │  │  SearchLogs    │    │   │
│    │   └────────────────┘  └────────────────┘  └────────────────┘    │   │
│    │                                                                  │   │
│    └──────────────────────────────┬──────────────────────────────────┘   │
│                                   │                                      │
│    ┌──────────────────────────────▼──────────────────────────────────┐   │
│    │                        Domain Layer                              │   │
│    │                                                                  │   │
│    │   ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │   │
│    │   │   LogEntry     │  │   LogLevel     │  │  LogSession    │    │   │
│    │   │                │  │                │  │                │    │   │
│    │   │  - DateTime    │  │  - Trace       │  │  - Id          │    │   │
│    │   │  - Level       │  │  - Debug       │  │  - FileName    │    │   │
│    │   │  - Message     │  │  - Info        │  │  - CreatedAt   │    │   │
│    │   │  - Logger      │  │  - Warn        │  │  - Entries[]   │    │   │
│    │   │  - ProcessId   │  │  - Error       │  │                │    │   │
│    │   │  - ThreadId    │  │  - Fatal       │  │                │    │   │
│    │   └────────────────┘  └────────────────┘  └────────────────┘    │   │
│    │                                                                  │   │
│    └──────────────────────────────┬──────────────────────────────────┘   │
│                                   │                                      │
│    ┌──────────────────────────────▼──────────────────────────────────┐   │
│    │                    Infrastructure Layer                          │   │
│    │                                                                  │   │
│    │   ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │   │
│    │   │  NLogParser    │  │  FileStorage   │  │  MemoryCache   │    │   │
│    │   │  (Regex-based) │  │  Service       │  │  Service       │    │   │
│    │   └────────────────┘  └────────────────┘  └────────────────┘    │   │
│    │                                                                  │   │
│    │   ┌────────────────┐  ┌────────────────┐                        │   │
│    │   │  JsonExporter  │  │  CsvExporter   │                        │   │
│    │   │                │  │                │                        │   │
│    │   └────────────────┘  └────────────────┘                        │   │
│    │                                                                  │   │
│    └─────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

### Поток данных (Data Flow)

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  User    │    │ Frontend │    │  API     │    │ Services │    │  Storage │
│  Browser │    │  React   │    │ ASP.NET  │    │  Layer   │    │  Temp    │
└────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
     │               │               │               │               │
     │  1. Select    │               │               │               │
     │     File      │               │               │               │
     │───────────────>               │               │               │
     │               │               │               │               │
     │               │ 2. POST       │               │               │
     │               │    /api/upload│               │               │
     │               │───────────────>               │               │
     │               │               │               │               │
     │               │               │ 3. Stream     │               │
     │               │               │    to Parser  │               │
     │               │               │───────────────>               │
     │               │               │               │               │
     │               │               │               │ 4. Parse &    │
     │               │               │               │    Store      │
     │               │               │               │───────────────>
     │               │               │               │               │
     │               │               │               │ 5. SessionId  │
     │               │               │<──────────────────────────────│
     │               │               │               │               │
     │               │ 6. SessionId  │               │               │
     │               │<───────────────               │               │
     │               │               │               │               │
     │               │ 7. GET        │               │               │
     │               │    /api/logs  │               │               │
     │               │───────────────>               │               │
     │               │               │               │               │
     │               │               │ 8. Filter &   │               │
     │               │               │    Paginate   │               │
     │               │               │───────────────>               │
     │               │               │               │               │
     │               │ 9. LogEntries │               │               │
     │               │<───────────────               │               │
     │               │               │               │               │
     │ 10. Display   │               │               │               │
     │     Table     │               │               │               │
     │<───────────────               │               │               │
     │               │               │               │               │
```

---

## Технологический стек

### Backend (ASP.NET Core)
| Компонент | Технология | Версия |
|-----------|------------|--------|
| Framework | ASP.NET Core | 9.0 |
| API Style | Minimal API + Controllers | - |
| DI Container | Microsoft.Extensions.DI | Built-in |
| Validation | FluentValidation | 11.x |
| Logging | NLog | 5.x |
| API Docs | Swagger/OpenAPI | Swashbuckle |
| Caching | IMemoryCache | Built-in |

### Frontend (React - рекомендуется)
| Компонент | Технология | Версия |
|-----------|------------|--------|
| Framework | React | 18.x |
| Language | TypeScript | 5.x |
| Build Tool | Vite | 5.x |
| State | Zustand | 4.x |
| UI Library | shadcn/ui | latest |
| HTTP Client | Axios | 1.x |
| Table | TanStack Table | 8.x |
| Icons | Lucide React | latest |

---

## Структура проекта

```
NLogMonitor/
├── src/
│   ├── NLogMonitor.Domain/           # Domain Layer
│   │   ├── Entities/
│   │   │   ├── LogEntry.cs
│   │   │   ├── LogSession.cs
│   │   │   └── LogLevel.cs
│   │   └── NLogMonitor.Domain.csproj
│   │
│   ├── NLogMonitor.Application/      # Application Layer
│   │   ├── Interfaces/
│   │   │   ├── ILogParser.cs
│   │   │   ├── ILogFilter.cs
│   │   │   ├── ILogExporter.cs
│   │   │   └── ISessionStorage.cs
│   │   ├── DTOs/
│   │   │   ├── LogEntryDto.cs
│   │   │   ├── FilterOptionsDto.cs
│   │   │   ├── PagedResultDto.cs
│   │   │   └── UploadResultDto.cs
│   │   ├── Services/
│   │   │   ├── LogService.cs
│   │   │   └── ExportService.cs
│   │   └── NLogMonitor.Application.csproj
│   │
│   ├── NLogMonitor.Infrastructure/   # Infrastructure Layer
│   │   ├── Parsing/
│   │   │   ├── NLogParser.cs
│   │   │   └── LogPatternRegistry.cs
│   │   ├── Storage/
│   │   │   ├── InMemorySessionStorage.cs
│   │   │   └── TempFileStorage.cs
│   │   ├── Export/
│   │   │   ├── JsonExporter.cs
│   │   │   └── CsvExporter.cs
│   │   ├── Caching/
│   │   │   └── LogCacheService.cs
│   │   └── NLogMonitor.Infrastructure.csproj
│   │
│   └── NLogMonitor.Api/              # Presentation Layer (API)
│       ├── Controllers/
│       │   ├── LogsController.cs
│       │   ├── UploadController.cs
│       │   └── ExportController.cs
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── NLogMonitor.Api.csproj
│
├── client/                            # Frontend (React)
│   ├── src/
│   │   ├── components/
│   │   │   ├── ui/                   # shadcn components
│   │   │   ├── LogTable/
│   │   │   │   ├── LogTable.tsx
│   │   │   │   ├── LogRow.tsx
│   │   │   │   └── columns.tsx
│   │   │   ├── FilterPanel/
│   │   │   │   ├── FilterPanel.tsx
│   │   │   │   └── LevelFilter.tsx
│   │   │   ├── SearchBar/
│   │   │   │   └── SearchBar.tsx
│   │   │   ├── FileUpload/
│   │   │   │   └── FileUpload.tsx
│   │   │   └── ExportButton/
│   │   │       └── ExportButton.tsx
│   │   ├── store/
│   │   │   ├── useLogStore.ts
│   │   │   └── useFilterStore.ts
│   │   ├── api/
│   │   │   ├── client.ts
│   │   │   └── logs.ts
│   │   ├── types/
│   │   │   └── index.ts
│   │   ├── hooks/
│   │   │   ├── useLogs.ts
│   │   │   └── useDebounce.ts
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts
│
├── tests/
│   ├── NLogMonitor.Domain.Tests/
│   ├── NLogMonitor.Application.Tests/
│   ├── NLogMonitor.Infrastructure.Tests/
│   └── NLogMonitor.Api.Tests/
│
├── NLogMonitor.sln
├── README.md
├── PLAN.md
└── .gitignore
```

---

## Примеры кода

### Domain Layer

#### LogEntry.cs
```csharp
namespace NLogMonitor.Domain.Entities;

public class LogEntry
{
    public int Id { get; init; }
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Logger { get; init; } = string.Empty;
    public int? ProcessId { get; init; }
    public int? ThreadId { get; init; }
    public string? Exception { get; init; }

    public bool MatchesSearch(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        return Message.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || Logger.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || (Exception?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
```

#### LogLevel.cs
```csharp
namespace NLogMonitor.Domain.Entities;

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
    Fatal = 5
}
```

#### LogSession.cs
```csharp
namespace NLogMonitor.Domain.Entities;

public class LogSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
    public List<LogEntry> Entries { get; init; } = [];
    public int TotalCount => Entries.Count;

    public Dictionary<LogLevel, int> GetLevelCounts()
    {
        return Entries
            .GroupBy(e => e.Level)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
```

### Application Layer

#### ILogParser.cs
```csharp
namespace NLogMonitor.Application.Interfaces;

public interface ILogParser
{
    IAsyncEnumerable<LogEntry> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default);

    bool CanParse(string firstLine);
}
```

#### LogEntryDto.cs
```csharp
namespace NLogMonitor.Application.DTOs;

public record LogEntryDto(
    int Id,
    DateTime Timestamp,
    string Level,
    string Message,
    string Logger,
    int? ProcessId,
    int? ThreadId,
    string? Exception
);

public record FilterOptionsDto(
    string? SearchText,
    LogLevel? MinLevel,
    LogLevel? MaxLevel,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Logger
);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record UploadResultDto(
    Guid SessionId,
    string FileName,
    int TotalEntries,
    Dictionary<string, int> LevelCounts
);
```

#### LogService.cs
```csharp
namespace NLogMonitor.Application.Services;

public class LogService : ILogService
{
    private readonly ISessionStorage _sessionStorage;
    private readonly ILogParser _parser;
    private readonly ILogger<LogService> _logger;

    public LogService(
        ISessionStorage sessionStorage,
        ILogParser parser,
        ILogger<LogService> logger)
    {
        _sessionStorage = sessionStorage;
        _parser = parser;
        _logger = logger;
    }

    public async Task<UploadResultDto> UploadAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var session = new LogSession
        {
            FileName = fileName,
            FileSize = stream.Length,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var entries = new List<LogEntry>();
        var id = 0;

        await foreach (var entry in _parser.ParseAsync(stream, cancellationToken))
        {
            entries.Add(entry with { Id = ++id });
        }

        session = session with { Entries = entries };
        await _sessionStorage.SaveAsync(session, cancellationToken);

        _logger.LogInformation(
            "Uploaded {FileName} with {Count} entries",
            fileName,
            entries.Count);

        return new UploadResultDto(
            session.Id,
            session.FileName,
            session.TotalCount,
            session.GetLevelCounts()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
        );
    }

    public async Task<PagedResultDto<LogEntryDto>> GetLogsAsync(
        Guid sessionId,
        FilterOptionsDto? filter,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionStorage.GetAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found");

        var query = session.Entries.AsEnumerable();

        // Apply filters
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
                query = query.Where(e => e.MatchesSearch(filter.SearchText));

            if (filter.MinLevel.HasValue)
                query = query.Where(e => e.Level >= filter.MinLevel.Value);

            if (filter.MaxLevel.HasValue)
                query = query.Where(e => e.Level <= filter.MaxLevel.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(e => e.Timestamp >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(e => e.Timestamp <= filter.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.Logger))
                query = query.Where(e =>
                    e.Logger.Contains(filter.Logger, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new LogEntryDto(
                e.Id,
                e.Timestamp,
                e.Level.ToString(),
                e.Message,
                e.Logger,
                e.ProcessId,
                e.ThreadId,
                e.Exception
            ))
            .ToList();

        return new PagedResultDto<LogEntryDto>(
            items,
            totalCount,
            page,
            pageSize,
            totalPages);
    }
}
```

### Infrastructure Layer

#### NLogParser.cs
```csharp
namespace NLogMonitor.Infrastructure.Parsing;

public partial class NLogParser : ILogParser
{
    // Standard NLog format regex
    // Format: 2024-01-15 10:30:45.1234|INFO|Message|LoggerName|1234|5678
    [GeneratedRegex(
        @"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{4})\s*\|\s*(\w+)\s*\|\s*(.*?)\s*\|\s*([.\w]+)\s*\|\s*(\d+)\s*\|\s*(\d+)?$",
        RegexOptions.Compiled)]
    private static partial Regex LogEntryPattern();

    private readonly ILogger<NLogParser> _logger;

    public NLogParser(ILogger<NLogParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string firstLine)
    {
        return LogEntryPattern().IsMatch(firstLine);
    }

    public async IAsyncEnumerable<LogEntry> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var lineNumber = 0;
        var multiLineMessage = new StringBuilder();
        LogEntry? currentEntry = null;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = LogEntryPattern().Match(line);

            if (match.Success)
            {
                // Yield previous entry if exists
                if (currentEntry is not null)
                {
                    if (multiLineMessage.Length > 0)
                    {
                        currentEntry = currentEntry with
                        {
                            Message = currentEntry.Message + Environment.NewLine +
                                      multiLineMessage.ToString()
                        };
                        multiLineMessage.Clear();
                    }
                    yield return currentEntry;
                }

                // Parse new entry
                currentEntry = ParseMatch(match, lineNumber);
            }
            else if (currentEntry is not null)
            {
                // Multi-line message continuation
                multiLineMessage.AppendLine(line);
            }
            else
            {
                _logger.LogWarning("Unparseable line {LineNumber}: {Line}",
                    lineNumber,
                    line.Length > 100 ? line[..100] + "..." : line);
            }
        }

        // Yield last entry
        if (currentEntry is not null)
        {
            if (multiLineMessage.Length > 0)
            {
                currentEntry = currentEntry with
                {
                    Message = currentEntry.Message + Environment.NewLine +
                              multiLineMessage.ToString()
                };
            }
            yield return currentEntry;
        }
    }

    private static LogEntry ParseMatch(Match match, int lineNumber)
    {
        return new LogEntry
        {
            Id = lineNumber,
            Timestamp = DateTime.Parse(match.Groups[1].Value),
            Level = Enum.Parse<LogLevel>(match.Groups[2].Value, ignoreCase: true),
            Message = match.Groups[3].Value.Trim(),
            Logger = match.Groups[4].Value,
            ProcessId = int.TryParse(match.Groups[5].Value, out var pid) ? pid : null,
            ThreadId = int.TryParse(match.Groups[6].Value, out var tid) ? tid : null
        };
    }
}
```

#### InMemorySessionStorage.cs
```csharp
namespace NLogMonitor.Infrastructure.Storage;

public class InMemorySessionStorage : ISessionStorage, IDisposable
{
    private readonly ConcurrentDictionary<Guid, LogSession> _sessions = new();
    private readonly Timer _cleanupTimer;
    private readonly ILogger<InMemorySessionStorage> _logger;

    public InMemorySessionStorage(ILogger<InMemorySessionStorage> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(
            CleanupExpiredSessions,
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    public Task SaveAsync(LogSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        _logger.LogDebug("Saved session {SessionId}", session.Id);
        return Task.CompletedTask;
    }

    public Task<LogSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    private void CleanupExpiredSessions(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredIds = _sessions
            .Where(kv => kv.Value.ExpiresAt < now)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var id in expiredIds)
        {
            _sessions.TryRemove(id, out _);
            _logger.LogInformation("Cleaned up expired session {SessionId}", id);
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}
```

### API Layer

#### LogsController.cs
```csharp
namespace NLogMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;

    public LogsController(ILogService logService)
    {
        _logService = logService;
    }

    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(PagedResultDto<LogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogs(
        Guid sessionId,
        [FromQuery] string? search,
        [FromQuery] LogLevel? minLevel,
        [FromQuery] LogLevel? maxLevel,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? logger,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var filter = new FilterOptionsDto(
            search, minLevel, maxLevel, fromDate, toDate, logger);

        try
        {
            var result = await _logService.GetLogsAsync(
                sessionId, filter, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Session {sessionId} not found");
        }
    }
}
```

#### UploadController.cs
```csharp
namespace NLogMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<UploadController> _logger;
    private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB

    public UploadController(ILogService logService, ILogger<UploadController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [DisableFormValueModelBinding]
    [ProducesResponseType(typeof(UploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
            return BadRequest("Expected multipart/form-data");

        var boundary = HeaderUtilities.RemoveQuotes(
            MediaTypeHeaderValue.Parse(Request.ContentType).Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
            return BadRequest("Missing boundary in Content-Type");

        var reader = new MultipartReader(boundary, Request.Body);
        var section = await reader.ReadNextSectionAsync(cancellationToken);

        while (section != null)
        {
            var contentDisposition = section.GetContentDispositionHeader();

            if (contentDisposition?.IsFileDisposition() == true)
            {
                var fileName = contentDisposition.FileName.Value ?? "unknown.log";

                _logger.LogInformation("Processing upload: {FileName}", fileName);

                var result = await _logService.UploadAsync(
                    section.Body,
                    fileName,
                    cancellationToken);

                return Ok(result);
            }

            section = await reader.ReadNextSectionAsync(cancellationToken);
        }

        return BadRequest("No file found in request");
    }
}
```

#### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Domain & Application
builder.Services.AddScoped<ILogService, LogService>();

// Infrastructure
builder.Services.AddSingleton<ILogParser, NLogParser>();
builder.Services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
builder.Services.AddScoped<ILogExporter, JsonExporter>();
builder.Services.AddScoped<ILogExporter, CsvExporter>();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Logging
builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Frontend (React + TypeScript)

#### types/index.ts
```typescript
export enum LogLevel {
  Trace = 0,
  Debug = 1,
  Info = 2,
  Warn = 3,
  Error = 4,
  Fatal = 5,
}

export interface LogEntry {
  id: number;
  timestamp: string;
  level: string;
  message: string;
  logger: string;
  processId?: number;
  threadId?: number;
  exception?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface UploadResult {
  sessionId: string;
  fileName: string;
  totalEntries: number;
  levelCounts: Record<string, number>;
}

export interface FilterOptions {
  searchText?: string;
  minLevel?: LogLevel;
  maxLevel?: LogLevel;
  fromDate?: string;
  toDate?: string;
  logger?: string;
}
```

#### store/useLogStore.ts
```typescript
import { create } from 'zustand';
import type { LogEntry, PagedResult, FilterOptions, UploadResult } from '@/types';

interface LogState {
  // State
  sessionId: string | null;
  fileName: string | null;
  logs: LogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  isLoading: boolean;
  error: string | null;
  filter: FilterOptions;
  levelCounts: Record<string, number>;

  // Actions
  setSession: (result: UploadResult) => void;
  setLogs: (result: PagedResult<LogEntry>) => void;
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
  setFilter: (filter: Partial<FilterOptions>) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
}

const initialState = {
  sessionId: null,
  fileName: null,
  logs: [],
  totalCount: 0,
  page: 1,
  pageSize: 100,
  totalPages: 0,
  isLoading: false,
  error: null,
  filter: {},
  levelCounts: {},
};

export const useLogStore = create<LogState>((set) => ({
  ...initialState,

  setSession: (result) =>
    set({
      sessionId: result.sessionId,
      fileName: result.fileName,
      totalCount: result.totalEntries,
      levelCounts: result.levelCounts,
      page: 1,
      error: null,
    }),

  setLogs: (result) =>
    set({
      logs: result.items,
      totalCount: result.totalCount,
      page: result.page,
      totalPages: result.totalPages,
    }),

  setPage: (page) => set({ page }),
  setPageSize: (pageSize) => set({ pageSize, page: 1 }),
  setFilter: (filter) => set((state) => ({
    filter: { ...state.filter, ...filter },
    page: 1
  })),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
  reset: () => set(initialState),
}));
```

#### api/logs.ts
```typescript
import axios from 'axios';
import type { PagedResult, LogEntry, UploadResult, FilterOptions } from '@/types';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
});

export async function uploadFile(file: File): Promise<UploadResult> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await client.post<UploadResult>('/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return response.data;
}

export async function getLogs(
  sessionId: string,
  filter: FilterOptions,
  page: number,
  pageSize: number
): Promise<PagedResult<LogEntry>> {
  const params = new URLSearchParams();

  if (filter.searchText) params.append('search', filter.searchText);
  if (filter.minLevel !== undefined) params.append('minLevel', filter.minLevel.toString());
  if (filter.maxLevel !== undefined) params.append('maxLevel', filter.maxLevel.toString());
  if (filter.fromDate) params.append('fromDate', filter.fromDate);
  if (filter.toDate) params.append('toDate', filter.toDate);
  if (filter.logger) params.append('logger', filter.logger);

  params.append('page', page.toString());
  params.append('pageSize', pageSize.toString());

  const response = await client.get<PagedResult<LogEntry>>(
    `/logs/${sessionId}?${params.toString()}`
  );

  return response.data;
}

export async function exportLogs(
  sessionId: string,
  format: 'json' | 'csv',
  filter?: FilterOptions
): Promise<Blob> {
  const params = new URLSearchParams();
  params.append('format', format);

  if (filter?.searchText) params.append('search', filter.searchText);
  // ... other filters

  const response = await client.get(`/export/${sessionId}?${params.toString()}`, {
    responseType: 'blob',
  });

  return response.data;
}
```

#### hooks/useLogs.ts
```typescript
import { useEffect } from 'react';
import { useLogStore } from '@/store/useLogStore';
import { getLogs } from '@/api/logs';
import { useDebounce } from './useDebounce';

export function useLogs() {
  const {
    sessionId,
    filter,
    page,
    pageSize,
    setLogs,
    setLoading,
    setError,
  } = useLogStore();

  const debouncedSearch = useDebounce(filter.searchText, 300);

  useEffect(() => {
    if (!sessionId) return;

    const fetchLogs = async () => {
      setLoading(true);
      setError(null);

      try {
        const result = await getLogs(
          sessionId,
          { ...filter, searchText: debouncedSearch },
          page,
          pageSize
        );
        setLogs(result);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch logs');
      } finally {
        setLoading(false);
      }
    };

    fetchLogs();
  }, [sessionId, debouncedSearch, filter.minLevel, filter.maxLevel, page, pageSize]);
}
```

#### components/LogTable/LogTable.tsx
```tsx
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  type ColumnDef,
} from '@tanstack/react-table';
import { useLogStore } from '@/store/useLogStore';
import { useLogs } from '@/hooks/useLogs';
import type { LogEntry } from '@/types';
import { cn } from '@/lib/utils';

const levelColors: Record<string, string> = {
  Trace: 'text-gray-400',
  Debug: 'text-gray-500',
  Info: 'text-blue-600',
  Warn: 'text-yellow-600',
  Error: 'text-red-600',
  Fatal: 'text-red-800 font-bold',
};

const columns: ColumnDef<LogEntry>[] = [
  {
    accessorKey: 'timestamp',
    header: 'Time',
    cell: ({ getValue }) => {
      const date = new Date(getValue<string>());
      return date.toLocaleTimeString('ru-RU', {
        hour12: false,
        fractionalSecondDigits: 3
      });
    },
    size: 120,
  },
  {
    accessorKey: 'level',
    header: 'Level',
    cell: ({ getValue }) => {
      const level = getValue<string>();
      return (
        <span className={cn('font-mono text-sm', levelColors[level])}>
          {level}
        </span>
      );
    },
    size: 80,
  },
  {
    accessorKey: 'message',
    header: 'Message',
    cell: ({ getValue }) => (
      <span className="font-mono text-sm truncate block max-w-[600px]">
        {getValue<string>()}
      </span>
    ),
  },
  {
    accessorKey: 'logger',
    header: 'Logger',
    size: 200,
  },
];

export function LogTable() {
  useLogs(); // Fetch logs on mount and filter changes

  const { logs, isLoading, error } = useLogStore();

  const table = useReactTable({
    data: logs,
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  if (error) {
    return (
      <div className="p-4 text-red-600 bg-red-50 rounded-lg">
        Error: {error}
      </div>
    );
  }

  return (
    <div className="relative overflow-auto border rounded-lg">
      {isLoading && (
        <div className="absolute inset-0 bg-white/50 flex items-center justify-center">
          <div className="animate-spin h-8 w-8 border-4 border-blue-500 border-t-transparent rounded-full" />
        </div>
      )}

      <table className="w-full text-sm">
        <thead className="bg-gray-50 sticky top-0">
          {table.getHeaderGroups().map((headerGroup) => (
            <tr key={headerGroup.id}>
              {headerGroup.headers.map((header) => (
                <th
                  key={header.id}
                  className="px-4 py-3 text-left font-medium text-gray-700 border-b"
                  style={{ width: header.getSize() }}
                >
                  {flexRender(
                    header.column.columnDef.header,
                    header.getContext()
                  )}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody>
          {table.getRowModel().rows.map((row) => (
            <tr key={row.id} className="hover:bg-gray-50 border-b">
              {row.getVisibleCells().map((cell) => (
                <td key={cell.id} className="px-4 py-2">
                  {flexRender(cell.column.columnDef.cell, cell.getContext())}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>

      {logs.length === 0 && !isLoading && (
        <div className="p-8 text-center text-gray-500">
          No log entries found
        </div>
      )}
    </div>
  );
}
```

#### components/FileUpload/FileUpload.tsx
```tsx
import { useCallback, useState } from 'react';
import { Upload, FileText, X } from 'lucide-react';
import { uploadFile } from '@/api/logs';
import { useLogStore } from '@/store/useLogStore';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

export function FileUpload() {
  const [isDragging, setIsDragging] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<number | null>(null);
  const { setSession, setLoading, setError, fileName, reset } = useLogStore();

  const handleFile = useCallback(async (file: File) => {
    if (!file.name.endsWith('.log') && !file.name.endsWith('.txt')) {
      setError('Please upload a .log or .txt file');
      return;
    }

    setLoading(true);
    setError(null);
    setUploadProgress(0);

    try {
      const result = await uploadFile(file);
      setSession(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setLoading(false);
      setUploadProgress(null);
    }
  }, [setSession, setLoading, setError]);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);

    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  }, [handleFile]);

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
  }, [handleFile]);

  if (fileName) {
    return (
      <div className="flex items-center gap-2 px-4 py-2 bg-green-50 rounded-lg">
        <FileText className="h-5 w-5 text-green-600" />
        <span className="text-green-800 font-medium">{fileName}</span>
        <Button variant="ghost" size="sm" onClick={reset}>
          <X className="h-4 w-4" />
        </Button>
      </div>
    );
  }

  return (
    <div
      className={cn(
        'relative border-2 border-dashed rounded-lg p-8 text-center transition-colors',
        isDragging ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'
      )}
      onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
      onDragLeave={() => setIsDragging(false)}
      onDrop={handleDrop}
    >
      <input
        type="file"
        accept=".log,.txt"
        onChange={handleChange}
        className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
      />

      <Upload className="mx-auto h-12 w-12 text-gray-400" />
      <p className="mt-4 text-lg font-medium text-gray-700">
        Drop log file here or click to browse
      </p>
      <p className="mt-1 text-sm text-gray-500">
        Supports .log and .txt files up to 100MB
      </p>

      {uploadProgress !== null && (
        <div className="mt-4 w-full bg-gray-200 rounded-full h-2">
          <div
            className="bg-blue-500 h-2 rounded-full transition-all"
            style={{ width: `${uploadProgress}%` }}
          />
        </div>
      )}
    </div>
  );
}
```

#### components/FilterPanel/FilterPanel.tsx
```tsx
import { useLogStore } from '@/store/useLogStore';
import { LogLevel } from '@/types';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

const levels = [
  { value: LogLevel.Trace, label: 'Trace', color: 'bg-gray-200' },
  { value: LogLevel.Debug, label: 'Debug', color: 'bg-gray-300' },
  { value: LogLevel.Info, label: 'Info', color: 'bg-blue-200' },
  { value: LogLevel.Warn, label: 'Warn', color: 'bg-yellow-200' },
  { value: LogLevel.Error, label: 'Error', color: 'bg-red-200' },
  { value: LogLevel.Fatal, label: 'Fatal', color: 'bg-red-400' },
];

export function FilterPanel() {
  const { filter, setFilter, levelCounts } = useLogStore();

  const handleLevelToggle = (level: LogLevel) => {
    const currentMin = filter.minLevel ?? LogLevel.Trace;

    if (level === currentMin) {
      // Reset to show all
      setFilter({ minLevel: undefined });
    } else {
      setFilter({ minLevel: level });
    }
  };

  return (
    <div className="flex flex-wrap gap-2">
      {levels.map(({ value, label, color }) => {
        const count = levelCounts[label] ?? 0;
        const isActive = (filter.minLevel ?? LogLevel.Trace) <= value;

        return (
          <Button
            key={value}
            variant="outline"
            size="sm"
            onClick={() => handleLevelToggle(value)}
            className={cn(
              'gap-2 transition-all',
              isActive ? color : 'opacity-50'
            )}
          >
            {label}
            <span className="text-xs bg-white/50 px-1.5 py-0.5 rounded">
              {count}
            </span>
          </Button>
        );
      })}
    </div>
  );
}
```

#### App.tsx
```tsx
import { FileUpload } from '@/components/FileUpload/FileUpload';
import { FilterPanel } from '@/components/FilterPanel/FilterPanel';
import { SearchBar } from '@/components/SearchBar/SearchBar';
import { LogTable } from '@/components/LogTable/LogTable';
import { ExportButton } from '@/components/ExportButton/ExportButton';
import { Pagination } from '@/components/Pagination/Pagination';
import { useLogStore } from '@/store/useLogStore';

export default function App() {
  const { sessionId, totalCount } = useLogStore();

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <h1 className="text-2xl font-bold text-gray-900">
            NLog Monitor
          </h1>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 py-6 space-y-6">
        {!sessionId ? (
          <FileUpload />
        ) : (
          <>
            <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
              <div className="flex-1 max-w-md">
                <SearchBar />
              </div>
              <div className="flex gap-2">
                <FileUpload />
                <ExportButton />
              </div>
            </div>

            <FilterPanel />

            <div className="bg-white rounded-lg shadow">
              <div className="px-4 py-3 border-b flex justify-between items-center">
                <span className="text-sm text-gray-600">
                  {totalCount.toLocaleString()} entries
                </span>
                <Pagination />
              </div>
              <LogTable />
            </div>
          </>
        )}
      </main>
    </div>
  );
}
```

---

## Фазы разработки

### Фаза 1: Базовая инфраструктура
**Цель:** Настроить проект и базовую архитектуру

1. Создание solution и проектов
2. Настройка DI и конфигурации
3. Базовые domain entities
4. Интерфейсы application layer
5. Swagger документация

### Фаза 2: Парсинг и хранение
**Цель:** Реализовать загрузку и парсинг логов

1. NLog parser с поддержкой стандартного формата
2. Multi-line message handling
3. In-memory session storage
4. Session expiration и cleanup
5. Unit tests для парсера

### Фаза 3: API endpoints
**Цель:** Реализовать REST API

1. Upload controller со streaming
2. Logs controller с фильтрацией
3. Export controller (JSON/CSV)
4. Error handling middleware
5. API tests

### Фаза 4: Frontend базовый
**Цель:** Создать React приложение

1. Vite + React + TypeScript setup
2. State management (Zustand)
3. API client
4. File upload component
5. Basic log table

### Фаза 5: UI компоненты
**Цель:** Полный интерфейс

1. Filter panel
2. Search bar с debounce
3. Pagination
4. Export functionality
5. Error handling UI

### Фаза 6: Оптимизация
**Цель:** Производительность и UX

1. Virtual scrolling для больших файлов
2. Caching на backend
3. Lazy loading
4. Loading states
5. Responsive design

### Фаза 7: Финализация
**Цель:** Production-ready

1. E2E tests
2. Docker support
3. CI/CD pipeline
4. Documentation
5. Performance testing

---

## Сравнение альтернатив

### React vs Vue для Frontend

| Критерий | React | Vue |
|----------|-------|-----|
| **Ecosystem** | Богатейший | Хороший |
| **TypeScript** | Отличный | Отличный |
| **Learning curve** | Средняя | Низкая |
| **State management** | Zustand/Redux | Pinia (built-in) |
| **Component libs** | shadcn/ui, Radix | Vuetify, Quasar |
| **Table libs** | TanStack Table | TanStack Table |
| **Performance** | Отличная | Отличная |
| **Community** | Огромное | Большое |
| **Job market** | Лидер | Второе место |

**Рекомендация:** React - больше библиотек, лучшая интеграция с TypeScript, shadcn/ui даёт современный UI.

### State Management

| Решение | React | Vue | Размер | Сложность |
|---------|-------|-----|--------|-----------|
| **Zustand** | Да | - | 1kb | Низкая |
| **Redux Toolkit** | Да | - | 10kb | Средняя |
| **Pinia** | - | Да | 1kb | Низкая |
| **Jotai** | Да | - | 2kb | Низкая |

**Рекомендация:** Zustand для React - минимальный boilerplate, простой API.

### UI Libraries

| Библиотека | Стиль | Кастомизация | Accessibility |
|------------|-------|--------------|---------------|
| **shadcn/ui** | Tailwind | Полная | Отличная |
| **Radix UI** | Headless | Полная | Отличная |
| **MUI** | Material | Средняя | Хорошая |
| **Ant Design** | Ant Design | Средняя | Средняя |

**Рекомендация:** shadcn/ui - копируемые компоненты, Tailwind, полный контроль.

### Backend Storage

| Вариант | Плюсы | Минусы | Для NLogMonitor |
|---------|-------|--------|-----------------|
| **In-Memory** | Быстро, просто | Теряется при рестарте | MVP |
| **Redis** | Быстро, TTL | Доп. зависимость | Масштабирование |
| **SQLite** | Персистентность | Медленнее | Локальное хранение |
| **PostgreSQL** | Полнофункциональная БД | Overkill для логов | Enterprise |

**Рекомендация:** In-Memory для MVP с опцией Redis для масштабирования.

### API Style

| Стиль | Плюсы | Минусы |
|-------|-------|--------|
| **Minimal API** | Компактный код, быстрый старт | Меньше структуры |
| **Controllers** | Организованность, атрибуты | Больше кода |
| **Hybrid** | Лучшее из обоих | Смешанный стиль |

**Рекомендация:** Controllers для основных endpoints, Minimal API для простых.

---

## Чек-листы

### Фаза 1: Базовая инфраструктура
- [ ] Создать NLogMonitor.sln
- [ ] Создать NLogMonitor.Domain проект
- [ ] Создать NLogMonitor.Application проект
- [ ] Создать NLogMonitor.Infrastructure проект
- [ ] Создать NLogMonitor.Api проект
- [ ] Настроить project references
- [ ] Добавить NuGet пакеты
- [ ] Настроить DI в Program.cs
- [ ] Добавить Swagger
- [ ] Создать LogEntry entity
- [ ] Создать LogLevel enum
- [ ] Создать LogSession entity
- [ ] Определить ILogParser interface
- [ ] Определить ISessionStorage interface
- [ ] Определить ILogService interface
- [ ] Создать DTOs
- [ ] Настроить appsettings.json
- [ ] Настроить NLog
- [ ] Первый запуск и проверка Swagger

### Фаза 2: Парсинг и хранение
- [ ] Реализовать NLogParser с Regex
- [ ] Добавить поддержку multi-line messages
- [ ] Реализовать IAsyncEnumerable для streaming
- [ ] Создать InMemorySessionStorage
- [ ] Добавить session expiration logic
- [ ] Реализовать cleanup timer
- [ ] Написать unit tests для парсера
- [ ] Тестирование с реальными лог-файлами
- [ ] Обработка различных форматов дат
- [ ] Обработка ошибок парсинга

### Фаза 3: API endpoints
- [ ] Создать UploadController
- [ ] Реализовать streaming upload
- [ ] Добавить size limit (100MB)
- [ ] Создать LogsController
- [ ] Реализовать фильтрацию
- [ ] Реализовать пагинацию
- [ ] Создать ExportController
- [ ] Реализовать JSON export
- [ ] Реализовать CSV export
- [ ] Добавить exception handling middleware
- [ ] Настроить CORS
- [ ] Написать integration tests
- [ ] Документировать API в Swagger
- [ ] Тестирование через Postman/curl

### Фаза 4: Frontend базовый
- [ ] Создать Vite + React + TypeScript проект
- [ ] Настроить путь aliases (@/)
- [ ] Установить и настроить Tailwind
- [ ] Установить shadcn/ui
- [ ] Настроить Zustand store
- [ ] Создать API client (axios)
- [ ] Определить TypeScript types
- [ ] Создать FileUpload компонент
- [ ] Реализовать drag-and-drop
- [ ] Создать базовый LogTable
- [ ] Настроить routing (если нужен)
- [ ] Проверить работу upload
- [ ] Проверить отображение логов

### Фаза 5: UI компоненты
- [ ] Создать FilterPanel компонент
- [ ] Реализовать level filters
- [ ] Создать SearchBar с debounce
- [ ] Создать Pagination компонент
- [ ] Создать ExportButton
- [ ] Добавить loading states
- [ ] Добавить error handling UI
- [ ] Реализовать level color coding
- [ ] Добавить tooltips
- [ ] Адаптивная верстка
- [ ] Тёмная тема (опционально)

### Фаза 6: Оптимизация
- [ ] Добавить virtual scrolling (react-virtual)
- [ ] Оптимизировать re-renders (React.memo)
- [ ] Добавить caching на backend
- [ ] Оптимизировать bundle size
- [ ] Lazy loading компонентов
- [ ] Skeleton loaders
- [ ] Optimistic updates
- [ ] Performance profiling
- [ ] Lighthouse audit
- [ ] Memory leak проверка

### Фаза 7: Финализация
- [ ] Написать E2E tests (Playwright)
- [ ] Создать Dockerfile для API
- [ ] Создать Dockerfile для frontend
- [ ] docker-compose.yml
- [ ] GitHub Actions CI/CD
- [ ] README.md с инструкциями
- [ ] CHANGELOG.md
- [ ] Performance testing с большими файлами
- [ ] Security review
- [ ] Accessibility audit
- [ ] Cross-browser testing
- [ ] Release v1.0.0

### Pre-release Checklist
- [ ] Все tests проходят
- [ ] No critical/high security issues
- [ ] Performance benchmarks OK
- [ ] Documentation complete
- [ ] Docker images built
- [ ] Demo data prepared
- [ ] Changelog updated
- [ ] Version bumped
- [ ] Git tags created

---

## Дополнительные материалы

### Полезные ссылки
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [React Docs](https://react.dev)
- [TanStack Table](https://tanstack.com/table)
- [Zustand](https://github.com/pmndrs/zustand)
- [shadcn/ui](https://ui.shadcn.com)
- [Tailwind CSS](https://tailwindcss.com)

### NLog формат по умолчанию
```
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
```
Пример: `2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1`

### Команды для старта
```bash
# Backend
dotnet new sln -n NLogMonitor
dotnet new classlib -n NLogMonitor.Domain
dotnet new classlib -n NLogMonitor.Application
dotnet new classlib -n NLogMonitor.Infrastructure
dotnet new webapi -n NLogMonitor.Api

# Frontend
npm create vite@latest client -- --template react-ts
cd client
npm install zustand axios @tanstack/react-table lucide-react
npx shadcn@latest init
```
