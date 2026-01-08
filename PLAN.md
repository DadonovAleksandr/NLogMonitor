# NLogMonitor - План разработки

## Содержание
1. [Обзор проекта](#обзор-проекта)
2. [Архитектура системы](#архитектура-системы)
3. [Технологический стек](#технологический-стек)
4. [Структура проекта](#структура-проекта)
5. [План разработки](#план-разработки)
6. [Docker конфигурация](#docker-конфигурация)

---

## Обзор проекта

**NLogMonitor** — кроссплатформенное приложение для просмотра и анализа NLog-логов. Работает в двух режимах:
- **Web-приложение** (Docker) — для разработки и серверного использования
- **Desktop-приложение** (Photino) — нативное окно с системными диалогами

### Ключевые возможности
- Открытие лог-файла через нативный диалог или drag-and-drop
- Открытие директории логов (автоматический выбор последнего по имени .log файла)
- Мониторинг изменений файла в реальном времени (FileSystemWatcher + SignalR)
- Парсинг стандартного формата NLog
- Фильтрация по уровню логирования (Trace, Debug, Info, Warn, Error, Fatal)
- Полнотекстовый поиск по сообщениям
- Пагинация и виртуализация для больших файлов
- Экспорт в JSON/CSV
- Список недавних файлов/директорий
- Client-side logging (сбор ошибок с фронтенда)

### Формат имени лог-файла
```
fileName="${var:logDirectory}/${shortdate}.log"
```
При открытии директории выбирается файл с **последним именем** (сортировка по имени в обратном порядке), например: `2024-01-15.log` будет выбран вместо `2024-01-14.log`.

---

## Архитектура системы

### Высокоуровневая архитектура

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    РЕЖИМ РАБОТЫ: Web (Docker) / Desktop (Photino)            │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                     Vue 3 SPA (WebView / Browser)                       │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  │  │
│  │  │  LogTable   │  │  Filters    │  │  Search     │  │ FileSelector  │  │  │
│  │  │  Component  │  │  Panel      │  │  Bar        │  │   Component   │  │  │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └───────┬───────┘  │  │
│  │         │                │                │                 │          │  │
│  │  ┌──────┴────────────────┴────────────────┴─────────────────┴────────┐ │  │
│  │  │                    State Management (Pinia)                        │ │  │
│  │  └───────────────────────────┬────────────────────────────────────────┘ │  │
│  └──────────────────────────────┼──────────────────────────────────────────┘  │
│                                 │ HTTP/REST + SignalR                         │
├─────────────────────────────────┼─────────────────────────────────────────────┤
│                      ASP.NET Core Web API (Embedded/Docker)                   │
│  ┌──────────────────────────────┴────────────────────────────────────────┐   │
│  │                     API Controllers                                    │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐  │   │
│  │  │  /api/logs  │  │ /api/files  │  │ /api/export │  │/api/client-  │  │   │
│  │  │   GET       │  │ open/watch  │  │    GET      │  │    logs      │  │   │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬───────┘  │   │
│  └─────────┼────────────────┼────────────────┼────────────────┼───────────┘   │
│            │                │                │                │               │
│  ┌─────────┴────────────────┴────────────────┴────────────────┴───────────┐   │
│  │                    Application Services                                 │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  │   │
│  │  │ LogParser   │  │ FileWatcher │  │ LogExporter │  │ RecentFiles   │  │   │
│  │  │  Service    │  │  Service    │  │  Service    │  │   Service     │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────────┘  │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────┐   │
│  │                     Infrastructure                                      │   │
│  │  ┌─────────────┐  ┌───────────────┐  ┌─────────────┐  ┌─────────────┐  │   │
│  │  │ InMemory    │  │ FileSystem    │  │  JSON File  │  │ NLog        │  │   │
│  │  │ Session     │  │   Watcher     │  │ (Recent)    │  │ Parser      │  │   │
│  │  └─────────────┘  └───────────────┘  └─────────────┘  └─────────────┘  │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────────────────┘
```

### Clean Architecture (слои)

```
┌─────────────────────────────────────────────────────────────────┐
│                      Presentation Layer                          │
│   Vue 3 SPA (client/)  │  ASP.NET API Controllers               │
├─────────────────────────────────────────────────────────────────┤
│                      Application Layer                           │
│   Interfaces (ILogParser, ISessionStorage, IFileWatcher...)     │
│   DTOs (LogEntryDto, FilterOptionsDto, PagedResultDto...)       │
│   Services (LogService, ExportService, RecentLogsService)       │
├─────────────────────────────────────────────────────────────────┤
│                        Domain Layer                              │
│   Entities: LogEntry, LogSession, LogLevel, RecentLogEntry      │
├─────────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                          │
│   NLogParser, InMemorySessionStorage, FileWatcherService        │
│   JsonExporter, CsvExporter, RecentLogsFileRepository           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Технологический стек

### Backend (ASP.NET Core)
| Компонент | Технология | Версия |
|-----------|------------|--------|
| Framework | ASP.NET Core | 9.0 |
| API Style | Controllers + Minimal API | - |
| DI Container | Microsoft.Extensions.DI | Built-in |
| Validation | FluentValidation | 11.x |
| Logging | NLog | 5.x |
| API Docs | Swagger/OpenAPI | Swashbuckle |
| Real-time | SignalR | Built-in |
| Rate Limiting | AspNetCoreRateLimit | 5.x |

### Frontend (Vue 3)
| Компонент | Технология | Версия |
|-----------|------------|--------|
| Framework | Vue | 3.x |
| Language | TypeScript | 5.x |
| Build Tool | Vite | 5.x |
| State | Pinia | 2.x |
| UI Library | shadcn-vue | latest |
| HTTP Client | Axios | 1.x |
| Table | TanStack Table Vue | 8.x |
| Icons | Lucide Vue | latest |
| Real-time | @microsoft/signalr | 8.x |

### Desktop (Photino)
| Компонент | Технология | Версия |
|-----------|------------|--------|
| Framework | Photino.NET | 3.x |
| WebView | Platform native | Edge/WebKit/GTK |
| File Dialogs | System.Windows.Forms | .NET 9 |
| IPC | JS ↔ .NET Bridge | Photino built-in |

### Инфраструктура
| Компонент | Технология |
|-----------|------------|
| Контейнеризация | Docker + docker-compose |
| Web Server | Nginx (reverse proxy) |
| Хранение сессий | In-Memory |
| Хранение недавних | JSON файл |

---

## Структура проекта

```
NLogMonitor/
├── src/
│   ├── NLogMonitor.Domain/           # Domain Layer
│   │   ├── Entities/
│   │   │   ├── LogEntry.cs
│   │   │   ├── LogSession.cs
│   │   │   ├── LogLevel.cs
│   │   │   └── RecentLogEntry.cs
│   │   └── NLogMonitor.Domain.csproj
│   │
│   ├── NLogMonitor.Application/      # Application Layer
│   │   ├── Interfaces/
│   │   │   ├── ILogParser.cs
│   │   │   ├── ILogService.cs
│   │   │   ├── ISessionStorage.cs
│   │   │   ├── IFileWatcherService.cs
│   │   │   ├── ILogExporter.cs
│   │   │   └── IRecentLogsRepository.cs
│   │   ├── DTOs/
│   │   │   ├── LogEntryDto.cs
│   │   │   ├── FilterOptionsDto.cs
│   │   │   ├── PagedResultDto.cs
│   │   │   ├── OpenFileResultDto.cs
│   │   │   ├── RecentLogDto.cs
│   │   │   └── ClientLogDto.cs
│   │   ├── Services/
│   │   │   ├── LogService.cs
│   │   │   ├── ExportService.cs
│   │   │   └── RecentLogsService.cs
│   │   └── NLogMonitor.Application.csproj
│   │
│   ├── NLogMonitor.Infrastructure/   # Infrastructure Layer
│   │   ├── Parsing/
│   │   │   └── NLogParser.cs
│   │   ├── Storage/
│   │   │   ├── InMemorySessionStorage.cs
│   │   │   └── RecentLogsFileRepository.cs
│   │   ├── FileSystem/
│   │   │   ├── FileWatcherService.cs
│   │   │   └── DirectoryScanner.cs
│   │   ├── Export/
│   │   │   ├── JsonExporter.cs
│   │   │   └── CsvExporter.cs
│   │   └── NLogMonitor.Infrastructure.csproj
│   │
│   ├── NLogMonitor.Api/              # Presentation Layer (API)
│   │   ├── Controllers/
│   │   │   ├── LogsController.cs
│   │   │   ├── FilesController.cs
│   │   │   ├── UploadController.cs
│   │   │   ├── ExportController.cs
│   │   │   ├── RecentController.cs
│   │   │   └── ClientLogsController.cs
│   │   ├── Hubs/
│   │   │   └── LogWatcherHub.cs
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   └── NLogMonitor.Api.csproj
│   │
│   └── NLogMonitor.Desktop/          # Photino Desktop Shell
│       ├── Program.cs
│       ├── Services/
│       │   └── NativeDialogService.cs
│       ├── wwwroot/                   # Vue build output
│       └── NLogMonitor.Desktop.csproj
│
├── client/                            # Frontend (Vue 3)
│   ├── src/
│   │   ├── components/
│   │   │   ├── ui/                   # shadcn-vue components
│   │   │   ├── LogTable/
│   │   │   ├── FilterPanel/
│   │   │   ├── SearchBar/
│   │   │   ├── FileSelector/
│   │   │   ├── RecentFiles/
│   │   │   └── ExportButton/
│   │   ├── stores/
│   │   │   ├── logStore.ts
│   │   │   ├── filterStore.ts
│   │   │   └── recentStore.ts
│   │   ├── api/
│   │   │   ├── client.ts
│   │   │   ├── logs.ts
│   │   │   ├── files.ts
│   │   │   └── signalr.ts
│   │   ├── services/
│   │   │   └── logger.ts             # Client-side logger
│   │   ├── composables/
│   │   │   ├── useLogs.ts
│   │   │   ├── useFileWatcher.ts
│   │   │   └── usePhotinoBridge.ts
│   │   ├── types/
│   │   │   └── index.ts
│   │   ├── App.vue
│   │   └── main.ts
│   ├── Dockerfile
│   ├── nginx.conf
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
├── docker-compose.yml
├── docker-compose.override.yml
├── NLogMonitor.sln
├── PLAN.md
├── CLAUDE.md
├── README.md
└── .gitignore
```

---

## План разработки

### Фаза 1: Базовая инфраструктура
- [ ] **1.1 Создание solution и проектов**
  - [ ] Создать NLogMonitor.sln
  - [ ] Создать NLogMonitor.Domain (classlib)
  - [ ] Создать NLogMonitor.Application (classlib)
  - [ ] Создать NLogMonitor.Infrastructure (classlib)
  - [ ] Создать NLogMonitor.Api (webapi)
  - [ ] Настроить project references между слоями

- [ ] **1.2 Domain Layer**
  - [ ] Создать LogEntry entity (Id, Timestamp, Level, Message, Logger, ProcessId, ThreadId, Exception)
  - [ ] Создать LogLevel enum (Trace, Debug, Info, Warn, Error, Fatal)
  - [ ] Создать LogSession entity (Id, FileName, FilePath, FileSize, CreatedAt, ExpiresAt, Entries)
  - [ ] Создать RecentLogEntry entity (Path, IsDirectory, OpenedAt)

- [ ] **1.3 Application Layer - интерфейсы**
  - [ ] Определить ILogParser interface (ParseAsync, CanParse)
  - [ ] Определить ISessionStorage interface (SaveAsync, GetAsync, DeleteAsync)
  - [ ] Определить ILogService interface (OpenFileAsync, GetLogsAsync)
  - [ ] Определить IFileWatcherService interface (StartWatching, StopWatching)
  - [ ] Определить ILogExporter interface (ExportAsync)
  - [ ] Определить IRecentLogsRepository interface (GetAllAsync, AddAsync)

- [ ] **1.4 Application Layer - DTOs**
  - [ ] Создать LogEntryDto
  - [ ] Создать FilterOptionsDto (SearchText, MinLevel, MaxLevel, FromDate, ToDate, Logger)
  - [ ] Создать PagedResultDto<T> (Items, TotalCount, Page, PageSize, TotalPages)
  - [ ] Создать OpenFileResultDto (SessionId, FileName, FilePath, TotalEntries, LevelCounts)
  - [ ] Создать RecentLogDto

- [ ] **1.5 Настройка API проекта**
  - [ ] Настроить DI в Program.cs
  - [ ] Добавить NuGet пакеты (FluentValidation, NLog, Swashbuckle)
  - [ ] Настроить Swagger
  - [ ] Настроить CORS для development
  - [ ] Настроить NLog
  - [ ] Настроить appsettings.json
  - [ ] Первый запуск и проверка Swagger UI

**Результат фазы:** Запускаемое API с Swagger UI, базовая структура проекта.

---

### Фаза 2: Парсинг и хранение логов
- [ ] **2.1 NLog Parser**
  - [ ] Реализовать NLogParser с Regex для стандартного формата
  - [ ] Добавить поддержку multi-line messages (stack traces)
  - [ ] Реализовать IAsyncEnumerable для streaming парсинга больших файлов
  - [ ] Обработка различных форматов дат
  - [ ] Обработка ошибок парсинга (логирование непарсируемых строк)

- [ ] **2.2 Session Storage**
  - [ ] Реализовать InMemorySessionStorage с ConcurrentDictionary
  - [ ] Добавить session expiration logic (TTL 1 час)
  - [ ] Реализовать cleanup timer для удаления просроченных сессий
  - [ ] Реализовать IDisposable для корректной остановки таймера

- [ ] **2.3 Directory Scanner**
  - [ ] Реализовать FindLastLogFileByName (сортировка по имени в обратном порядке)
  - [ ] Поддержка паттерна `*.log`
  - [ ] Обработка пустой директории

- [ ] **2.4 Application Service**
  - [ ] Реализовать LogService.OpenFileAsync (чтение + парсинг + сохранение сессии)
  - [ ] Реализовать LogService.GetLogsAsync (фильтрация + пагинация)

- [ ] **2.5 Тестирование**
  - [ ] Unit tests для NLogParser (разные форматы, multi-line, ошибки)
  - [ ] Unit tests для InMemorySessionStorage
  - [ ] Тестирование с реальными лог-файлами

**Результат фазы:** Работающий парсер логов с хранением в памяти.

---

### Фаза 3: API Endpoints
- [ ] **3.1 Files Controller**
  - [ ] POST /api/files/open — открытие файла по пути
  - [ ] POST /api/files/open-directory — открытие директории (выбор последнего по имени файла)
  - [ ] POST /api/files/{sessionId}/stop-watching — остановка мониторинга

- [ ] **3.2 Upload Controller (Web режим)**
  - [ ] POST /api/upload — загрузка файла через multipart/form-data
  - [ ] Валидация расширения (.log, .txt)
  - [ ] Лимит размера файла (100MB)

- [ ] **3.3 Logs Controller**
  - [ ] GET /api/logs/{sessionId} — получение логов с фильтрацией
  - [ ] Query параметры: search, minLevel, maxLevel, fromDate, toDate, logger, page, pageSize
  - [ ] Валидация параметров (FluentValidation)

- [ ] **3.4 Export Controller**
  - [ ] GET /api/export/{sessionId} — экспорт с query параметром format (json/csv)
  - [ ] Реализовать JsonExporter
  - [ ] Реализовать CsvExporter
  - [ ] Поддержка фильтров при экспорте

- [ ] **3.5 Recent Controller**
  - [ ] GET /api/recent — список недавних файлов/директорий
  - [ ] Реализовать RecentLogsFileRepository (JSON файл в AppData)
  - [ ] Лимит на количество записей (10-20)

- [ ] **3.6 Middleware и обработка ошибок**
  - [ ] ExceptionHandlingMiddleware (unified error response)
  - [ ] Логирование ошибок через NLog

- [ ] **3.7 Документация и тестирование**
  - [ ] XML comments для Swagger
  - [ ] Integration tests для контроллеров
  - [ ] Тестирование через Postman/curl

**Результат фазы:** Полнофункциональное REST API для работы с логами.

---

### Фаза 4: Frontend базовый (Vue 3)
- [ ] **4.1 Инициализация проекта**
  - [ ] Создать Vite + Vue 3 + TypeScript проект
  - [ ] Настроить path aliases (@/)
  - [ ] Установить и настроить Tailwind CSS
  - [ ] Установить shadcn-vue и инициализировать компоненты

- [ ] **4.2 Типы и API клиент**
  - [ ] Определить TypeScript types (LogEntry, PagedResult, FilterOptions, etc.)
  - [ ] Создать axios client с baseURL
  - [ ] Создать API методы (uploadFile, getLogs, openFile, openDirectory, exportLogs)

- [ ] **4.3 State Management (Pinia)**
  - [ ] Создать logStore (sessionId, fileName, logs, totalCount, page, pageSize, isLoading, error)
  - [ ] Создать filterStore (searchText, minLevel, maxLevel, fromDate, toDate, logger)
  - [ ] Создать recentStore (recentFiles)

- [ ] **4.4 FileSelector компонент**
  - [ ] Кнопка "Выбрать файл" (input type=file)
  - [ ] Drag-and-drop зона для загрузки
  - [ ] Валидация расширения файла
  - [ ] Loading state при загрузке

- [ ] **4.5 LogTable компонент**
  - [ ] Базовая таблица с TanStack Table
  - [ ] Колонки: Time, Level, Message, Logger
  - [ ] Цветовая индикация уровней логирования
  - [ ] Отображение состояния "нет данных"

- [ ] **4.6 Интеграция и проверка**
  - [ ] App.vue с базовой разметкой
  - [ ] Проверка загрузки файла → отображение логов
  - [ ] Настройка proxy в vite.config.ts для API

**Результат фазы:** Работающее Vue приложение с загрузкой файла и отображением логов.

---

### Фаза 5: UI компоненты
- [ ] **5.1 FilterPanel компонент**
  - [ ] Кнопки-фильтры по уровням (Trace, Debug, Info, Warn, Error, Fatal)
  - [ ] Подсчёт количества записей каждого уровня
  - [ ] Активное/неактивное состояние фильтров
  - [ ] Цветовая индикация уровней

- [ ] **5.2 SearchBar компонент**
  - [ ] Input с placeholder
  - [ ] Debounce 300ms
  - [ ] Иконка поиска
  - [ ] Кнопка очистки

- [ ] **5.3 Pagination компонент**
  - [ ] Кнопки Previous/Next
  - [ ] Выбор размера страницы (50, 100, 200)
  - [ ] Отображение текущей страницы и общего количества
  - [ ] Прямой переход на страницу (опционально)

- [ ] **5.4 ExportButton компонент**
  - [ ] Dropdown с выбором формата (JSON/CSV)
  - [ ] Скачивание файла
  - [ ] Loading state

- [ ] **5.5 RecentFiles компонент**
  - [ ] Список недавних файлов/директорий
  - [ ] Иконки для файла/директории
  - [ ] Клик для повторного открытия
  - [ ] Отображение в начальном экране (когда файл не загружен)

- [ ] **5.6 Улучшение UX**
  - [ ] Loading spinner для таблицы
  - [ ] Error toast/alert
  - [ ] Empty state (нет результатов поиска)
  - [ ] Responsive design (адаптивная верстка)

**Результат фазы:** Полнофункциональный UI с фильтрацией, поиском, пагинацией и экспортом.

---

### Фаза 6: Real-time обновления
- [ ] **6.1 Backend - FileWatcher**
  - [ ] Реализовать FileWatcherService с FileSystemWatcher
  - [ ] Debounce событий изменения файла (200ms)
  - [ ] Отслеживание нескольких сессий одновременно
  - [ ] Корректная остановка при закрытии сессии

- [ ] **6.2 Backend - SignalR Hub**
  - [ ] Создать LogWatcherHub
  - [ ] Метод JoinSession(sessionId) — подписка на обновления
  - [ ] Метод LeaveSession(sessionId) — отписка
  - [ ] Событие NewLogs — отправка новых записей клиентам
  - [ ] Настройка SignalR в Program.cs

- [ ] **6.3 Frontend - SignalR клиент**
  - [ ] Установить @microsoft/signalr
  - [ ] Создать signalr.ts — connection manager
  - [ ] Создать composable useFileWatcher(sessionId)
  - [ ] Автоматическое переподключение при разрыве

- [ ] **6.4 Интеграция**
  - [ ] При открытии файла — подписка на обновления
  - [ ] При получении NewLogs — добавление в store
  - [ ] Индикатор "Live" в UI
  - [ ] При закрытии/смене файла — отписка

- [ ] **6.5 Тестирование**
  - [ ] Тест: изменение файла → появление новых записей
  - [ ] Тест: переподключение при разрыве соединения
  - [ ] Нагрузочное тестирование (частые изменения файла)

**Результат фазы:** Автоматическое обновление логов при изменении файла.

---

### Фаза 7: Docker конфигурация
- [ ] **7.1 Backend Dockerfile**
  - [ ] Multi-stage build (build → publish → runtime)
  - [ ] Оптимизация слоёв для кэширования
  - [ ] Non-root user для безопасности
  - [ ] Health check endpoint

- [ ] **7.2 Frontend Dockerfile**
  - [ ] Multi-stage build (build → nginx)
  - [ ] Оптимизированный nginx.conf
  - [ ] Gzip compression
  - [ ] SPA routing (try_files)

- [ ] **7.3 docker-compose.yml**
  - [ ] Сервис api (backend)
  - [ ] Сервис client (frontend + nginx)
  - [ ] Volumes для логов и данных
  - [ ] Переменные окружения

- [ ] **7.4 docker-compose.override.yml (development)**
  - [ ] Hot reload для backend (watch mode)
  - [ ] Vite dev server для frontend
  - [ ] Порты для отладки

- [ ] **7.5 Документация**
  - [ ] Инструкции по запуску в README
  - [ ] Переменные окружения (.env.example)

**Результат фазы:** Приложение запускается одной командой `docker-compose up`.

---

### Фаза 8: Client-side Logging
- [ ] **8.1 Backend - ClientLogsController**
  - [ ] POST /api/client-logs — приём batch логов
  - [ ] Валидация Level (обязательное поле)
  - [ ] Нормализация алиасов (warning→warn, fatal/critical→error)
  - [ ] Rate limiting для защиты от спама

- [ ] **8.2 Backend - логирование**
  - [ ] Запись в общий лог-файл с префиксом [CLIENT]
  - [ ] Structured logging с контекстом (userId, version, url)
  - [ ] Санитизация входных данных

- [ ] **8.3 Frontend - ClientLogger service**
  - [ ] Методы: trace, debug, info, warn, error, exception
  - [ ] Буферизация логов (batchSize: 10)
  - [ ] Автоматический flush по таймеру (5 сек)
  - [ ] Retry с exponential backoff (maxRetries: 3)

- [ ] **8.4 Frontend - глобальный контекст**
  - [ ] setGlobalContext({ userId, version, sessionId })
  - [ ] Автоматическое добавление url, userAgent

- [ ] **8.5 Frontend - error handlers**
  - [ ] window.onerror — глобальные ошибки JS
  - [ ] window.onunhandledrejection — необработанные Promise
  - [ ] Vue Error Boundary (errorCaptured hook)

- [ ] **8.6 Тестирование**
  - [ ] Unit tests для ClientLogger
  - [ ] Integration tests для /api/client-logs
  - [ ] Тест rate limiting

**Результат фазы:** Ошибки с фронтенда автоматически отправляются на сервер.

---

### Фаза 9: Photino Desktop
- [ ] **9.1 Создание Desktop проекта**
  - [ ] Создать NLogMonitor.Desktop (console → winexe)
  - [ ] Добавить Photino.NET NuGet пакет
  - [ ] Reference на NLogMonitor.Api

- [ ] **9.2 Program.cs - основа**
  - [ ] Запуск embedded ASP.NET Core в фоновом потоке
  - [ ] Создание PhotinoWindow
  - [ ] Загрузка index.html (production) или localhost:5173 (dev)
  - [ ] RegisterWebMessageReceivedHandler для IPC

- [ ] **9.3 Нативные диалоги**
  - [ ] ShowOpenFileDialog (OpenFileDialog)
  - [ ] ShowOpenFolderDialog (FolderBrowserDialog)
  - [ ] Фильтры файлов (*.log)

- [ ] **9.4 JS ↔ .NET Bridge**
  - [ ] Message handler для команд (openFile, openFolder, isDesktop)
  - [ ] JSON сериализация запросов/ответов
  - [ ] Отправка результата обратно в WebView

- [ ] **9.5 Frontend - usePhotinoBridge composable**
  - [ ] Определение режима (isDesktop)
  - [ ] showOpenFileDialog() → Promise<string | null>
  - [ ] showOpenFolderDialog() → Promise<string | null>
  - [ ] Fallback на web-версию если не desktop

- [ ] **9.6 FileSelector - режимы работы**
  - [ ] Web: drag-and-drop + input[type=file]
  - [ ] Desktop: нативные кнопки "Открыть файл" / "Открыть директорию"
  - [ ] Переключение на основе isDesktop

- [ ] **9.7 Сборка и публикация**
  - [ ] npm run build → client/dist
  - [ ] Копирование dist в wwwroot
  - [ ] dotnet publish -c Release -r win-x64 --self-contained
  - [ ] Тестирование собранного приложения

**Результат фазы:** Desktop приложение с нативными диалогами.

---

### Фаза 10: Оптимизация и тестирование
- [ ] **10.1 Performance - Frontend**
  - [ ] Virtual scrolling для больших списков (@tanstack/vue-virtual)
  - [ ] Lazy loading компонентов (defineAsyncComponent)
  - [ ] Оптимизация bundle size (analyze + tree shaking)
  - [ ] Memoization для вычисляемых значений

- [ ] **10.2 Performance - Backend**
  - [ ] IMemoryCache для частых запросов
  - [ ] Streaming для экспорта больших файлов
  - [ ] Оптимизация парсера (Span<char>)

- [ ] **10.3 UX улучшения**
  - [ ] Skeleton loaders
  - [ ] Smooth animations
  - [ ] Keyboard shortcuts (Ctrl+F для поиска)
  - [ ] Dark mode (опционально)

- [ ] **10.4 Тестирование**
  - [ ] E2E tests (Playwright)
  - [ ] Performance testing с большими файлами (100MB+, 1M+ записей)
  - [ ] Cross-browser testing
  - [ ] Cross-platform testing Desktop (Windows, Linux, macOS)

- [ ] **10.5 Документация**
  - [ ] README.md с инструкциями
  - [ ] Changelog
  - [ ] Примеры использования API

- [ ] **10.6 Финализация**
  - [ ] Security review
  - [ ] Code cleanup
  - [ ] Release v1.0.0

**Результат фазы:** Production-ready приложение.

---

## Docker конфигурация

### docker-compose.yml
```yaml
services:
  api:
    build:
      context: .
      dockerfile: src/NLogMonitor.Api/Dockerfile
    ports:
      - "5000:8080"
    volumes:
      - ./logs:/app/logs
      - ./data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  client:
    build:
      context: ./client
      dockerfile: Dockerfile
    ports:
      - "80:80"
    depends_on:
      - api
```

### Команды запуска
```bash
# Production
docker-compose up -d --build

# Development (с hot reload)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up

# Только backend
docker-compose up api

# Просмотр логов
docker-compose logs -f api
```

---

## NLog формат

### Поддерживаемый формат по умолчанию
```
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
```

### Пример записи
```
2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1
2024-01-15 10:30:46.5678|ERROR|Unhandled exception|MyApp.Service|1234|5
System.NullReferenceException: Object reference not set...
   at MyApp.Service.Process()
   at MyApp.Program.Main()
```

---

## Полезные ссылки

- [Vue 3 Docs](https://vuejs.org/)
- [Pinia](https://pinia.vuejs.org/)
- [shadcn-vue](https://www.shadcn-vue.com/)
- [TanStack Table Vue](https://tanstack.com/table/latest/docs/framework/vue/vue-table)
- [Photino.NET](https://www.tryphotino.io/)
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [SignalR](https://docs.microsoft.com/aspnet/core/signalr)
- [Docker Compose](https://docs.docker.com/compose/)
