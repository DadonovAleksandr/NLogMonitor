# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

nLogMonitor — кроссплатформенное приложение для просмотра и анализа NLog-логов. Full-stack проект с Clean Architecture: .NET 10 Backend + Vue 3/TypeScript Frontend (планируется). Работает в двух режимах: Web (Docker) и Desktop (Photino).

**Текущий статус:** Фаза 4 ✅ В ПРОЦЕССЕ (осталась интеграция E2E). Следующая: Фаза 5 (UI компоненты). Полный план — см. `PLAN.md`.

### Выполнено в Фазе 3.1
- ✅ Path traversal защита (санитизация file.FileName)
- ✅ Desktop-only эндпоинты защищены атрибутом `[DesktopOnly]` (Web-режим возвращает 404)
- ✅ Потоковый экспорт (запись напрямую в Response.Body)
- ✅ Синхронизация temp-каталог и sessionId
- ✅ DirectoryNotFoundException → HTTP 404
- ✅ stop-watching → HTTP 501 Not Implemented
- ✅ XML-комментарии в Swagger
- ✅ 43 интеграционных теста с WebApplicationFactory
- ✅ Очистка temp-каталогов при удалении/истечении сессии (cleanup callbacks)

### Выполнено в Фазе 4
- ✅ Инициализация Vue 3 + Vite + TypeScript
- ✅ shadcn-vue компоненты (Button, Input, Card, Table)
- ✅ Tailwind CSS с dark theme
- ✅ TypeScript типы, соответствующие backend DTOs
- ✅ Axios API клиент с interceptors
- ✅ Pinia stores (logStore, filterStore, recentStore)
- ✅ FileSelector компонент с drag & drop
- ✅ LogTable компонент с TanStack Table
- ✅ LogLevelBadge с цветовой индикацией уровней
- ✅ App.vue с интеграцией компонентов

## Build & Run Commands

