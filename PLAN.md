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
│   NLogParser, InMemorySessionStorage, DirectoryScanner          │
│   FileWatcherService, JsonExporter, CsvExporter                 │
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

### Матрица функциональности Web vs Desktop

| Функция | Web (Docker) | Desktop (Photino) |
|---------|:------------:|:-----------------:|
| Открытие файла | Upload (multipart) | Системный диалог |
| Открытие директории | ❌ | ✅ |
| File watching (real-time) | ❌ (только загруженный) | ✅ |
| Максимальный размер файла | 100 MB | Без ограничений |
| Recent files | Per-session (в памяти) | Persist локально (JSON) |
| Экспорт | ✅ | ✅ |
| Фильтрация/поиск | ✅ (серверная) | ✅ (серверная) |
| Пагинация | ✅ (серверная) | ✅ (серверная) |

**Важно:** Фильтрация, поиск и пагинация выполняются **на сервере** (LINQ over in-memory collection), а не на клиенте. Это критично для больших логов (100K+ записей).

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
│   │   │   ├── IRecentLogsRepository.cs
│   │   │   └── IDirectoryScanner.cs
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
│   │   ├── Configuration/
│   │   │   └── SessionSettings.cs
│   │   ├── Exceptions/
│   │   │   └── NoLogFilesFoundException.cs
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

**Definition of Done (DoD):**
- [x] `dotnet build` без ошибок и warnings
- [x] `dotnet run --project src/nLogMonitor.Api` запускает сервер
- [x] GET /health возвращает 200 OK
- [x] Swagger UI доступен на /swagger
- [x] Все entities и interfaces созданы согласно списку

---

### Фаза 2: Парсинг и хранение логов
- [x] **2.1 NLog Parser**
  - [x] Реализовать быстрый парсер для однострочных записей — поиск разделителей `|` с начала строки, без regex для производительности
  - [x] Реализовать парсер для многострочных записей — поиск разделителей `|` **с конца** строки (logger, processid, threadid фиксированы в конце)
  - [x] Реализовать определение начала новой записи по дате — regex `^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}` для определения границ записей
  - [x] Реализовать накопительный буфер для многострочных сообщений — StringBuilder для сбора строк до успешного парсинга
  - [x] Реализовать IAsyncEnumerable для streaming парсинга больших файлов — построчное чтение без загрузки всего файла в память
  - [x] Использовать Span<char>/ReadOnlySpan<char> для zero-allocation парсинга — минимизация аллокаций памяти
  - [x] Добавить fallback на Regex для нестандартных случаев — запасной вариант если быстрый парсер не справился
  - [x] Обработка ошибок парсинга — логирование непарсируемых строк без прерывания обработки

- [x] **2.2 Session Storage**
  - [x] Реализовать InMemorySessionStorage с ConcurrentDictionary — потокобезопасное хранилище сессий
  - [x] Добавить fallback TTL (5 минут) — страховка для потерянных сессий (основное управление через SignalR в Фазе 6)
  - [x] Реализовать sliding expiration — продление TTL при каждом обращении к сессии
  - [x] Реализовать cleanup timer для удаления просроченных сессий — фоновая очистка каждые 1 минуту
  - [x] Реализовать IDisposable для корректной остановки таймера — освобождение ресурсов при завершении приложения
  - [x] Подготовить методы для привязки к SignalR — BindConnectionAsync, UnbindConnectionAsync, GetSessionByConnectionAsync (реализация в Фазе 6)

- [x] **2.3 Directory Scanner**
  - [x] Реализовать FindLastLogFileByName — поиск файла с последним именем (сортировка по имени в обратном порядке)
  - [x] Поддержка паттерна `*.log` — фильтрация только лог-файлов в директории
  - [x] Обработка пустой директории — возврат информативной ошибки при отсутствии файлов

