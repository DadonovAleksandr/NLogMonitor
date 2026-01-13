# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

nLogMonitor — кроссплатформенное приложение для просмотра и анализа NLog-логов. Full-stack проект с Clean Architecture: .NET 10 Backend + Vue 3/TypeScript Frontend. Работает в двух режимах: **Web** (скрипты запуска) и **Desktop** (Photino).

**Текущий статус:** Фаза 9 завершена (Photino Desktop). Следующая: Фаза 10 (Оптимизация). См. `PLAN.md`.

## Build & Run Commands

```bash
# Development (backend + frontend с hot reload)
scripts\start-dev.bat        # Windows
./scripts/start-dev.sh       # Linux

# Остановка серверов
scripts\stop.bat             # Windows
./scripts/stop.sh            # Linux

# Production сборка (Web)
scripts\build.bat            # Windows → publish/nLogMonitor.Api.exe
./scripts/build.sh           # Linux

# Desktop сборка (Photino)
scripts\build-desktop.bat    # Windows → publish/desktop/win-x64/nLogMonitor.Desktop.exe
./scripts/build-desktop.sh   # Linux

# Desktop dev mode (hot reload)
scripts\start-desktop-dev.bat     # Windows
scripts\stop-desktop-dev.bat      # Остановка

# Backend отдельно
dotnet build
dotnet run --project src/nLogMonitor.Api              # http://localhost:5000
dotnet watch run --project src/nLogMonitor.Api        # Hot reload

# Tests (NUnit, 330 тестов)
dotnet test                                            # Все тесты
dotnet test tests/nLogMonitor.Infrastructure.Tests     # Infrastructure (143)
dotnet test tests/nLogMonitor.Application.Tests        # Application (28)
dotnet test tests/nLogMonitor.Api.Tests               # Api (159)
dotnet test --filter "FullyQualifiedName~TestMethodName"  # Один тест

# Lint / Format
dotnet format
dotnet format --verify-no-changes    # CI check

# Frontend
cd client
npm install
npm run dev       # http://localhost:5173
npm run build     # Production

# API проверка
curl http://localhost:5000/health
# Swagger: http://localhost:5000/swagger (только Development)
```

## Architecture

**Clean Architecture** с 4 слоями (зависимости только внутрь):

```
src/
├── nLogMonitor.Domain        # Entities: LogEntry, LogSession, LogLevel, RecentLogEntry
├── nLogMonitor.Application   # Interfaces, DTOs, Services (ILogParser, ISessionStorage, LogService)
├── nLogMonitor.Infrastructure # Реализации: NLogParser, InMemorySessionStorage, FileWatcherService
├── nLogMonitor.Api           # Controllers, SignalR Hub, Middleware
└── nLogMonitor.Desktop       # Photino: embedded ASP.NET Core + WebView

tests/
├── nLogMonitor.Infrastructure.Tests  # Parser, Storage, Export, FileWatcher
├── nLogMonitor.Application.Tests     # LogService
└── nLogMonitor.Api.Tests            # Controllers, SignalR Hub (integration)

client/
├── src/api/           # Axios client, SignalR, info
├── src/components/    # Vue компоненты (LogTable, HeaderTabBar, StatusBar, Toolbar, FileSelector, etc.)
├── src/composables/   # useFileWatcher, usePhotinoBridge, useToast
├── src/stores/        # Pinia (logStore, tabsStore, filterStore, recentStore, settingsStore)
├── src/services/      # ClientLogger
└── src/types/         # TypeScript types
```

## Key Implementation Patterns

### NLog Format
```
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
```
- Пример: `2024-01-15 10:30:45.1234|INFO|App started|MyApp.Program|1234|1`
- Новая запись определяется по дате: `^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}`
- Разделители `|` ищутся **с конца** (logger, processid, threadid фиксированы)
- Message может содержать `\n` и `|` (многострочные stack traces)

### Real-time Updates (SignalR)
- **1:N маппинг**: один sessionId → множество connectionId (multi-tab support)
- **Инкрементальное чтение**: `ParseFromPositionAsync` с `LastReadPosition` — только новые строки
- **FileWatcher debounce**: 200ms для группировки изменений
- **Session lifecycle**: сессия живёт пока SignalR connected; fallback TTL 5 мин
- **Cleanup callbacks**: при удалении сессии останавливается FileWatcher

### Server-side Operations
Фильтрация, поиск и пагинация выполняются **на сервере** (LINQ). Критично для 100K+ записей.

### Photino Desktop
- Embedded ASP.NET Core в фоновом потоке + PhotinoWindow с WebView
- JS ↔ .NET Bridge: `BridgeRequest/BridgeResponse`, Promise-based async API
- Нативные диалоги: `ShowOpenFile`, `ShowOpenFolder`

## API Endpoints

| Endpoint | Метод | Описание |
|----------|-------|----------|
| `/api/upload` | POST | Загрузка файла (Web, max 100MB) |
| `/api/files/open` | POST | Открытие файла по пути (Desktop-only) |
| `/api/files/open-directory` | POST | Открытие директории (Desktop-only) |
| `/api/files/{sessionId}/stop-watching` | POST | Остановка мониторинга |
| `/api/logs/{sessionId}` | GET | Логи с фильтрацией и пагинацией |
| `/api/export/{sessionId}?format=json\|csv` | GET | Потоковый экспорт |
| `/api/recent` | GET/DELETE | Недавние файлы |
| `/api/settings` | GET/PUT | Пользовательские настройки |
| `/api/metrics` | GET | Метрики сервера |
| `/api/client-logs` | POST | Логи с фронтенда (rate limit 100 req/min) |
| `/api/info` | GET | Информация о приложении (версия) |
| `/health` | GET | Health check |
| `/hubs/log-watcher` | SignalR | Real-time: JoinSession, LeaveSession, NewLogs |

## Configuration

`src/nLogMonitor.Api/appsettings.json`:
- `App.Mode: Web|Desktop` — режим работы
- `SessionSettings.FallbackTtlMinutes: 5` — TTL для потерянных сессий
- `FileSettings.MaxFileSizeMB: 100` — лимит размера файла
- `FileSettings.AllowedExtensions: [".log", ".txt"]`
- `Cors.AllowedOrigins` — разрешённые origins

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, SignalR, FluentValidation, NLog
- **Frontend:** Vue 3.5, TypeScript 5.9, Vite 7.2, Pinia 3.0, TanStack Table 8.21, Tailwind CSS, shadcn-vue, @microsoft/signalr 10.0
- **Desktop:** Photino.NET 3.1.18
- **Testing:** NUnit 4.x, Moq, FluentAssertions
