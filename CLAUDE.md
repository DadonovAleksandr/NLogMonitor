# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NLogMonitor — кроссплатформенное приложение для просмотра и анализа NLog-логов. Full-stack проект с Clean Architecture: .NET 10 Backend + Vue 3/TypeScript Frontend (планируется). Работает в двух режимах: Web (Docker) и Desktop (Photino).

**Текущий статус:** Фаза 1 завершена — базовая инфраструктура проекта. Следующая: Фаза 2 (Парсинг и хранение логов). Полный план — см. `PLAN.md`.

## Build & Run Commands

```bash
# Backend
dotnet build                              # Сборка solution
dotnet run --project src/NLogMonitor.Api  # Запуск API (localhost:5000)
dotnet watch run --project src/NLogMonitor.Api  # Hot reload

# Tests (NUnit) — когда будут созданы
dotnet test                               # Все тесты
dotnet test tests/NLogMonitor.Application.Tests  # Конкретный проект
dotnet test --filter "FullyQualifiedName~TestMethodName"  # Один тест

# Проверка работы API
curl http://localhost:5000/health         # Health check
# Swagger UI: http://localhost:5000/swagger
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
- **NLogMonitor.Domain** — сущности: LogEntry, LogSession, LogLevel enum, RecentLogEntry
- **NLogMonitor.Application** — интерфейсы: ILogParser, ISessionStorage, ILogService, IFileWatcherService, ILogExporter, IRecentLogsRepository; DTOs: LogEntryDto, FilterOptionsDto, PagedResultDto, OpenFileResultDto, RecentLogDto, ClientLogDto
- **NLogMonitor.Infrastructure** — пока пустой, будет содержать реализации
- **NLogMonitor.Api** — Program.cs с настройкой DI, CORS, Swagger, SignalR, NLog

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
- **TTL сессии** — автоматическое удаление через 1 час (настройка в appsettings.json)
- **SignalR** — real-time обновления при изменении файла (Фаза 6)
- **Virtual scrolling** — для таблицы с миллионами записей (Frontend, Фаза 4-5)

## Configuration

Ключевые настройки в `src/NLogMonitor.Api/appsettings.json`:
- `SessionSettings.TimeToLiveMinutes: 60` — TTL сессии
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