- [x] **2.4 Application Service (серверная фильтрация)**
  - [x] Реализовать LogService.OpenFileAsync — открытие файла: чтение → парсинг → создание сессии → сохранение
  - [x] Реализовать LogService.GetLogsAsync — **серверная** фильтрация, поиск и пагинация через LINQ
  - [ ] Построить индекс по Level для быстрой фильтрации — Dictionary<LogLevel, List<int>> (перенесено в оптимизацию, LINQ достаточно для <1M записей)
  - [x] Полнотекстовый поиск через Contains (case-insensitive) — на сервере, не на клиенте
  - [x] Пагинация через Skip/Take — клиент получает только запрошенную страницу

- [x] **2.5 Тестирование**
  - [x] Unit tests для однострочного парсера — тесты стандартных записей, записей с пробелами вокруг `|`
  - [x] Unit tests для многострочного парсера — тесты stack traces, сообщений с `|` и `\n` внутри
  - [x] Unit tests для определения границ записей — проверка корректного разделения записей по дате
  - [x] Unit tests для InMemorySessionStorage — тесты CRUD операций и TTL логики
  - [ ] Тестирование с реальными лог-файлами из nLogViewer — интеграционные тесты на примерах реальных логов

**Результат фазы:** Работающий парсер логов с хранением в памяти. ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] Unit tests для парсера: ≥90% покрытие NLogParser
- [x] Парсинг файла 10MB (50K записей) < 2 сек
- [x] Многострочные записи корректно парсятся (тест со stack trace)
- [x] InMemorySessionStorage: тесты CRUD + TTL expiration
- [x] LogService.GetLogsAsync возвращает отфильтрованные данные

---

### Фаза 3: API Endpoints
- [x] **3.1 Files Controller (Desktop-only)**
  - [x] POST /api/files/open — открытие файла по абсолютному пути (**только Desktop**)
  - [x] POST /api/files/open-directory — открытие директории с автоматическим выбором последнего по имени .log файла (**только Desktop**)
  - [x] POST /api/files/{sessionId}/stop-watching — остановка мониторинга изменений файла для указанной сессии

- [x] **3.2 Upload Controller (Web режим)**
  - [x] POST /api/upload — загрузка файла через multipart/form-data для Web-версии
  - [x] Валидация расширения (.log, .txt) — проверка допустимых типов файлов
  - [x] Лимит размера файла (100MB) — защита от загрузки слишком больших файлов
  - [x] Сохранение в `/app/temp/{sessionId}/` — временное хранение загруженного файла
  - [x] Очистка временных файлов при удалении сессии — удаление папки `{sessionId}` вместе с файлом

- [x] **3.3 Logs Controller**
  - [x] GET /api/logs/{sessionId} — получение логов сессии с поддержкой фильтрации и пагинации
  - [x] Query параметры — search, minLevel, maxLevel, fromDate, toDate, logger, page, pageSize
  - [x] Валидация параметров (FluentValidation) — проверка корректности входных данных

- [x] **3.4 Export Controller (потоковый)**
  - [x] GET /api/export/{sessionId} — экспорт логов с query параметром format (json/csv)
  - [x] Реализовать JsonExporter с IAsyncEnumerable — **потоковая** генерация без загрузки всех данных в память
  - [x] Реализовать CsvExporter с yield return — построчная запись в Response.Body
  - [x] Установить Content-Type и Content-Disposition для скачивания — правильные заголовки для браузера
  - [x] Поддержка фильтров при экспорте — применение серверных фильтров из Фазы 2.4
  - [x] Тест экспорта 100K+ записей — убедиться что память не растёт линейно

- [x] **3.5 Recent Controller**
  - [x] GET /api/recent — получение списка недавно открытых файлов и директорий
  - [x] Реализовать RecentLogsFileRepository — хранение истории в JSON файле в AppData
  - [x] Лимит на количество записей (10-20) — автоматическое удаление старых записей

- [x] **3.6 Middleware и обработка ошибок**
  - [x] ExceptionHandlingMiddleware — единообразный формат ошибок {error, message, details}
  - [x] Логирование ошибок через NLog — запись stack trace и контекста в лог-файл

