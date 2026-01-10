# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

nLogMonitor — кроссплатформенное приложение для просмотра и анализа NLog-логов. Full-stack проект с Clean Architecture: .NET 10 Backend + Vue 3/TypeScript Frontend. Работает в двух режимах: Web (скрипты запуска) и Desktop (Photino).

**Текущий статус:** Фаза 9 ✅ ЗАВЕРШЕНО (Photino Desktop). Следующая: Фаза 10 (Оптимизация и тестирование). Полный план — см. `PLAN.md`.

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

### Выполнено в Фазе 5
- ✅ FilterPanel компонент с toggle-кнопками по уровням и подсчётом записей
- ✅ SearchBar компонент с debounce 300ms и иконкой поиска
- ✅ Pagination компонент с выбором размера страницы (50, 100, 200)
- ✅ ExportButton компонент с dropdown выбора формата (JSON/CSV)
- ✅ RecentFiles компонент с историей открытых файлов
- ✅ Loading states и error handling через Toast компоненты
- ✅ Empty states с информативными placeholders
- ✅ Responsive design для корректного отображения на всех разрешениях

### Выполнено в Фазе 6 (Real-time обновления через SignalR)
- ✅ FileWatcherService с debounce 200ms и поддержкой множественных сессий
- ✅ LogWatcherHub для real-time коммуникации (JoinSession, LeaveSession, SendNewLogs)
- ✅ Управление lifecycle сессий: привязка к connectionId, автоматическое удаление при disconnect
- ✅ ISessionStorage расширен методами BindConnectionAsync, UnbindConnectionAsync, GetSessionByConnectionAsync
- ✅ InMemorySessionStorage: маппинг connectionId ↔ sessionId через ConcurrentDictionary
- ✅ FileWatcherBackgroundService для автоматического запуска FileWatcher
- ✅ Frontend SignalR клиент (@microsoft/signalr) с автореконнектом
- ✅ Composable useFileWatcher для интеграции с Vue компонентами
- ✅ LiveIndicator компонент с 4 состояниями (Live, Connecting, Reconnecting, Disconnected)
- ✅ Интеграция real-time обновлений в LogTable и FilterPanel
- ✅ 8 интеграционных тестов для LogWatcherHub
- ✅ 3 нагрузочных теста (500 файлов × 100 записей, 100 одновременных обновлений, debounce)

### Критичные исправления (Фаза 6.1)
- ✅ Multi-client support: 1:N маппинг (один sessionId → множество connectionId) для поддержки multi-tab и автореконнекта
- ✅ Инкрементальное чтение: добавлено поле `LastReadPosition` в LogSession, метод `ParseFromPositionAsync` в ILogParser
- ✅ Thread-safe обновление: метод `AppendEntriesAsync` в ISessionStorage для атомарного добавления новых записей
- ✅ Cleanup callbacks: автоматическая остановка FileWatcher при удалении сессии (предотвращение утечек ресурсов)
- ✅ NotifyFilters.FileName: поддержка log rotation и корректная обработка переименования файлов
- ✅ Race condition fix: подписка на события FileSystemWatcher ДО включения мониторинга
- ✅ Truncation handling: обработка случаев, когда файл усечён (новый размер < LastReadPosition)

### Выполнено в Фазе 7 (Скрипты запуска и конфигурация)
- ✅ Shell скрипты для Linux: start-dev.sh, build.sh, stop.sh
- ✅ Windows скрипт остановки: stop.bat
- ✅ Production конфигурация: appsettings.Production.json
- ✅ Раздача статики через UseStaticFiles и UseDefaultFiles в Program.cs
- ✅ API метрики: GET /api/metrics с sessions_active_count, logs_total_count, sessions_memory_bytes, server_uptime_seconds, signalr_connections_count
- ✅ Документация: README.md обновлён, .env.example создан
- ✅ ISessionStorage расширен методами GetActiveSessionCountAsync, GetTotalLogsCountAsync, GetActiveConnectionsCountAsync

### Выполнено в Фазе 8 (Client-side Logging)
- ✅ ClientLogsController: POST /api/client-logs для приёма batch логов с фронтенда
- ✅ Rate Limiting: 100 запросов в минуту на IP (ASP.NET Core Rate Limiting middleware)
- ✅ Нормализация уровней: warning→warn, fatal→error, critical→error
- ✅ Валидация: Level и Message обязательные, лимиты длины полей
- ✅ Санитизация: экранирование HTML, удаление управляющих символов
- ✅ Structured logging с NLog: префикс [CLIENT], контекст (userId, version, url, userAgent)
- ✅ Frontend ClientLogger service: trace/debug/info/warn/error/fatal/exception методы
- ✅ Буферизация (batchSize: 10) и автоматический flush по таймеру (5 сек)
- ✅ Retry с exponential backoff (1s, 2s, 4s) - 3 попытки
- ✅ Глобальный контекст: setGlobalContext({ userId, version, sessionId })
- ✅ Автоматическое добавление url и userAgent к каждому логу
- ✅ Error handlers: window.onerror, window.onunhandledrejection, app.config.errorHandler
- ✅ Отправка логов при закрытии страницы (beforeunload) и visibilitychange
- ✅ 23 интеграционных теста для /api/client-logs