```bash
# Backend
dotnet build                              # Сборка solution
dotnet run --project src/nLogMonitor.Api  # Запуск API (http://localhost:5000)
dotnet watch run --project src/nLogMonitor.Api  # Hot reload

# Tests (NUnit) — 240 тестов
dotnet test                               # Все тесты (240: Infrastructure 113 + Application 28 + Api 99)
dotnet test tests/nLogMonitor.Infrastructure.Tests  # Тесты парсера, хранилища, экспорта (113)
dotnet test tests/nLogMonitor.Application.Tests  # Тесты сервисов (28)
dotnet test tests/nLogMonitor.Api.Tests           # Unit + Integration тесты контроллеров (99)
dotnet test --filter "FullyQualifiedName~TestMethodName"  # Один тест

# Lint / Format
dotnet format                             # Автоформатирование кода
dotnet format --verify-no-changes         # Проверка форматирования (CI)

# Frontend
cd client
npm install                           # Установка зависимостей
npm run dev                           # Dev server (http://localhost:5173)
npm run build                         # Production build
npm run lint                          # Проверка кода

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

### Новые файлы (Фаза 3 + 3.1)
- **nLogMonitor.Api/Controllers/** — FilesController, UploadController, LogsController, ExportController, RecentController
- **nLogMonitor.Api/Middleware/** — ExceptionHandlingMiddleware
- **nLogMonitor.Api/Models/** — ApiErrorResponse, OpenFileRequest, OpenDirectoryRequest
- **nLogMonitor.Api/Validators/** — FilterOptionsValidator
- **nLogMonitor.Api/Filters/** — DesktopOnlyAttribute (защита Desktop-only эндпоинтов)
- **nLogMonitor.Infrastructure/Export/** — JsonExporter, CsvExporter (потоковый экспорт)
- **nLogMonitor.Infrastructure/Storage/** — RecentLogsFileRepository
- **nLogMonitor.Application/Configuration/** — FileSettings, RecentLogsSettings, AppSettings (режим Web/Desktop)

### Структура tests/
- **nLogMonitor.Infrastructure.Tests** — 113 тестов: NLogParserTests, InMemorySessionStorageTests (+ cleanup callbacks), DirectoryScannerTests, JsonExporterTests (12), CsvExporterTests (18), RecentLogsFileRepositoryTests
- **nLogMonitor.Application.Tests** — 28 тестов: LogServiceTests (фильтрация, пагинация, поиск, статистика)
- **nLogMonitor.Api.Tests** — 99 тестов: Unit тесты контроллеров (56) + Integration тесты с WebApplicationFactory (43: Files, Upload, Export, Health, Logs, Recent)

### Структура client/
- **src/types/** — TypeScript типы (LogEntry, PagedResult, FilterOptions, etc.)
- **src/api/** — Axios клиент и API методы (client.ts, logs.ts, files.ts, export.ts, health.ts)
- **src/stores/** — Pinia stores (logStore, filterStore, recentStore)
- **src/components/ui/** — shadcn-vue компоненты (Button, Input, Card, Table)
- **src/components/FileSelector/** — загрузка файлов с drag & drop
- **src/components/LogTable/** — таблица логов с TanStack Table и LogLevelBadge

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
- **Потоковый экспорт** — Utf8JsonWriter и StreamWriter пишут напрямую в Response.Body без промежуточных буферов
- **SignalR session lifecycle** — сессия живёт пока открыта вкладка (SignalR connected), удаляется при закрытии (Фаза 6)
- **Fallback TTL** — 5 минут как страховка для потерянных сессий (краш браузера, потеря сети)
- **Cleanup callbacks** — очистка temp-каталогов при удалении сессии через RegisterCleanupCallbackAsync
- **DesktopOnlyAttribute** — фильтр для защиты Desktop-эндпоинтов в Web-режиме (возвращает 404)
- **Virtual scrolling** — для таблицы с миллионами записей (Frontend, Фаза 4-5)

## Configuration

Ключевые настройки в `src/nLogMonitor.Api/appsettings.json`:
- `App.Mode: Web|Desktop` — режим работы приложения (Web по умолчанию, Desktop для полного доступа к файловой системе)
- `SessionSettings.FallbackTtlMinutes: 5` — fallback TTL для потерянных сессий
- `FileSettings.MaxFileSizeMB: 100` — лимит размера файла (RequestSizeLimit = 110MB с запасом)
- `FileSettings.AllowedExtensions: [".log", ".txt"]`
- `Cors.AllowedOrigins` — разрешённые origins для frontend

## API Endpoints (работающие)

- `POST /api/files/open` — открытие файла по пути (Desktop-only, 404 в Web-режиме)
- `POST /api/files/open-directory` — открытие директории (Desktop-only)
- `POST /api/files/{sessionId}/stop-watching` — 501 Not Implemented (планируется в Фазе 6)
- `POST /api/upload` — загрузка файла (Web, max 100MB, path traversal защита)
- `GET /api/logs/{sessionId}` — логи с фильтрацией и пагинацией
- `GET /api/export/{sessionId}?format=json|csv` — потоковый экспорт
- `GET /api/recent` — недавние файлы
- `DELETE /api/recent` — очистка недавних
- `GET /health` — health check

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, SignalR, FluentValidation, NLog
- **Frontend:** Vue 3, TypeScript 5, Vite, Pinia, TanStack Table, Tailwind CSS, shadcn-vue, lucide-vue-next
- **Desktop (план):** Photino.NET
- **Testing:** NUnit 3.x, Moq, coverlet

## Development Notes

**Серверная фильтрация и пагинация:** Все операции фильтрации, поиска и пагинации выполняются на сервере (LINQ over in-memory collection). Клиент получает только запрошенную страницу данных. Это критично для больших логов (100K+ записей).

**Жизненный цикл сессии:**
1. Основной механизм — SignalR: сессия живёт пока открыта вкладка браузера (WebSocket connected)
2. При закрытии вкладки → OnDisconnectedAsync → удаление сессии
3. Fallback TTL (5 мин) — страховка для потерянных соединений (crash браузера, потеря сети)