- [x] **3.7 Документация и тестирование**
  - [x] XML comments для Swagger — описание эндпоинтов, параметров и моделей
  - [x] Integration tests для контроллеров — тесты полного цикла запрос-ответ
  - [x] Тестирование через Postman/curl — ручная проверка всех эндпоинтов

**Результат фазы:** Полнофункциональное REST API для работы с логами. ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] Integration tests для всех endpoints (≥80% покрытие контроллеров)
- [x] POST /api/upload успешно принимает файл ≤100MB
- [x] GET /api/logs/{sessionId}?page=1&pageSize=50 возвращает ровно 50 записей
- [x] GET /api/export/{sessionId}?format=csv генерирует валидный CSV
- [ ] Экспорт 100K записей: память < 200MB, время < 10 сек (перенесено в оптимизацию)
- [x] Swagger документация актуальна для всех endpoints

---

### Фаза 3.1: Исправления и улучшения Фазы 3

> **Контекст:** Код-ревью выявило 10 замечаний по безопасности, архитектуре и качеству кода. Данная фаза устраняет критические и важные проблемы перед переходом к Frontend.

#### 3.1.1 Безопасность (Критические)

- [ ] **Path Traversal в Upload**
  - [ ] Заменить `file.FileName` на `Path.GetFileName(file.FileName)` для извлечения только имени файла
  - [ ] Добавить валидацию результирующего пути через `Path.GetFullPath()` — проверка что путь не выходит за пределы temp-директории
  - [ ] Добавить unit-тест на попытку path traversal (`../../../etc/passwd.log`)
  - **Файл:** `src/nLogMonitor.Api/Controllers/UploadController.cs:120`

- [ ] **Desktop-only эндпоинты в Web-режиме**
  - [ ] Добавить `AppMode` в конфигурацию (`appsettings.json`) — enum: `Web`, `Desktop`
  - [ ] Создать `DesktopOnlyAttribute` (ActionFilterAttribute) — возвращает 404/403 если режим не Desktop
  - [ ] Применить `[DesktopOnly]` к `FilesController.OpenFile` и `FilesController.OpenDirectory`
  - [ ] Добавить unit-тесты для проверки блокировки в Web-режиме
  - **Файлы:** `src/nLogMonitor.Api/Controllers/FilesController.cs:45,101`, `src/nLogMonitor.Api/Program.cs`

#### 3.1.2 Архитектура (Важные)

- [ ] **Несоответствие Guid (temp-каталог vs sessionId)**
  - [ ] Модифицировать `ILogService.OpenFileAsync` — добавить опциональный параметр `Guid? sessionId`
  - [ ] В `UploadController` передавать сгенерированный `sessionId` в `LogService`
  - [ ] Альтернатива: добавить `TempDirectory` property в `LogSession` для связи
  - [ ] Добавить unit-тест на соответствие sessionId и temp-директории
  - **Файлы:** `src/nLogMonitor.Api/Controllers/UploadController.cs:110`, `src/nLogMonitor.Application/Services/LogService.cs:49`

- [ ] **stop-watching возвращает 204 без действия**
  - [ ] Изменить HTTP статус на `501 Not Implemented` с информативным сообщением
  - [ ] Вернуть `ApiErrorResponse` с описанием: "File watching planned for Phase 6"
  - [ ] Обновить XML-документацию метода
  - **Файл:** `src/nLogMonitor.Api/Controllers/FilesController.cs:149`

- [ ] **Экспорт не потоковый (3 прохода по данным)**
  - [ ] Убрать `filteredEntries.Count()` — не нужен для streaming
  - [ ] Переписать `JsonExporter` на `IAsyncEnumerable` + `Utf8JsonWriter` для потоковой записи
  - [ ] Переписать `CsvExporter` на `IAsyncEnumerable` + прямую запись в `Stream`
  - [ ] Использовать `FileCallbackResult` или `PushStreamContent` в контроллере
  - [ ] Добавить benchmark-тест: экспорт 100K записей, замер памяти
  - **Файлы:** `src/nLogMonitor.Api/Controllers/ExportController.cs:95`, `src/nLogMonitor.Infrastructure/Export/JsonExporter.cs`, `src/nLogMonitor.Infrastructure/Export/CsvExporter.cs`