### Выполнено в Фазе 9 (Photino Desktop)
- ✅ Проект nLogMonitor.Desktop с Photino.NET 3.1.18
- ✅ Embedded ASP.NET Core сервер в фоновом потоке
- ✅ PhotinoWindow с WebView и автоматическим выбором порта
- ✅ Нативные диалоги: ShowOpenFile, ShowOpenFolder (кроссплатформенные)
- ✅ JS ↔ .NET Bridge: BridgeRequest/BridgeResponse, JSON сериализация
- ✅ Message handler для команд: isDesktop, getServerPort, showOpenFile, showOpenFolder
- ✅ Frontend usePhotinoBridge composable с Promise-based API
- ✅ FileSelector обновлён: Web режим (drag & drop) и Desktop режим (нативные кнопки)
- ✅ Скрипты сборки: build-desktop.bat (Windows), build-desktop.sh (Linux)
- ✅ Self-contained exe ~50 MB (< 100 MB DoD)

## Build & Run Commands

Все скрипты находятся в папке `scripts/`.

```bash
# Быстрый запуск (Development)
scripts\start-dev.bat                     # Windows: backend + frontend с hot reload
./scripts/start-dev.sh                    # Linux: backend + frontend с hot reload

# Остановка серверов
scripts\stop.bat                          # Windows
./scripts/stop.sh                         # Linux

# Production сборка (Web)
scripts\build.bat                         # Windows
./scripts/build.sh                        # Linux
# Результат в publish/, запуск: cd publish && ./nLogMonitor.Api.exe

# Desktop сборка (Photino)
scripts\build-desktop.bat                 # Windows: frontend + Desktop exe
./scripts/build-desktop.sh                # Linux: frontend + Desktop binary
# Результат в publish/desktop/win-x64/, запуск: nLogMonitor.Desktop.exe

# Desktop разработка (hot reload)
scripts\start-desktop-dev.bat             # Windows: Vite dev server + Desktop с hot reload
scripts\stop-desktop-dev.bat              # Windows: остановка Desktop dev процессов

# Backend
dotnet build                              # Сборка solution
dotnet run --project src/nLogMonitor.Api  # Запуск API (http://localhost:5000)
dotnet watch run --project src/nLogMonitor.Api  # Hot reload

# Tests (NUnit) — 306 тестов
dotnet test                               # Все тесты (306)
dotnet test tests/nLogMonitor.Infrastructure.Tests  # Тесты парсера, хранилища, экспорта, FileWatcher (134 теста)
dotnet test tests/nLogMonitor.Application.Tests  # Тесты сервисов (28 тестов)
dotnet test tests/nLogMonitor.Api.Tests           # Unit + Integration тесты контроллеров + SignalR Hub + нагрузочные (144 теста)
dotnet test --filter "FullyQualifiedName~TestMethodName"  # Один тест

# Lint / Format
dotnet format                             # Автоформатирование кода
dotnet format --verify-no-changes         # Проверка форматирования (CI)

# Frontend
cd client
npm install                           # Установка зависимостей
npm run dev                           # Dev server (http://localhost:5173)
npm run build                         # Production build

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
- **nLogMonitor.Infrastructure** — Parsing/NLogParser.cs (парсер с IAsyncEnumerable), Storage/InMemorySessionStorage.cs (хранилище сессий), FileSystem/DirectoryScanner.cs (сканер директорий), FileSystem/FileWatcherService.cs (отслеживание изменений с debounce)
- **nLogMonitor.Api** — Program.cs с настройкой DI, CORS, Swagger, SignalR, NLog
- **nLogMonitor.Desktop** — Photino Desktop приложение: Program.cs (embedded ASP.NET Core + PhotinoWindow), BridgeRequest/BridgeResponse (IPC), нативные диалоги, Controllers/Hubs/Services (скопированы из Api)

### Новые файлы (Фаза 3 + 3.1 + 6 + 6.1 + 7 + 9)
- **nLogMonitor.Api/Controllers/** — FilesController (с cleanup callbacks), UploadController, LogsController, ExportController, RecentController, MetricsController
- **nLogMonitor.Api/Hubs/** — LogWatcherHub (SignalR Hub для real-time обновлений)
- **nLogMonitor.Api/Services/** — FileWatcherBackgroundService (инкрементальное чтение с LastReadPosition)
- **nLogMonitor.Api/Middleware/** — ExceptionHandlingMiddleware
- **nLogMonitor.Api/Models/** — ApiErrorResponse, OpenFileRequest, OpenDirectoryRequest
- **nLogMonitor.Api/Validators/** — FilterOptionsValidator
- **nLogMonitor.Api/Filters/** — DesktopOnlyAttribute (защита Desktop-only эндпоинтов)
- **nLogMonitor.Infrastructure/Export/** — JsonExporter, CsvExporter (потоковый экспорт)
- **nLogMonitor.Infrastructure/Storage/** — RecentLogsFileRepository, InMemorySessionStorage (1:N маппинг, AppendEntriesAsync)
- **nLogMonitor.Infrastructure/FileSystem/** — FileWatcherService (debounce 200ms, NotifyFilters.FileName, race condition fix)
- **nLogMonitor.Infrastructure/Parsing/** — NLogParser (ParseFromPositionAsync для инкрементального чтения)
- **nLogMonitor.Application/Configuration/** — FileSettings, RecentLogsSettings, AppSettings (режим Web/Desktop)
- **nLogMonitor.Application/DTOs/** — MetricsDto (метрики сервера)
- **nLogMonitor.Application/Interfaces/ISessionStorage** — методы BindConnectionAsync, UnbindConnectionAsync, GetSessionByConnectionAsync, AppendEntriesAsync, GetActiveSessionCountAsync, GetTotalLogsCountAsync, GetActiveConnectionsCountAsync
- **nLogMonitor.Application/Interfaces/ILogParser** — метод ParseFromPositionAsync
- **nLogMonitor.Domain/Entities/LogSession** — поле LastReadPosition
- **nLogMonitor.Desktop/Program.cs** — embedded ASP.NET Core + PhotinoWindow, BridgeRequest/BridgeResponse, message handlers
- **nLogMonitor.Desktop/Controllers/** — скопированы из Api (FilesController, LogsController, ExportController, etc.)
- **nLogMonitor.Desktop/Hubs/** — LogWatcherHub (SignalR Hub)
- **nLogMonitor.Desktop/Services/** — FileWatcherBackgroundService
- **scripts/** — папка со скриптами запуска и сборки:
  - `start-dev.bat/sh` — запуск dev mode (backend + frontend)
  - `stop.bat/sh` — остановка серверов (Web)
  - `build.bat/sh` — production сборка (Web)
  - `build-desktop.bat/sh` — сборка Desktop приложения
  - `start-desktop-dev.bat` — Desktop dev mode с Vite hot reload
  - `stop-desktop-dev.bat` — остановка Desktop dev процессов

### Структура tests/
- **nLogMonitor.Infrastructure.Tests** — NLogParserTests, InMemorySessionStorageTests (+ cleanup callbacks), DirectoryScannerTests, JsonExporterTests, CsvExporterTests, RecentLogsFileRepositoryTests, FileWatcherServiceTests (debounce, множественные сессии)
- **nLogMonitor.Application.Tests** — LogServiceTests (фильтрация, пагинация, поиск, статистика)
- **nLogMonitor.Api.Tests** — Unit тесты контроллеров + Integration тесты с WebApplicationFactory (Files, Upload, Export, Health, Logs, Recent, LogWatcherHub, ClientLogs) + нагрузочные тесты (500 файлов, 100 обновлений, debounce)

### Структура client/
- **src/types/** — TypeScript типы (LogEntry, PagedResult, FilterOptions, etc.)
- **src/api/** — Axios клиент и API методы (client.ts, config.ts, logs.ts, files.ts, export.ts, health.ts, signalr.ts, client-logs.ts)
- **src/services/** — ClientLogger service (logger.ts) — буферизация, retry, error handlers
- **src/composables/** — useFileWatcher (интеграция SignalR с Vue компонентами), usePhotinoBridge (Desktop IPC), useToast (уведомления)
- **src/stores/** — Pinia stores (logStore, filterStore, recentStore)
- **src/components/ui/** — shadcn-vue компоненты (Button, Input, Card, Table, Toast, Badge, Select, etc.)
- **src/components/FileSelector/** — загрузка файлов (Web: drag & drop, Desktop: нативные диалоги)
- **src/components/LogTable/** — таблица логов с TanStack Table и LogLevelBadge, интеграция real-time обновлений
- **src/components/FilterPanel/** — фильтры по уровням логирования с подсчётом, интеграция real-time обновлений
- **src/components/SearchBar/** — поиск с debounce и очисткой
- **src/components/Pagination/** — пагинация с выбором размера страницы
- **src/components/ExportButton/** — экспорт в JSON/CSV
- **src/components/RecentFiles/** — список недавних файлов
- **src/components/LiveIndicator/** — индикатор статуса real-time соединения (Live, Connecting, Reconnecting, Disconnected)

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

- **IAsyncEnumerable** — для streaming парсинга больших файлов (ParseAsync)
- **Инкрементальное чтение** — ParseFromPositionAsync с LastReadPosition для чтения только новых записей (избегаем перечитывания всего файла)
- **Потоковый экспорт** — Utf8JsonWriter и StreamWriter пишут напрямую в Response.Body без промежуточных буферов
- **Multi-client support** — 1:N маппинг (один sessionId → множество connectionId) для поддержки multi-tab и автореконнекта
- **SignalR session lifecycle** — сессия живёт пока открыта вкладка (SignalR connected), удаляется при закрытии последнего клиента
- **FileWatcher debounce** — 200ms задержка для группировки множественных изменений файла
- **Real-time обновления** — только новые записи отображаются в UI через SignalR (инкрементально, без дубликатов)
- **Thread-safe updates** — AppendEntriesAsync с lock(session) для атомарного добавления новых записей
- **Fallback TTL** — 5 минут как страховка для потерянных сессий (краш браузера, потеря сети)
- **Cleanup callbacks** — автоматическая остановка FileWatcher и очистка temp-каталогов при удалении сессии
- **DesktopOnlyAttribute** — фильтр для защиты Desktop-эндпоинтов в Web-режиме (возвращает 404)
- **Log rotation support** — NotifyFilters.FileName для обработки переименования файлов при ротации
- **Race condition prevention** — подписка на события FileSystemWatcher ДО включения EnableRaisingEvents
- **Truncation handling** — обработка случаев усечения файла (newSize < LastReadPosition)
- **Virtual scrolling** — для таблицы с миллионами записей (Frontend, Фаза 4-5)
- **Photino Bridge** — JS ↔ .NET IPC через window.external.sendMessage/receiveMessage, Promise-based async API
- **Embedded ASP.NET Core** — Kestrel сервер в фоновом потоке Desktop приложения, динамический выбор порта

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
- `POST /api/files/{sessionId}/stop-watching` — остановка мониторинга файла (реализовано в Фазе 6)
- `POST /api/upload` — загрузка файла (Web, max 100MB, path traversal защита)
- `GET /api/logs/{sessionId}` — логи с фильтрацией и пагинацией
- `GET /api/export/{sessionId}?format=json|csv` — потоковый экспорт
- `GET /api/recent` — недавние файлы
- `DELETE /api/recent` — очистка недавних
- `GET /api/metrics` — метрики сервера (sessions, logs, memory, uptime, connections)
- `POST /api/client-logs` — приём batch логов с фронтенда (rate limiting 100 req/min per IP)
- `GET /health` — health check

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, SignalR, FluentValidation, NLog
- **Frontend:** Vue 3.5, TypeScript 5.9, Vite 7.2, Pinia 3.0, TanStack Table 8.21, Tailwind CSS 3.4, Reka UI 2.7, lucide-vue-next, @microsoft/signalr 10.0
- **Desktop:** Photino.NET 3.1.18 (embedded ASP.NET Core + WebView)
- **Testing:** NUnit 4.x, Moq, FluentAssertions, coverlet

## Development Notes

**Серверная фильтрация и пагинация:** Все операции фильтрации, поиска и пагинации выполняются на сервере (LINQ over in-memory collection). Клиент получает только запрошенную страницу данных. Это критично для больших логов (100K+ записей).

**Жизненный цикл сессии:**
1. Основной механизм — SignalR: сессия живёт пока открыта хотя бы одна вкладка браузера (WebSocket connected)
2. Поддержка multi-tab: один sessionId может быть связан с множеством connectionId (1:N маппинг)
3. При закрытии вкладки → OnDisconnectedAsync → удаление connectionId из коллекции
4. Сессия удаляется только когда отключается ПОСЛЕДНИЙ клиент (все connectionId удалены)
5. Автореконнект: при восстановлении соединения клиент автоматически присоединяется к существующей сессии
6. Fallback TTL (5 мин) — страховка для потерянных соединений (crash браузера, потеря сети)
7. Cleanup callbacks: при удалении сессии автоматически останавливается FileWatcher и очищаются ресурсы

**Инкрементальное чтение файлов:**
1. При открытии файла: полный парсинг, LastReadPosition = fileSize
2. При изменении файла: ParseFromPositionAsync(filePath, LastReadPosition) читает только новые строки
3. AppendEntriesAsync атомарно добавляет новые записи в session.Entries и обновляет LevelCounts
4. Обработка truncation: если newSize < LastReadPosition, парсинг начинается с позиции 0
5. Real-time клиенты получают только новые записи (без дубликатов и лишних данных)

**Performance:**
- Для файла 100K записей при изменении читаются только новые строки (не весь файл)
- Избегаем дубликатов в UI и рассинхронизации счётчиков
- GetLogs/Export работают с теми же данными, что и real-time view (consistency)
