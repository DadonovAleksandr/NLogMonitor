# 📝 Changelog

Все значимые изменения в проекте документируются в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/),
проект придерживается [Semantic Versioning](https://semver.org/lang/ru/).

---

## [Unreleased]

> Планируется: Фаза 10 — Оптимизация и тестирование

---

## [0.9.0] - 2026-01-10

> 🎯 **Фаза 9: Photino Desktop** ✅

### 🚀 Добавлено
- [x] Проект nLogMonitor.Desktop с Photino.NET 3.1.18
- [x] Embedded ASP.NET Core сервер в фоновом потоке
- [x] PhotinoWindow с WebView и автоматическим выбором порта
- [x] Нативные диалоги: ShowOpenFile, ShowOpenFolder (кроссплатформенные)
- [x] JS ↔ .NET Bridge: BridgeRequest/BridgeResponse, JSON сериализация
- [x] Message handler для команд: isDesktop, getServerPort, showOpenFile, showOpenFolder
- [x] Frontend usePhotinoBridge composable с Promise-based API
- [x] FileSelector обновлён: Web режим (drag & drop) и Desktop режим (нативные кнопки)
- [x] Скрипты сборки: build-desktop.bat (Windows), build-desktop.sh (Linux/macOS)
- [x] Self-contained exe ~50 MB (< 100 MB DoD)

---

## [0.8.0] - 2026-01-10

> 🎯 **Фаза 8: Client-side Logging** ✅

### 🚀 Добавлено
- [x] POST /api/client-logs — приём batch логов с фронтенда
- [x] Rate Limiting: 100 запросов в минуту на IP (ASP.NET Core Rate Limiting middleware)
- [x] Нормализация уровней: warning→warn, fatal→error, critical→error
- [x] Валидация: Level и Message обязательные, лимиты длины полей
- [x] Санитизация: экранирование HTML, удаление управляющих символов
- [x] Structured logging с NLog: префикс [CLIENT], контекст (userId, version, url, userAgent)
- [x] Frontend ClientLogger service: trace/debug/info/warn/error/fatal/exception методы
- [x] Буферизация (batchSize: 10) и автоматический flush по таймеру (5 сек)
- [x] Retry с exponential backoff (1s, 2s, 4s) — 3 попытки
- [x] Глобальный контекст: setGlobalContext({ userId, version, sessionId })
- [x] Автоматическое добавление url и userAgent к каждому логу
- [x] Error handlers: window.onerror, window.onunhandledrejection, app.config.errorHandler
- [x] Отправка логов при закрытии страницы (beforeunload) и visibilitychange
- [x] 23 интеграционных теста для /api/client-logs

### 🔧 Изменено
- [x] nlog.config: добавлен ${all-event-properties} для вывода контекста клиентских логов
- [x] console.error заменены на ClientLogger в App.vue, useFileWatcher.ts, signalr.ts, ExportButton.vue
- [x] Исправлен race condition авто-flush таймера в logger.ts

### 📊 Статистика
- Общее количество тестов: 306 (было 283)
  - Infrastructure: 134 (без изменений)
  - Application: 28 (без изменений)
  - Api: 144 (было 121)

---

## [0.7.0] - 2026-01-10

> 🎯 **Фаза 7: Скрипты запуска и Production конфигурация** ✅

### 🚀 Добавлено
- [x] Shell скрипты для Linux/macOS: start-dev.sh, build.sh, stop.sh
- [x] Windows скрипт остановки: stop.bat
- [x] Production конфигурация: appsettings.Production.json
- [x] Раздача статики через UseStaticFiles и UseDefaultFiles в Program.cs
- [x] GET /api/metrics — метрики сервера (sessions_active_count, logs_total_count, sessions_memory_bytes, server_uptime_seconds, signalr_connections_count)
- [x] Документация: README.md обновлён, .env.example создан
- [x] ISessionStorage расширен методами GetActiveSessionCountAsync, GetTotalLogsCountAsync, GetActiveConnectionsCountAsync

---

## [0.1.0] - 2026-01-09

> 🎯 **Фаза 1: Базовая инфраструктура** ✅

### 🚀 Добавлено
- [x] Создание Solution и проектов (.NET 10.0)
- [x] Настройка Dependency Injection
- [x] Domain Entities: LogEntry, LogSession, LogLevel, RecentLogEntry
- [x] Application Interfaces: ILogParser, ISessionStorage, ILogService, IFileWatcherService, ILogExporter, IRecentLogsRepository
- [x] Application DTOs: LogEntryDto, FilterOptionsDto, PagedResultDto, OpenFileResultDto, RecentLogDto, ClientLogDto
- [x] Swagger/OpenAPI документация
- [x] Health checks endpoint (/health)
- [x] Базовая конфигурация CORS (для localhost:5173)
- [x] NLog конфигурация с файловыми логами
- [x] SignalR инфраструктура для real-time обновлений

---

## [0.2.0] - 2025-01-09

> 🎯 **Фаза 2: Парсинг и хранение логов** ✅

### 🚀 Добавлено
- [x] NLogParser с IAsyncEnumerable и Span<char> оптимизациями для высокопроизводительного парсинга
- [x] Поддержка многострочных сообщений (exceptions, stack traces)
- [x] InMemorySessionStorage с ConcurrentDictionary, TTL (5 мин), sliding expiration
- [x] Фоновый cleanup timer для автоматической очистки истекших сессий
- [x] DirectoryScanner для поиска последнего лог-файла в директории
- [x] LogService с серверной фильтрацией, поиском и пагинацией через LINQ
- [x] 83 unit-теста (55 Infrastructure + 28 Application) с полным покрытием

---

## [0.3.0] - 2026-01-09

> 🎯 **Фаза 3: REST API** ✅

### 🚀 Добавлено
- [x] POST /api/upload — загрузка файлов (max 100MB, .log/.txt)
- [x] GET /api/logs/{sessionId} — получение логов с фильтрацией и пагинацией
- [x] GET /api/export/{sessionId} — экспорт в JSON/CSV с потоковой генерацией
- [x] POST /api/files/open — открытие файла по пути (Desktop)
- [x] POST /api/files/open-directory — открытие директории с автовыбором последнего .log
- [x] GET /api/recent, DELETE /api/recent — управление недавними файлами
- [x] Фильтрация по уровням, датам, логгеру, полнотекстовый поиск
- [x] Валидация входных данных (FluentValidation)
- [x] ExceptionHandlingMiddleware для единообразных ошибок
- [x] JsonExporter и CsvExporter с потоковой генерацией
- [x] RecentLogsFileRepository для хранения истории в JSON
- [x] 56 unit-тестов для контроллеров и валидаторов
- [x] Общее количество тестов: 160 (Infrastructure 76 + Application 28 + Api 56)

---

## [0.3.1] - 2026-01-10

> 🎯 **Фаза 3.1: Исправления и улучшения после код-ревью** ✅

### 🔒 Безопасность
- [x] Path traversal защита в UploadController — санитизация file.FileName через Path.GetFileName()
- [x] Desktop-only эндпоинты защищены атрибутом `[DesktopOnly]` — возвращают 404 в Web-режиме
- [x] Добавлена конфигурация `App.Mode: Web|Desktop` для определения режима работы

### 🔧 Изменено
- [x] Потоковый экспорт — Utf8JsonWriter и StreamWriter пишут напрямую в Response.Body
- [x] stop-watching возвращает HTTP 501 Not Implemented (вместо 204 без действия)
- [x] DirectoryNotFoundException теперь возвращает HTTP 404 (вместо 500)
- [x] Синхронизация temp-каталога и sessionId — теперь всегда совпадают
- [x] Все контроллеры возвращают консистентный формат ApiErrorResponse

### 🚀 Добавлено
- [x] DesktopOnlyAttribute — ActionFilterAttribute для защиты Desktop-only эндпоинтов
- [x] AppSettings конфигурация с режимом Web/Desktop
- [x] XML-комментарии включены в Swagger (GenerateDocumentationFile)
- [x] 24 интеграционных теста с WebApplicationFactory
- [x] Unit-тесты для экспортеров (30 тестов: JsonExporter 12, CsvExporter 18)

### 📊 Статистика
- Общее количество тестов: 240 (было 160) *(число актуально на момент релиза)*
  - Infrastructure: 113 (было 76)
  - Application: 28 (без изменений)
  - Api: 99 (было 56)

---

## [0.4.0] - 2026-01-10

> 🎯 **Фаза 4: Базовый Frontend** ✅

### 🚀 Добавлено
- [x] Vite + Vue 3 + TypeScript проект
- [x] Pinia stores (logStore, filterStore, recentStore)
- [x] Axios API client с interceptors
- [x] Tailwind CSS + Reka UI (shadcn-vue API) (Button, Input, Card, Table)
- [x] TypeScript типы соответствующие backend DTOs
- [x] FileSelector компонент с drag & drop
- [x] LogTable компонент с TanStack Table
- [x] LogLevelBadge с цветовой индикацией уровней
- [x] Dark theme поддержка

---

## [0.5.0] - 2026-01-10

> 🎯 **Фаза 5: Расширенные UI компоненты** ✅

### 🚀 Добавлено
- [x] FilterPanel (фильтры по уровням с счётчиками, toggle-кнопками, подсчётом записей)
- [x] SearchBar с debounce 300ms и иконкой поиска
- [x] Pagination компонент (Previous/Next, выбор размера страницы: 50, 100, 200)
- [x] ExportButton (dropdown с выбором JSON/CSV)
- [x] RecentFiles компонент (история открытых файлов)
- [x] Loading states и error handling через Toast компоненты
- [x] Empty states с информативными placeholders
- [x] Responsive дизайн для корректного отображения на всех разрешениях

---

## [0.6.0] - 2026-01-10

> 🎯 **Фаза 6: Real-time обновления через SignalR** ✅

### 🚀 Добавлено
- [x] FileWatcherService с debounce 200ms и поддержкой множественных сессий
- [x] LogWatcherHub для real-time коммуникации (JoinSession, LeaveSession, SendNewLogs)
- [x] Управление lifecycle сессий: привязка к connectionId, автоматическое удаление при disconnect
- [x] ISessionStorage расширен методами BindConnectionAsync, UnbindConnectionAsync, GetSessionByConnectionAsync
- [x] InMemorySessionStorage: маппинг connectionId ↔ sessionId через ConcurrentDictionary
- [x] FileWatcherBackgroundService для автоматического запуска FileWatcher
- [x] Frontend SignalR клиент (@microsoft/signalr) с автореконнектом
- [x] Composable useFileWatcher для интеграции с Vue компонентами
- [x] LiveIndicator компонент с 4 состояниями (Live, Connecting, Reconnecting, Disconnected)
- [x] Интеграция real-time обновлений в LogTable и FilterPanel
- [x] 8 интеграционных тестов для LogWatcherHub
- [x] 3 нагрузочных теста (500 файлов × 100 записей, 100 одновременных обновлений, debounce)

### 🔧 Изменено
- [x] POST /api/files/{sessionId}/stop-watching теперь возвращает 204 No Content (вместо 501 Not Implemented)
- [x] InMemorySessionStorage добавлен маппинг connectionId для SignalR lifecycle управления
- [x] Cleanup callbacks теперь вызываются при удалении сессии через SignalR disconnect

### 📊 Статистика
- Общее количество тестов: 283 (было 240)
  - Infrastructure: 134 (было 113)
  - Application: 28 (без изменений)
  - Api: 121 (было 99)

---

## [1.0.0] - YYYY-MM-DD

> 🎉 **Первый стабильный релиз**

### 🚀 Добавлено
- [ ] Docker контейнеризация
- [ ] docker-compose.yml
- [ ] GitHub Actions CI/CD
- [ ] E2E тесты (Playwright)
- [ ] Полная документация

---

## Формат записей

### Типы изменений

- 🚀 **Добавлено** — новая функциональность
- 🔧 **Изменено** — изменения в существующей функциональности
- 🗑️ **Устарело** — функции, которые будут удалены
- 🗑️ **Удалено** — удалённые функции
- 🐛 **Исправлено** — исправления багов
- 🔒 **Безопасность** — исправления уязвимостей

### Пример записи

```markdown
## [1.2.3] - 2024-01-15

### 🚀 Добавлено
- Поддержка формата Serilog (#123)
- Темная тема (#124)

### 🐛 Исправлено
- Некорректный парсинг дат в формате ISO (#125)

### 🔒 Безопасность
- Обновлены зависимости с уязвимостями (#126)
```

---

[Unreleased]: https://github.com/YOUR_USERNAME/nLogMonitor/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v1.0.0
[0.9.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.9.0
[0.8.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.8.0
[0.7.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.7.0
[0.6.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.6.0
[0.5.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.5.0
[0.4.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.4.0
[0.3.1]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.3.1
[0.3.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.3.0
[0.2.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.2.0
[0.1.0]: https://github.com/YOUR_USERNAME/nLogMonitor/releases/tag/v0.1.0