#### 3.1.3 Обработка ошибок (Средние)

- [ ] **DirectoryNotFoundException не мапится в middleware**
  - [ ] Добавить case для `DirectoryNotFoundException` в `ExceptionHandlingMiddleware`
  - [ ] Возвращать HTTP 404 с типом ошибки "NotFound"
  - [ ] Добавить unit-тест для проверки маппинга
  - **Файлы:** `src/nLogMonitor.Infrastructure/FileSystem/DirectoryScanner.cs:15`, `src/nLogMonitor.Api/Middleware/ExceptionHandlingMiddleware.cs:71`

- [ ] **Формат ошибок разъезжается в ExportController**
  - [ ] Заменить `BadRequest(string)` на `BadRequest(new ApiErrorResponse(...))`
  - [ ] Заменить `NotFound()` на `NotFound(new ApiErrorResponse(...))`
  - [ ] Проверить все контроллеры на консистентность формата ошибок
  - **Файл:** `src/nLogMonitor.Api/Controllers/ExportController.cs:78,87`

#### 3.1.4 Тестирование (Важные)

- [ ] **Интеграционные тесты для контроллеров**
  - [ ] Создать `FilesControllerTests` с `WebApplicationFactory<Program>`
  - [ ] Создать `UploadControllerTests` — тесты загрузки, валидации расширений, лимита размера
  - [ ] Создать `ExportControllerTests` — тесты JSON/CSV экспорта, несуществующей сессии
  - [ ] Создать `RecentControllerTests` — тесты получения и очистки истории
  - [ ] Добавить тест на path traversal в Upload (security test)
  - **Файлы:** `tests/nLogMonitor.Api.Tests/Controllers/`

#### 3.1.5 Конфигурация (Низкие)

- [ ] **RequestSizeLimit расходится с FileSettings.MaxFileSizeMB**
  - [ ] Изменить `[RequestSizeLimit]` на `110 * 1024 * 1024` (110 MiB) — запас на multipart overhead
  - [ ] Или убрать атрибут и настроить через `FormOptions` в `Program.cs`
  - [ ] Добавить комментарий объясняющий связь с `FileSettings.MaxFileSizeMB`
  - **Файлы:** `src/nLogMonitor.Api/Controllers/UploadController.cs:16`, `src/nLogMonitor.Application/Configuration/FileSettings.cs:16`

- [ ] **XML-комментарии не подключены к Swagger**
  - [ ] Добавить `<GenerateDocumentationFile>true</GenerateDocumentationFile>` в .csproj
  - [ ] Добавить `<NoWarn>$(NoWarn);1591</NoWarn>` для подавления предупреждений
  - [ ] Добавить `options.IncludeXmlComments(...)` в конфигурацию Swagger
  - [ ] Проверить отображение описаний в Swagger UI
  - **Файлы:** `src/nLogMonitor.Api/nLogMonitor.Api.csproj`, `src/nLogMonitor.Api/Program.cs:32`

**Результат фазы:** Устранены критические уязвимости, улучшена консистентность API, добавлены недостающие тесты.

**Definition of Done (DoD):**
- [ ] Path traversal: тест с `../../../` проходит (возвращает ошибку)
- [ ] Desktop-only: `/api/files/open` возвращает 404 в Web-режиме
- [ ] Экспорт: 100K записей экспортируются без роста памяти > 50MB
- [ ] Все контроллеры возвращают `ApiErrorResponse` для ошибок
- [ ] Integration tests: ≥10 новых тестов для Files/Upload/Export/Recent
- [ ] `DirectoryNotFoundException` → HTTP 404
- [ ] XML-комментарии отображаются в Swagger UI

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

