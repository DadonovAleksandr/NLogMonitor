# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

nLogMonitor — кроссплатформенное приложение для просмотра и анализа NLog-логов. Full-stack проект с Clean Architecture: .NET 10 Backend + Vue 3/TypeScript Frontend (планируется). Работает в двух режимах: Web (Docker) и Desktop (Photino).

**Текущий статус:** Фаза 2 завершена — парсинг и хранение логов. Следующая: Фаза 3 (API Endpoints). Полный план — см. `PLAN.md`.

## Build & Run Commands

```bash
# Backend
dotnet build                              # Сборка solution
dotnet run --project src/nLogMonitor.Api  # Запуск API (http://localhost:5000)
dotnet watch run --project src/nLogMonitor.Api  # Hot reload

# Tests (NUnit) — 83 теста
dotnet test                               # Все тесты (83: Infrastructure 55 + Application 28)
dotnet test tests/nLogMonitor.Infrastructure.Tests  # Тесты парсера и хранилища (55)
dotnet test tests/nLogMonitor.Application.Tests  # Тесты сервисов (28)
dotnet test --filter "FullyQualifiedName~TestMethodName"  # Один тест

# Lint / Format
dotnet format                             # Автоформатирование кода
dotnet format --verify-no-changes         # Проверка форматирования (CI)

# Проверка работы API
curl http://localhost:5000/health         # Health check → {"status":"healthy","timestamp":"..."}
# Swagger UI: http://localhost:5000/swagger (только в Development)
```

## Architecture

**Clean Architecture** с 4 слоями (зависимости только внутрь):

```
Api (Controllers, SignalR Hub)
    ↓
Application (Interfaces, DTOs, Services)
    ↓
Domain (Entities: LogEntry, LogSession, LogLevel, RecentLogEntry)
    ↑
Infrastructure (Parser, Storage, Export) — реализует интерфейсы Application
```

### Текущая структура src/
- **nLogMonitor.Domain** — сущности: LogEntry, LogSession, LogLevel enum, RecentLogEntry
- **nLogMonitor.Application** — интерфейсы: ILogParser, ISessionStorage, ILogService, IFileWatcherService, ILogExporter, IRecentLogsRepository, IDirectoryScanner; DTOs: LogEntryDto, FilterOptionsDto, PagedResultDto, OpenFileResultDto, RecentLogDto, ClientLogDto; Services/LogService.cs; Configuration/SessionSettings.cs; Exceptions/NoLogFilesFoundException.cs
- **nLogMonitor.Infrastructure** — Parsing/NLogParser.cs (парсер с IAsyncEnumerable), Storage/InMemorySessionStorage.cs (хранилище сессий), FileSystem/DirectoryScanner.cs (сканер директорий)
- **nLogMonitor.Api** — Program.cs с настройкой DI, CORS, Swagger, SignalR, NLog

### Структура tests/
- **nLogMonitor.Infrastructure.Tests** — 55 тестов: NLogParserTests, InMemorySessionStorageTests, DirectoryScannerTests
- **nLogMonitor.Application.Tests** — 28 тестов: LogServiceTests (фильтрация, пагинация, поиск, статистика)

## NLog Format

Поддерживаемый формат:
```
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
```

**Парсинг многострочных записей:**
- Новая запись определяется по дате: `^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}`
- Разделители `|` ищутся **с конца** строки (logger, processid, threadid фиксированы)
- Message может содержать `\n` и `|`

Пример: `2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1`

## Key Patterns

- **IAsyncEnumerable** — для streaming парсинга больших файлов
- **SignalR session lifecycle** — сессия живёт пока открыта вкладка (SignalR connected), удаляется при закрытии (Фаза 6)
- **Fallback TTL** — 5 минут как страховка для потерянных сессий (краш браузера, потеря сети)
- **Virtual scrolling** — для таблицы с миллионами записей (Frontend, Фаза 4-5)

## Configuration

Ключевые настройки в `src/nLogMonitor.Api/appsettings.json`:
- `SessionSettings.FallbackTtlMinutes: 5` — fallback TTL для потерянных сессий
- `FileSettings.MaxFileSizeMB: 100` — лимит размера файла
- `FileSettings.AllowedExtensions: [".log", ".txt"]`
- `Cors.AllowedOrigins` — разрешённые origins для frontend

## API Endpoints (планируемые)

- `POST /api/files/open` — открытие файла по пути (Desktop)
- `POST /api/files/open-directory` — открытие директории
- `POST /api/upload` — загрузка файла (Web, max 100MB)
- `GET /api/logs/{sessionId}` — логи с фильтрацией и пагинацией
- `GET /api/export/{sessionId}?format=json|csv` — экспорт
- `GET /api/recent` — недавние файлы
- `GET /health` — health check (уже работает)

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, SignalR, FluentValidation, NLog
- **Frontend (план):** Vue 3, TypeScript 5, Vite, Pinia, TanStack Table, Tailwind CSS, shadcn-vue
- **Desktop (план):** Photino.NET
- **Testing:** NUnit 3.x, Moq, coverlet

## Development Notes

**Серверная фильтрация и пагинация:** Все операции фильтрации, поиска и пагинации выполняются на сервере (LINQ over in-memory collection). Клиент получает только запрошенную страницу данных. Это критично для больших логов (100K+ записей).

**Жизненный цикл сессии:**
1. Основной механизм — SignalR: сессия живёт пока открыта вкладка браузера (WebSocket connected)
2. При закрытии вкладки → OnDisconnectedAsync → удаление сессии
3. Fallback TTL (5 мин) — страховка для потерянных соединений (crash браузера, потеря сети)
