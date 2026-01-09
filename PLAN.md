# nLogMonitor - План разработки

## Содержание
1. [Обзор проекта](#обзор-проекта)
2. [Архитектура системы](#архитектура-системы)
3. [Технологический стек](#технологический-стек)
4. [Структура проекта](#структура-проекта)
5. [План разработки](#план-разработки)
6. [Docker конфигурация](#docker-конфигурация)
7. [Планируемые доработки (Roadmap)](#планируемые-доработки-roadmap)

---

## Обзор проекта

**nLogMonitor** — кроссплатформенное приложение для просмотра и анализа NLog-логов. Работает в двух режимах:
- **Web-приложение** (Docker) — для разработки и серверного использования
- **Desktop-приложение** (Photino) — нативное окно с системными диалогами

### Ключевые возможности
- Открытие лог-файла через нативный диалог (Web: загрузка файла, Desktop: системный диалог выбора)
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
| Framework | ASP.NET Core | 10.0 |
| API Style | Controllers + Minimal API | - |
| DI Container | Microsoft.Extensions.DI | Built-in |
| Validation | FluentValidation | 11.x |
| Logging | NLog | 5.x |
| API Docs | Swagger/OpenAPI | Swashbuckle |
| Real-time | SignalR | Built-in |
| Rate Limiting | AspNetCoreRateLimit | 5.x |
| Testing | NUnit | 3.x |
| Mocking | Moq | 4.x |
| Code Coverage | coverlet | 6.x |

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
| File Dialogs | Photino.NET built-in | 3.x |
| IPC | JS ↔ .NET Bridge | Photino built-in |

### Инфраструктура
| Компонент | Технология |
|-----------|------------|
| Контейнеризация | Docker + docker-compose |
| Web Server | Nginx (reverse proxy) |
| Хранение сессий | In-Memory |
| Хранение недавних | JSON файл |

### Стратегия работы с файлами

**Web-режим (Docker)** — только upload через браузер:

| Параметр | Значение |
|----------|----------|
| Endpoint | `POST /api/upload` |
| Лимит размера | 100 MB |
| Хранение | `/app/temp/{sessionId}/` (внутри контейнера) |
| Очистка | Вместе с сессией (SignalR disconnect или fallback TTL) |
| Persist | Нет — файлы теряются при перезапуске контейнера |

**Desktop-режим (Photino)** — прямой доступ к файловой системе:

| Параметр | Значение |
|----------|----------|
| Endpoints | `POST /api/files/open`, `POST /api/files/open-directory` |
| Диалоги | Photino built-in (кроссплатформенные) |
| Ограничения | Нет лимита размера, доступ к любым путям |

---

## Структура проекта

```
nLogMonitor/
├── src/
│   ├── nLogMonitor.Domain/           # Domain Layer
│   │   ├── Entities/
│   │   │   ├── LogEntry.cs
│   │   │   ├── LogSession.cs
│   │   │   ├── LogLevel.cs
│   │   │   └── RecentLogEntry.cs
│   │   └── nLogMonitor.Domain.csproj
│   │
│   ├── nLogMonitor.Application/      # Application Layer
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
│   │   └── nLogMonitor.Application.csproj
│   │
│   ├── nLogMonitor.Infrastructure/   # Infrastructure Layer
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
│   │   └── nLogMonitor.Infrastructure.csproj
│   │
│   ├── nLogMonitor.Api/              # Presentation Layer (API)
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
│   │   └── nLogMonitor.Api.csproj
│   │
│   └── nLogMonitor.Desktop/          # Photino Desktop Shell
│       ├── Program.cs
│       ├── Services/
│       │   └── NativeDialogService.cs
│       ├── wwwroot/                   # Vue build output
│       └── nLogMonitor.Desktop.csproj
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
│   ├── nLogMonitor.Domain.Tests/
│   ├── nLogMonitor.Application.Tests/
│   ├── nLogMonitor.Infrastructure.Tests/
│   └── nLogMonitor.Api.Tests/
│
├── docker-compose.yml
├── docker-compose.override.yml
├── nLogMonitor.sln
├── PLAN.md
├── CLAUDE.md
├── README.md
└── .gitignore
```

---

## План разработки

### Фаза 1: Базовая инфраструктура
- [x] **1.1 Создание solution и проектов**
  - [x] Создать nLogMonitor.sln — корневой solution файл для всего проекта
  - [x] Создать nLogMonitor.Domain (classlib) — слой доменных сущностей без внешних зависимостей
  - [x] Создать nLogMonitor.Application (classlib) — слой бизнес-логики, интерфейсов и DTO
  - [x] Создать nLogMonitor.Infrastructure (classlib) — реализации интерфейсов, работа с файлами и хранилищем
  - [x] Создать nLogMonitor.Api (webapi) — ASP.NET Core Web API проект
  - [x] Настроить project references между слоями — Domain ← Application ← Infrastructure ← Api

- [x] **1.2 Domain Layer**
  - [x] Создать LogEntry entity — основная сущность записи лога с полями: Id, Timestamp, Level, Message, Logger, ProcessId, ThreadId, Exception
  - [x] Создать LogLevel enum — уровни логирования: Trace, Debug, Info, Warn, Error, Fatal
  - [x] Создать LogSession entity — сессия работы с файлом: Id, FileName, FilePath, FileSize, CreatedAt, ExpiresAt, Entries
  - [x] Создать RecentLogEntry entity — запись истории открытых файлов: Path, IsDirectory, OpenedAt

- [x] **1.3 Application Layer - интерфейсы**
  - [x] Определить ILogParser interface — контракт парсинга логов: ParseAsync для потокового чтения, CanParse для проверки формата
  - [x] Определить ISessionStorage interface — хранилище сессий в памяти: SaveAsync, GetAsync, DeleteAsync
  - [x] Определить ILogService interface — основной сервис: OpenFileAsync для открытия файла, GetLogsAsync для получения с фильтрацией
  - [x] Определить IFileWatcherService interface — мониторинг изменений файла: StartWatching, StopWatching
  - [x] Определить ILogExporter interface — экспорт логов: ExportAsync в различные форматы
  - [x] Определить IRecentLogsRepository interface — работа с историей: GetAllAsync, AddAsync

- [x] **1.4 Application Layer - DTOs**
  - [x] Создать LogEntryDto — DTO для передачи записи лога на фронтенд
  - [x] Создать FilterOptionsDto — параметры фильтрации: SearchText, MinLevel, MaxLevel, FromDate, ToDate, Logger
  - [x] Создать PagedResultDto<T> — обёртка для пагинации: Items, TotalCount, Page, PageSize, TotalPages
  - [x] Создать OpenFileResultDto — результат открытия файла: SessionId, FileName, FilePath, TotalEntries, LevelCounts
  - [x] Создать RecentLogDto — DTO для отображения недавних файлов

- [x] **1.5 Настройка API проекта**
  - [x] Настроить DI в Program.cs — регистрация сервисов и интерфейсов через Microsoft.Extensions.DependencyInjection
  - [x] Добавить NuGet пакеты — FluentValidation для валидации, NLog для логирования, Swashbuckle для Swagger
  - [x] Настроить Swagger — автогенерация документации API на /swagger
  - [x] Настроить CORS для development — разрешить запросы с localhost:5173 (Vite dev server)
  - [x] Настроить NLog — конфигурация логирования в файл и консоль
  - [x] Настроить appsettings.json — параметры приложения: TTL сессии, пути к файлам, лимиты
  - [x] Первый запуск и проверка Swagger UI — убедиться что API запускается и Swagger доступен

**Результат фазы:** Запускаемое API с Swagger UI, базовая структура проекта. ✅ ЗАВЕРШЕНО

---

### Фаза 2: Парсинг и хранение логов
- [ ] **2.1 NLog Parser**
  - [ ] Реализовать быстрый парсер для однострочных записей — поиск разделителей `|` с начала строки, без regex для производительности
  - [ ] Реализовать парсер для многострочных записей — поиск разделителей `|` **с конца** строки (logger, processid, threadid фиксированы в конце)
  - [ ] Реализовать определение начала новой записи по дате — regex `^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}` для определения границ записей
  - [ ] Реализовать накопительный буфер для многострочных сообщений — StringBuilder для сбора строк до успешного парсинга
  - [ ] Реализовать IAsyncEnumerable для streaming парсинга больших файлов — построчное чтение без загрузки всего файла в память
  - [ ] Использовать Span<char>/ReadOnlySpan<char> для zero-allocation парсинга — минимизация аллокаций памяти
  - [ ] Добавить fallback на Regex для нестандартных случаев — запасной вариант если быстрый парсер не справился
  - [ ] Обработка ошибок парсинга — логирование непарсируемых строк без прерывания обработки

- [ ] **2.2 Session Storage**
  - [ ] Реализовать InMemorySessionStorage с ConcurrentDictionary — потокобезопасное хранилище сессий
  - [ ] Добавить fallback TTL (5 минут) — страховка для потерянных сессий (основное управление через SignalR в Фазе 6)
  - [ ] Реализовать sliding expiration — продление TTL при каждом обращении к сессии
  - [ ] Реализовать cleanup timer для удаления просроченных сессий — фоновая очистка каждые 1 минуту
  - [ ] Реализовать IDisposable для корректной остановки таймера — освобождение ресурсов при завершении приложения
  - [ ] Подготовить методы для привязки к SignalR — BindConnectionAsync, UnbindConnectionAsync, GetSessionByConnectionAsync (реализация в Фазе 6)

- [ ] **2.3 Directory Scanner**
  - [ ] Реализовать FindLastLogFileByName — поиск файла с последним именем (сортировка по имени в обратном порядке)
  - [ ] Поддержка паттерна `*.log` — фильтрация только лог-файлов в директории
  - [ ] Обработка пустой директории — возврат информативной ошибки при отсутствии файлов

- [ ] **2.4 Application Service**
  - [ ] Реализовать LogService.OpenFileAsync — открытие файла: чтение → парсинг → создание сессии → сохранение
  - [ ] Реализовать LogService.GetLogsAsync — получение логов с применением фильтров и пагинации

- [ ] **2.5 Тестирование**
  - [ ] Unit tests для однострочного парсера — тесты стандартных записей, записей с пробелами вокруг `|`
  - [ ] Unit tests для многострочного парсера — тесты stack traces, сообщений с `|` и `\n` внутри
  - [ ] Unit tests для определения границ записей — проверка корректного разделения записей по дате
  - [ ] Unit tests для InMemorySessionStorage — тесты CRUD операций и TTL логики
  - [ ] Тестирование с реальными лог-файлами из nLogViewer — интеграционные тесты на примерах реальных логов

**Результат фазы:** Работающий парсер логов с хранением в памяти.

---

### Фаза 3: API Endpoints
- [ ] **3.1 Files Controller (Desktop-only)**
  - [ ] POST /api/files/open — открытие файла по абсолютному пути (**только Desktop**)
  - [ ] POST /api/files/open-directory — открытие директории с автоматическим выбором последнего по имени .log файла (**только Desktop**)
  - [ ] POST /api/files/{sessionId}/stop-watching — остановка мониторинга изменений файла для указанной сессии

- [ ] **3.2 Upload Controller (Web режим)**
  - [ ] POST /api/upload — загрузка файла через multipart/form-data для Web-версии
  - [ ] Валидация расширения (.log, .txt) — проверка допустимых типов файлов
  - [ ] Лимит размера файла (100MB) — защита от загрузки слишком больших файлов
  - [ ] Сохранение в `/app/temp/{sessionId}/` — временное хранение загруженного файла
  - [ ] Очистка временных файлов при удалении сессии — удаление папки `{sessionId}` вместе с файлом

- [ ] **3.3 Logs Controller**
  - [ ] GET /api/logs/{sessionId} — получение логов сессии с поддержкой фильтрации и пагинации
  - [ ] Query параметры — search, minLevel, maxLevel, fromDate, toDate, logger, page, pageSize
  - [ ] Валидация параметров (FluentValidation) — проверка корректности входных данных

- [ ] **3.4 Export Controller**
  - [ ] GET /api/export/{sessionId} — экспорт логов с query параметром format (json/csv)
  - [ ] Реализовать JsonExporter — форматирование логов в JSON с поддержкой streaming
  - [ ] Реализовать CsvExporter — форматирование логов в CSV с корректным экранированием
  - [ ] Поддержка фильтров при экспорте — применение тех же фильтров что и при просмотре

- [ ] **3.5 Recent Controller**
  - [ ] GET /api/recent — получение списка недавно открытых файлов и директорий
  - [ ] Реализовать RecentLogsFileRepository — хранение истории в JSON файле в AppData
  - [ ] Лимит на количество записей (10-20) — автоматическое удаление старых записей

- [ ] **3.6 Middleware и обработка ошибок**
  - [ ] ExceptionHandlingMiddleware — единообразный формат ошибок {error, message, details}
  - [ ] Логирование ошибок через NLog — запись stack trace и контекста в лог-файл

- [ ] **3.7 Документация и тестирование**
  - [ ] XML comments для Swagger — описание эндпоинтов, параметров и моделей
  - [ ] Integration tests для контроллеров — тесты полного цикла запрос-ответ
  - [ ] Тестирование через Postman/curl — ручная проверка всех эндпоинтов

**Результат фазы:** Полнофункциональное REST API для работы с логами.

---

### Фаза 4: Frontend базовый (Vue 3)
- [ ] **4.1 Инициализация проекта**
  - [ ] Создать Vite + Vue 3 + TypeScript проект — `npm create vite@latest client -- --template vue-ts`
  - [ ] Настроить path aliases (@/) — удобные импорты вместо относительных путей
  - [ ] Установить и настроить Tailwind CSS — utility-first CSS фреймворк для стилизации
  - [ ] Установить shadcn-vue и инициализировать компоненты — готовые UI компоненты на базе Radix

- [ ] **4.2 Типы и API клиент**
  - [ ] Определить TypeScript types — интерфейсы LogEntry, PagedResult, FilterOptions, OpenFileResult
  - [ ] Создать axios client с baseURL — настройка базового URL и interceptors
  - [ ] Создать API методы — uploadFile, getLogs, openFile, openDirectory, exportLogs, getRecent

- [ ] **4.3 State Management (Pinia)**
  - [ ] Создать logStore — состояние: sessionId, fileName, logs, totalCount, page, pageSize, isLoading, error
  - [ ] Создать filterStore — фильтры: searchText, minLevel, maxLevel, fromDate, toDate, logger
  - [ ] Создать recentStore — список недавно открытых файлов и директорий

- [ ] **4.4 FileSelector компонент**
  - [ ] Кнопка "Выбрать файл" (input type=file) — стилизованный input для выбора файла
  - [ ] Валидация расширения файла — проверка .log и .txt перед загрузкой
  - [ ] Loading state при загрузке — отображение спиннера и блокировка повторной загрузки

- [ ] **4.5 LogTable компонент**
  - [ ] Базовая таблица с TanStack Table — настройка колонок и рендеринга строк
  - [ ] Колонки: Time, Level, Message, Logger — основные поля записи лога
  - [ ] Цветовая индикация уровней логирования — красный для Error, жёлтый для Warn и т.д.
  - [ ] Отображение состояния "нет данных" — placeholder при пустом списке

- [ ] **4.6 Интеграция и проверка**
  - [ ] App.vue с базовой разметкой — layout приложения: header, main, footer
  - [ ] Проверка загрузки файла → отображение логов — end-to-end тест основного flow
  - [ ] Настройка proxy в vite.config.ts для API — проксирование /api на localhost:5000

**Результат фазы:** Работающее Vue приложение с загрузкой файла и отображением логов.

---

### Фаза 5: UI компоненты
- [ ] **5.1 FilterPanel компонент**
  - [ ] Кнопки-фильтры по уровням (Trace, Debug, Info, Warn, Error, Fatal) — toggle-кнопки для включения/выключения уровней
  - [ ] Подсчёт количества записей каждого уровня — badge с числом на каждой кнопке
  - [ ] Активное/неактивное состояние фильтров — визуальное различие выбранных и невыбранных
  - [ ] Цветовая индикация уровней — соответствие цветов стандартам NLog

- [ ] **5.2 SearchBar компонент**
  - [ ] Input с placeholder — подсказка "Поиск по сообщениям..."
  - [ ] Debounce 300ms — задержка запроса для оптимизации производительности
  - [ ] Иконка поиска — визуальный индикатор назначения поля
  - [ ] Кнопка очистки — быстрый сброс поискового запроса

- [ ] **5.3 Pagination компонент**
  - [ ] Кнопки Previous/Next — навигация между страницами
  - [ ] Выбор размера страницы (50, 100, 200) — dropdown для настройки количества записей
  - [ ] Отображение текущей страницы и общего количества — "Страница 1 из 100"
  - [ ] Прямой переход на страницу (опционально) — input для ввода номера страницы

- [ ] **5.4 ExportButton компонент**
  - [ ] Dropdown с выбором формата (JSON/CSV) — меню выбора формата экспорта
  - [ ] Скачивание файла — автоматическое сохранение через download attribute
  - [ ] Loading state — индикатор генерации файла

- [ ] **5.5 RecentFiles компонент**
  - [ ] Список недавних файлов/директорий — отображение истории с датой открытия
  - [ ] Иконки для файла/директории — визуальное различие типа записи
  - [ ] Клик для повторного открытия — быстрый доступ к ранее открытым файлам
  - [ ] Отображение в начальном экране — показ когда файл не загружен

- [ ] **5.6 Улучшение UX**
  - [ ] Loading spinner для таблицы — скелетон или спиннер при загрузке данных
  - [ ] Error toast/alert — уведомления об ошибках через shadcn Toast
  - [ ] Empty state (нет результатов поиска) — информативный placeholder с иконкой
  - [ ] Responsive design (адаптивная верстка) — корректное отображение на мобильных

**Результат фазы:** Полнофункциональный UI с фильтрацией, поиском, пагинацией и экспортом.

---

### Фаза 6: Real-time обновления
- [ ] **6.1 Backend - FileWatcher**
  - [ ] Реализовать FileWatcherService с FileSystemWatcher — мониторинг изменений лог-файла
  - [ ] Debounce событий изменения файла (200ms) — предотвращение множественных срабатываний при записи
  - [ ] Отслеживание нескольких сессий одновременно — Dictionary<sessionId, FileWatcher>
  - [ ] Корректная остановка при закрытии сессии — освобождение FileSystemWatcher и ресурсов

- [ ] **6.2 Backend - SignalR Hub**
  - [ ] Создать LogWatcherHub — хаб для real-time коммуникации с клиентами
  - [ ] Метод JoinSession(sessionId) — добавление клиента в группу сессии + привязка connectionId к сессии
  - [ ] Метод LeaveSession(sessionId) — удаление клиента из группы + отвязка от сессии
  - [ ] Событие NewLogs — отправка новых записей всем подписчикам группы
  - [ ] Настройка SignalR в Program.cs — регистрация middleware и маршрутов

- [ ] **6.3 Backend - Управление жизненным циклом сессий через SignalR**
  - [ ] Реализовать OnDisconnectedAsync — удаление сессии при разрыве соединения (закрытие вкладки)
  - [ ] Реализовать BindConnectionAsync в ISessionStorage — привязка connectionId к sessionId
  - [ ] Реализовать UnbindConnectionAsync — отвязка при явном LeaveSession
  - [ ] Реализовать GetSessionByConnectionAsync — получение sessionId по connectionId
  - [ ] Хранение маппинга connectionId ↔ sessionId в ConcurrentDictionary
  - [ ] Логика: пока SignalR connected → сессия живёт бесконечно; disconnected → сессия удаляется
  - [ ] Fallback: если SignalR не подключен (старый клиент) — используется TTL из Фазы 2

- [ ] **6.4 Frontend - SignalR клиент**
  - [ ] Установить @microsoft/signalr — официальный npm пакет для SignalR
  - [ ] Создать signalr.ts — singleton менеджер подключения с автореконнектом
  - [ ] Создать composable useFileWatcher(sessionId) — Vue composable для подписки на обновления
  - [ ] Автоматическое переподключение при разрыве — exponential backoff при потере соединения

- [ ] **6.5 Интеграция**
  - [ ] При открытии файла — подписка на обновления — вызов JoinSession после успешного открытия
  - [ ] При получении NewLogs — добавление в store — append новых записей без перезагрузки
  - [ ] Индикатор "Live" в UI — зелёный badge показывающий активное соединение
  - [ ] При закрытии/смене файла — отписка — вызов LeaveSession и очистка состояния

- [ ] **6.6 Тестирование**
  - [ ] Тест: изменение файла → появление новых записей — проверка полного цикла обновления
  - [ ] Тест: переподключение при разрыве соединения — симуляция потери сети
  - [ ] Тест: закрытие вкладки → удаление сессии — проверка lifecycle через SignalR
  - [ ] Нагрузочное тестирование — проверка при частых изменениях файла (10+ в секунду)

**Результат фазы:** Автоматическое обновление логов при изменении файла. Сессии живут пока открыта вкладка (SignalR connected), удаляются при закрытии.

---

### Фаза 7: Docker конфигурация
- [ ] **7.1 Backend Dockerfile**
  - [ ] Multi-stage build (build → publish → runtime) — минимизация размера итогового образа
  - [ ] Оптимизация слоёв для кэширования — копирование csproj перед restore
  - [ ] Non-root user для безопасности — запуск от непривилегированного пользователя
  - [ ] Health check endpoint — /health для проверки работоспособности контейнера

- [ ] **7.2 Frontend Dockerfile**
  - [ ] Multi-stage build (build → nginx) — сборка и статический сервер в одном образе
  - [ ] Оптимизированный nginx.conf — настройка кэширования и безопасности
  - [ ] Gzip compression — сжатие статики для уменьшения трафика
  - [ ] SPA routing (try_files) — перенаправление всех запросов на index.html

- [ ] **7.3 docker-compose.yml**
  - [ ] Сервис api (backend) — ASP.NET Core на порту 5000
  - [ ] Сервис client (frontend + nginx) — статика на порту 80
  - [ ] Volumes для логов и данных — монтирование папок хоста для персистентности
  - [ ] Переменные окружения — ASPNETCORE_ENVIRONMENT и другие настройки

- [ ] **7.4 docker-compose.override.yml (development)**
  - [ ] Hot reload для backend (watch mode) — автоматическая перезагрузка при изменении кода
  - [ ] Vite dev server для frontend — HMR для быстрой разработки
  - [ ] Порты для отладки — expose отладочных портов .NET и Node

- [ ] **7.5 Документация**
  - [ ] Инструкции по запуску в README — шаги для production и development
  - [ ] Переменные окружения (.env.example) — шаблон с описанием всех переменных

**Результат фазы:** Приложение запускается одной командой `docker-compose up`.

---

### Фаза 8: Client-side Logging
- [ ] **8.1 Backend - ClientLogsController**
  - [ ] POST /api/client-logs — приём batch логов с фронтенда в JSON формате
  - [ ] Валидация Level (обязательное поле) — проверка наличия уровня логирования
  - [ ] Нормализация алиасов — преобразование warning→warn, fatal/critical→error
  - [ ] Rate limiting для защиты от спама — ограничение количества запросов на IP

- [ ] **8.2 Backend - логирование**
  - [ ] Запись в общий лог-файл с префиксом [CLIENT] — идентификация источника логов
  - [ ] Structured logging с контекстом — добавление userId, version, url в записи
  - [ ] Санитизация входных данных — защита от XSS и инъекций в логах

- [ ] **8.3 Frontend - ClientLogger service**
  - [ ] Методы: trace, debug, info, warn, error, exception — API аналогичный NLog
  - [ ] Буферизация логов (batchSize: 10) — накопление перед отправкой
  - [ ] Автоматический flush по таймеру (5 сек) — гарантия отправки даже при малом трафике
  - [ ] Retry с exponential backoff (maxRetries: 3) — повтор при ошибках сети

- [ ] **8.4 Frontend - глобальный контекст**
  - [ ] setGlobalContext({ userId, version, sessionId }) — установка метаданных для всех логов
  - [ ] Автоматическое добавление url, userAgent — сбор информации о браузере

- [ ] **8.5 Frontend - error handlers**
  - [ ] window.onerror — перехват глобальных ошибок JavaScript
  - [ ] window.onunhandledrejection — перехват необработанных Promise rejection
  - [ ] Vue Error Boundary (errorCaptured hook) — перехват ошибок в компонентах Vue

- [ ] **8.6 Тестирование**
  - [ ] Unit tests для ClientLogger — проверка буферизации и retry логики
  - [ ] Integration tests для /api/client-logs — тесты API эндпоинта
  - [ ] Тест rate limiting — проверка ограничения частоты запросов

**Результат фазы:** Ошибки с фронтенда автоматически отправляются на сервер.

---

### Фаза 9: Photino Desktop
- [ ] **9.1 Создание Desktop проекта**
  - [ ] Создать nLogMonitor.Desktop (console → winexe) — проект без консольного окна
  - [ ] Добавить Photino.NET NuGet пакет — кроссплатформенный WebView wrapper
  - [ ] Reference на nLogMonitor.Api — использование embedded web server

- [ ] **9.2 Program.cs - основа**
  - [ ] Запуск embedded ASP.NET Core в фоновом потоке — self-hosted Kestrel внутри приложения
  - [ ] Создание PhotinoWindow — нативное окно с WebView
  - [ ] Загрузка index.html (production) или localhost:5173 (dev) — переключение по конфигурации
  - [ ] RegisterWebMessageReceivedHandler для IPC — обработка сообщений от JavaScript

- [ ] **9.3 Нативные диалоги (Photino built-in, кроссплатформенные)**
  - [ ] ShowOpenFileDialog — `PhotinoWindow.ShowOpenFile()` с фильтрами `[("Log files", "*.log"), ("All files", "*.*")]`
  - [ ] ShowOpenFolderDialog — `PhotinoWindow.ShowOpenFolder()` для выбора директории
  - [ ] Обработка результата — проверка на null (пользователь отменил диалог)

- [ ] **9.4 JS ↔ .NET Bridge**
  - [ ] Message handler для команд — обработка openFile, openFolder, isDesktop
  - [ ] JSON сериализация запросов/ответов — структурированный обмен данными
  - [ ] Отправка результата обратно в WebView — callback через postMessage

- [ ] **9.5 Frontend - usePhotinoBridge composable**
  - [ ] Определение режима (isDesktop) — проверка наличия window.external.sendMessage
  - [ ] showOpenFileDialog() → Promise<string | null> — async wrapper над нативным диалогом
  - [ ] showOpenFolderDialog() → Promise<string | null> — async wrapper над нативным диалогом
  - [ ] Fallback на web-версию если не desktop — graceful degradation

- [ ] **9.6 FileSelector - режимы работы**
  - [ ] Web: input[type=file] — стандартный input для выбора файла
  - [ ] Desktop: нативные кнопки "Открыть файл" / "Открыть директорию" — системные диалоги
  - [ ] Переключение на основе isDesktop — автоматическое определение режима

- [ ] **9.7 Сборка и публикация**
  - [ ] npm run build → client/dist — production сборка фронтенда
  - [ ] Копирование dist в wwwroot — встраивание статики в Desktop приложение
  - [ ] dotnet publish -c Release -r win-x64 --self-contained — публикация автономного exe
  - [ ] Тестирование собранного приложения — проверка работы всех функций

**Результат фазы:** Desktop приложение с нативными диалогами.

---

### Фаза 10: Оптимизация и тестирование
- [ ] **10.1 Performance - Frontend**
  - [ ] Virtual scrolling для больших списков (@tanstack/vue-virtual) — рендеринг только видимых строк
  - [ ] Lazy loading компонентов (defineAsyncComponent) — отложенная загрузка редко используемых компонентов
  - [ ] Оптимизация bundle size (analyze + tree shaking) — анализ и удаление неиспользуемого кода
  - [ ] Memoization для вычисляемых значений — кэширование тяжёлых вычислений через computed

- [ ] **10.2 Performance - Backend**
  - [ ] IMemoryCache для частых запросов — кэширование результатов фильтрации и подсчётов
  - [ ] Streaming для экспорта больших файлов — потоковая генерация без загрузки в память
  - [ ] Оптимизация парсера (Span<char>) — использование Span для zero-allocation парсинга

- [ ] **10.3 UX улучшения**
  - [ ] Skeleton loaders — placeholder контент во время загрузки данных
  - [ ] Smooth animations — плавные переходы между состояниями UI
  - [ ] Keyboard shortcuts (Ctrl+F для поиска) — быстрый доступ к основным функциям
  - [ ] Dark mode (опционально) — тёмная тема для комфортной работы ночью

- [ ] **10.4 Тестирование**
  - [ ] E2E tests (Playwright) — автоматизированные тесты всего пользовательского сценария
  - [ ] Performance testing с большими файлами (100MB+, 1M+ записей) — проверка производительности
  - [ ] Cross-browser testing — проверка в Chrome, Firefox, Safari, Edge
  - [ ] Cross-platform testing Desktop (Windows, Linux, macOS) — проверка Photino на всех ОС

- [ ] **10.5 Документация**
  - [ ] README.md с инструкциями — руководство по установке и использованию
  - [ ] Changelog — история изменений по версиям
  - [ ] Примеры использования API — curl/Postman примеры для всех эндпоинтов

- [ ] **10.6 Финализация**
  - [ ] Security review — проверка на уязвимости (OWASP Top 10)
  - [ ] Code cleanup — удаление мёртвого кода и TODO комментариев
  - [ ] Release v1.0.0 — создание тега и релиза в Git

**Результат фазы:** Production-ready приложение.

---

## Docker конфигурация

### docker-compose.yml
```yaml
services:
  api:
    build:
      context: .
      dockerfile: src/nLogMonitor.Api/Dockerfile
    ports:
      - "5000:8080"
    volumes:
      - ./data:/app/data    # recent.json и другие персистентные данные
      # temp/ не монтируется — загруженные файлы теряются при перезапуске
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

**Формат полей:**
| Поле | Формат | Пример |
|------|--------|--------|
| longdate | `YYYY-MM-DD HH:mm:ss.ffff` | `2024-01-15 10:30:45.1234` |
| level | `TRACE\|DEBUG\|INFO\|WARN\|ERROR\|FATAL` | `ERROR` |
| message | Произвольный текст, **может быть многострочным** | `Error occurred\nStack trace...` |
| logger | Имя класса/модуля | `MyApp.Services.UserService` |
| processid | Целое число | `1234` |
| threadid | Целое число | `5` |

### Многострочные сообщения

Сообщение в записи лога **может содержать переносы строк** (stack traces, многострочные данные). При этом:
- Сообщение может содержать символы `|` внутри текста
- Новая запись определяется по **наличию даты в начале строки** (regex: `^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}`)
- Поля `logger`, `processid`, `threadid` всегда находятся **в конце записи**

**Алгоритм парсинга многострочных записей (как в nLogViewer):**
1. Читать строки в буфер пока не встретится строка, начинающаяся с даты
2. Для парсинга многострочной записи искать разделители `|` **с конца** строки
3. Последние 3 поля (logger, processid, threadid) фиксированы
4. Всё что между level и logger — это message (может содержать `\n` и `|`)

### Примеры записей

**Однострочная запись:**
```
2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1
```

**Многострочная запись (stack trace):**
```
2024-01-15 10:30:46.5678|ERROR|Unhandled exception: Object reference not set
   at MyApp.Service.Process() in C:\src\Service.cs:line 42
   at MyApp.Program.Main() in C:\src\Program.cs:line 15|MyApp.Service|1234|5
2024-01-15 10:30:47.0000|INFO|Next log entry|MyApp.Program|1234|1
```

В этом примере первая запись содержит message:
```
Unhandled exception: Object reference not set
   at MyApp.Service.Process() in C:\src\Service.cs:line 42
   at MyApp.Program.Main() in C:\src\Program.cs:line 15
```

---

## Планируемые доработки (Roadmap)

В данный раздел вынесены функции, которые **не входят в MVP**, но планируются к реализации в будущих версиях.

### Фаза 11: Удалённый доступ по SSH

**Текущее состояние:** Приложение работает только с локальными файлами (на машине, где запущено приложение).

**Планируется:** Возможность подключения к удалённым машинам по SSH для чтения логов с удалённых серверов.

- [ ] **11.1 Инфраструктура SSH-подключения**
  - [ ] Добавить SSH.NET NuGet пакет — библиотека для SSH-соединений из .NET
  - [ ] Создать ISshConnectionService interface — контракт для подключения: Connect, Disconnect, IsConnected
  - [ ] Создать SshConnectionManager — управление пулом SSH-соединений с кэшированием
  - [ ] Хранение учётных данных — SecureString для паролей, поддержка SSH-ключей (RSA, ED25519)

- [ ] **11.2 Удалённая файловая система**
  - [ ] Создать IRemoteFileSystem interface — контракт для удалённых операций: ListFiles, ReadFile, FileExists
  - [ ] Реализовать SftpFileSystem — работа с файлами через SFTP протокол
  - [ ] Адаптировать DirectoryScanner для удалённых директорий — поиск последнего .log файла на удалённой машине
  - [ ] Реализовать удалённый FileWatcher — tail -f через SSH для мониторинга изменений

- [ ] **11.3 API и UI**
  - [ ] POST /api/connections — создание SSH-соединения (host, port, username, password/key)
  - [ ] GET /api/connections — список активных соединений
  - [ ] DELETE /api/connections/{id} — закрытие соединения
  - [ ] POST /api/files/open-remote — открытие файла на удалённой машине
  - [ ] Компонент ConnectionManager — UI для управления SSH-подключениями
  - [ ] Сохранение профилей подключений — хранение часто используемых серверов (без паролей)

- [ ] **11.4 Безопасность**
  - [ ] Шифрование сохранённых учётных данных — DPAPI (Windows) / Keychain (macOS) / Secret Service (Linux)
  - [ ] Таймаут неактивных соединений — автоматическое закрытие через N минут
  - [ ] Логирование подключений — аудит SSH-сессий без записи credentials

---

### Фаза 12: Компактный режим (Dashboard)

**Текущее состояние:** Отображается содержимое одного открытого лог-файла в виде таблицы записей.

**Планируется:** Компактный режим отображения — dashboard со списком всех отслеживаемых лог-файлов и сводной информацией по каждому.

- [ ] **12.1 Backend — множественные сессии**
  - [ ] Расширить LogSession — добавить статистику: TotalCount, CountByLevel (Dictionary<LogLevel, int>)
  - [ ] Реализовать ISessionStatisticsService — вычисление и кэширование статистики по сессии
  - [ ] API endpoint GET /api/sessions — список всех активных сессий с краткой статистикой
  - [ ] API endpoint GET /api/sessions/{id}/stats — детальная статистика по сессии
  - [ ] Обновление статистики через SignalR — push при изменении файла

- [ ] **12.2 Цветовая индикация уровней**
  - [ ] Определить приоритет уровней — Fatal > Error > Warn > Info > Debug > Trace
  - [ ] Алгоритм определения "здоровья" файла — по наличию записей уровня:
    - 🔴 **Красный** — есть Fatal или Error (критические проблемы)
    - 🟡 **Жёлтый** — есть Warn, но нет Error/Fatal (предупреждения)
    - 🟢 **Зелёный** — только Info/Debug/Trace (нормальная работа)
    - ⚪ **Серый** — файл пуст или нет записей
  - [ ] Периодическое сканирование — проверка наличия новых Error/Fatal записей

- [ ] **12.3 Frontend — Dashboard компонент**
  - [ ] Создать DashboardView — новый view для компактного режима
  - [ ] Создать SessionCard компонент — карточка одного лог-файла:
    - Имя файла и путь (или хост для удалённых)
    - Общее количество записей
    - Счётчики по каждому уровню (badges с числами)
    - Индикатор "здоровья" (цветная полоса или иконка)
    - Время последнего обновления
    - Кнопка "Открыть" для перехода к полному просмотру
  - [ ] Создать sessionsStore (Pinia) — состояние списка сессий и их статистики
  - [ ] Анимация обновления — плавное изменение счётчиков при появлении новых записей

- [ ] **12.4 Переключение режимов**
  - [ ] Добавить переключатель режимов в header — "Детальный" / "Компактный"
  - [ ] Сохранение предпочтения в localStorage — запоминание выбранного режима
  - [ ] Routing — /dashboard для компактного режима, /logs/{sessionId} для детального
  - [ ] Quick actions из Dashboard — быстрое открытие файла, экспорт, закрытие сессии

- [ ] **12.5 Дополнительные улучшения**
  - [ ] Сортировка карточек — по имени, по количеству ошибок, по времени обновления
  - [ ] Фильтрация карточек — показать только "проблемные" (с Error/Fatal)
  - [ ] Групповые операции — закрыть все, экспортировать все, очистить статистику
  - [ ] Уведомления — toast/звук при появлении Error в любом из файлов

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