**Definition of Done (DoD):**
- [ ] `npm run build` без ошибок
- [ ] `npm run dev` запускает dev server на localhost:5173
- [ ] Upload файла → логи отображаются в таблице (E2E тест)
- [ ] TypeScript: no `any` в production коде (strict mode)
- [ ] Все API вызовы через axios client с error handling

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

**Definition of Done (DoD):**
- [ ] Фильтрация по Level: клик → API запрос → таблица обновляется
- [ ] Поиск: ввод текста → debounce 300ms → API запрос
- [ ] Пагинация: переключение страниц работает корректно
- [ ] Экспорт: скачивается файл с правильным Content-Type
- [ ] Responsive: корректное отображение на 320px-1920px

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

**Definition of Done (DoD):**
- [ ] Изменение файла → новые записи появляются в UI < 1 сек
- [ ] SignalR reconnect: потеря сети → восстановление → данные синхронизированы
- [ ] Закрытие вкладки → сессия удаляется (проверить через API)
- [ ] Индикатор "Live" отображается при активном соединении
- [ ] Нагрузочный тест: 10 изменений/сек → UI не тормозит

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

- [ ] **7.6 Метрики и мониторинг (минимальный набор)**
  - [ ] GET /api/metrics — JSON endpoint с базовыми метриками
  - [ ] Количество активных сессий — `sessions_active_count`
  - [ ] Общий размер данных в памяти — `sessions_memory_bytes` (примерная оценка)
  - [ ] Количество записей по всем сессиям — `logs_total_count`
  - [ ] Uptime сервера — `server_uptime_seconds`
  - [ ] Количество SignalR подключений — `signalr_connections_count`

**Результат фазы:** Приложение запускается одной командой `docker-compose up`.

**Definition of Done (DoD):**
- [ ] `docker-compose up -d --build` запускает оба сервиса без ошибок
- [ ] http://localhost:80 открывает frontend
- [ ] http://localhost:5000/health возвращает 200
- [ ] Upload файла через браузер → логи отображаются
- [ ] Размер образа api < 200MB, client < 50MB

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

**Definition of Done (DoD):**
- [ ] console.error в браузере → запись в серверном логе с [CLIENT] префиксом
- [ ] Unhandled Promise rejection → логируется на сервере
- [ ] Rate limiting: >100 req/min от одного IP → 429 Too Many Requests
- [ ] Batch отправка: 10 логов накапливаются → один POST запрос

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

**Definition of Done (DoD):**
- [ ] Windows: exe запускается, открывает файл через системный диалог
- [ ] Linux: AppImage/deb запускается на Ubuntu 22.04
- [ ] macOS: app bundle запускается на macOS 12+
- [ ] Все функции Web-версии работают в Desktop
- [ ] Размер exe (self-contained) < 100MB

---

### Фаза 10: Оптимизация и тестирование
- [ ] **10.1 Performance - Frontend**
  - [ ] Virtual scrolling для больших списков (@tanstack/vue-virtual) — рендеринг только видимых строк
  - [ ] Lazy loading компонентов (defineAsyncComponent) — отложенная загрузка редко используемых компонентов
  - [ ] Оптимизация bundle size (analyze + tree shaking) — анализ и удаление неиспользуемого кода
  - [ ] Memoization для вычисляемых значений — кэширование тяжёлых вычислений через computed

- [ ] **10.2 Performance - Backend**
  - [ ] IMemoryCache для частых запросов — кэширование результатов фильтрации и подсчётов
  - [ ] Профилирование памяти при 1M+ записей — dotMemory/PerfView для поиска утечек
  - [ ] Оптимизация индексов фильтрации — бенчмарки Dictionary vs HashSet для Level-индекса

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

**Definition of Done (DoD):**
- [ ] Файл 100MB (500K записей) загружается < 10 сек
- [ ] Virtual scrolling: 1M записей рендерится без лагов
- [ ] E2E тесты Playwright: ≥5 сценариев покрыто
- [ ] Bundle size frontend < 500KB (gzipped)
- [ ] Security: no critical/high vulnerabilities (npm audit, dotnet audit)
- [ ] README.md с полными инструкциями по запуску

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
