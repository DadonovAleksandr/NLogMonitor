# nLogMonitor - План разработки

## Содержание
1. [Обзор проекта](#обзор-проекта)
2. [Архитектура системы](#архитектура-системы)
3. [Технологический стек](#технологический-стек)
4. [Структура проекта](#структура-проекта)
5. [План разработки](#план-разработки)
6. [Скрипты запуска](#скрипты-запуска)
7. [Планируемые доработки (Roadmap)](#планируемые-доработки-roadmap)

---

## Обзор проекта

**nLogMonitor** — кроссплатформенное приложение для просмотра и анализа NLog-логов. Работает в двух режимах:
- **Web-приложение** — запуск через скрипт (backend + frontend)
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
| Framework | Vue | 3.5 |
| Language | TypeScript | 5.9 |
| Build Tool | Vite | 7.x |
| State | Pinia | 3.x |
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
| Запуск | Скрипты (PowerShell/Bash) |
| Backend Server | Kestrel (встроенный) |
| Frontend Dev Server | Vite |
| Хранение сессий | In-Memory |
| Хранение недавних | JSON файл |

### Стратегия работы с файлами

**Web-режим** — только upload через браузер:

| Параметр | Значение |
|----------|----------|
| Endpoint | `POST /api/upload` |
| Лимит размера | 100 MB |
| Хранение | `./temp/{sessionId}/` (рядом с приложением) |
| Очистка | Вместе с сессией (SignalR disconnect или fallback TTL) |
| Persist | Нет — файлы теряются при перезапуске сервера |

**Desktop-режим (Photino)** — прямой доступ к файловой системе:

| Параметр | Значение |
|----------|----------|
| Endpoints | `POST /api/files/open`, `POST /api/files/open-directory` |
| Диалоги | Photino built-in (кроссплатформенные) |
| Ограничения | Нет лимита размера, доступ к любым путям |

### Матрица функциональности Web vs Desktop

| Функция | Web | Desktop (Photino) |
|---------|:---:|:-----------------:|
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
│   │   ├── wwwroot/                   # Production: Vue build output
│   │   ├── Program.cs
│   │   ├── appsettings.json
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
├── start-dev.bat                 # Windows: запуск dev mode
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

- [x] **Path Traversal в Upload**
  - [x] Заменить `file.FileName` на `Path.GetFileName(file.FileName)` для извлечения только имени файла
  - [x] Добавить валидацию результирующего пути через `Path.GetFullPath()` — проверка что путь не выходит за пределы temp-директории
  - [x] Добавить unit-тест на попытку path traversal (`../../../etc/passwd.log`)
  - **Файл:** `src/nLogMonitor.Api/Controllers/UploadController.cs:119-136`

- [x] **Desktop-only эндпоинты в Web-режиме**
  - [x] Добавить `AppMode` в конфигурацию (`appsettings.json`) — enum: `Web`, `Desktop`
  - [x] Создать `DesktopOnlyAttribute` (ActionFilterAttribute) — возвращает 404 если режим не Desktop
  - [x] Применить `[DesktopOnly]` к `FilesController.OpenFile` и `FilesController.OpenDirectory`
  - [x] Добавить интеграционные тесты для проверки блокировки в Web-режиме
  - **Файлы:** `src/nLogMonitor.Application/Configuration/AppSettings.cs`, `src/nLogMonitor.Api/Filters/DesktopOnlyAttribute.cs`, `src/nLogMonitor.Api/Controllers/FilesController.cs`

#### 3.1.2 Архитектура (Важные)

- [x] **Несоответствие Guid (temp-каталог vs sessionId)**
  - [x] Модифицировать `ILogService.OpenFileAsync` — добавить опциональный параметр `Guid? sessionId`
  - [x] В `UploadController` передавать сгенерированный `sessionId` в `LogService`
  - [x] Теперь temp-директория и sessionId всегда совпадают
  - **Файлы:** `src/nLogMonitor.Application/Interfaces/ILogService.cs`, `src/nLogMonitor.Application/Services/LogService.cs`, `src/nLogMonitor.Api/Controllers/UploadController.cs`

- [x] **stop-watching возвращает 204 без действия**
  - [x] Изменить HTTP статус на `501 Not Implemented` с информативным сообщением
  - [x] Вернуть `ApiErrorResponse` с описанием: "File watching functionality is planned for Phase 6"
  - [x] Обновить XML-документацию метода и ProducesResponseType атрибуты
  - **Файл:** `src/nLogMonitor.Api/Controllers/FilesController.cs:157-187`

- [x] **Экспорт не потоковый (3 прохода по данным)**
  - [x] Убрать `filteredEntries.Count()` — не нужен для streaming
  - [x] Переписать `JsonExporter` на `Utf8JsonWriter` для потоковой записи напрямую в Response.Body
  - [x] Переписать `CsvExporter` на `StreamWriter` с потоковой записью
  - [x] Изменить интерфейс `ILogExporter` — метод `ExportToStreamAsync(entries, outputStream)`
  - [x] Добавить unit-тесты для экспортеров (30 тестов)
  - **Файлы:** `src/nLogMonitor.Application/Interfaces/ILogExporter.cs`, `src/nLogMonitor.Api/Controllers/ExportController.cs`, `src/nLogMonitor.Infrastructure/Export/JsonExporter.cs`, `src/nLogMonitor.Infrastructure/Export/CsvExporter.cs`

#### 3.1.3 Обработка ошибок (Средние)

- [x] **DirectoryNotFoundException не мапится в middleware**
  - [x] Добавить case для `DirectoryNotFoundException` в `ExceptionHandlingMiddleware`
  - [x] Возвращать HTTP 404 с типом ошибки "NotFound" и сообщением "Directory not found: {path}"
  - **Файл:** `src/nLogMonitor.Api/Middleware/ExceptionHandlingMiddleware.cs:81-85`

- [x] **Формат ошибок разъезжается в ExportController**
  - [x] Заменить `BadRequest(string)` на `BadRequest(new ApiErrorResponse(...))`
  - [x] Заменить `NotFound()` на `NotFound(new ApiErrorResponse(...))`
  - [x] Все контроллеры теперь возвращают консистентный формат ApiErrorResponse
  - **Файл:** `src/nLogMonitor.Api/Controllers/ExportController.cs:79-98`

#### 3.1.4 Тестирование (Важные)

- [x] **Интеграционные тесты для контроллеров**
  - [x] Создать `WebApplicationTestBase` и `DesktopModeTestBase` базовые классы с `WebApplicationFactory<Program>`
  - [x] Создать `FilesControllerIntegrationTests` — 8 тестов для Web и Desktop режимов
  - [x] Создать `UploadControllerIntegrationTests` — 7 тестов загрузки, валидации, path traversal
  - [x] Создать `ExportControllerIntegrationTests` — 6 тестов JSON/CSV экспорта
  - [x] Создать `HealthCheckIntegrationTests` — 3 теста health endpoint
  - [x] Добавить `public partial class Program { }` для доступа из тестов
  - **Файлы:** `tests/nLogMonitor.Api.Tests/Integration/` (24 новых интеграционных теста)

#### 3.1.5 Конфигурация (Низкие)

- [x] **RequestSizeLimit расходится с FileSettings.MaxFileSizeMB**
  - [x] Изменить `[RequestSizeLimit]` на 110 MB — запас на multipart overhead
  - [x] Добавить комментарии объясняющие связь с `FileSettings.MaxFileSizeMB`
  - **Файл:** `src/nLogMonitor.Api/Controllers/UploadController.cs:16-18`

- [x] **XML-комментарии не подключены к Swagger**
  - [x] Добавить `<GenerateDocumentationFile>true</GenerateDocumentationFile>` в .csproj
  - [x] Добавить `<NoWarn>$(NoWarn);1591</NoWarn>` для подавления предупреждений
  - [x] Добавить `options.IncludeXmlComments(...)` в конфигурацию Swagger
  - [x] XML-файл генерируется в bin/Debug/net10.0/nLogMonitor.Api.xml
  - **Файлы:** `src/nLogMonitor.Api/nLogMonitor.Api.csproj`, `src/nLogMonitor.Api/Program.cs:32-45`

**Результат фазы:** ✅ ЗАВЕРШЕНО. Устранены критические уязвимости, улучшена консистентность API, добавлены интеграционные тесты.

**Definition of Done (DoD):**
- [x] Path traversal: интеграционный тест с `../../../` проходит (имя файла санитизируется)
- [x] Desktop-only: `/api/files/open` возвращает 404 в Web-режиме
- [x] Экспорт: потоковая запись напрямую в Response.Body без промежуточных буферов
- [x] Все контроллеры возвращают `ApiErrorResponse` для ошибок
- [x] Integration tests: 24 новых интеграционных теста добавлено
- [x] `DirectoryNotFoundException` → HTTP 404 в middleware
- [x] XML-комментарии включены в Swagger через GenerateDocumentationFile

**Статистика тестов:** 240 тестов — Infrastructure: 113, Application: 28, Api: 99

---

### Фаза 4: Frontend базовый (Vue 3)
- [x] **4.1 Инициализация проекта**
  - [x] Создать Vite + Vue 3 + TypeScript проект — `npm create vite@latest client -- --template vue-ts`
  - [x] Настроить path aliases (@/) — удобные импорты вместо относительных путей
  - [x] Установить и настроить Tailwind CSS — utility-first CSS фреймворк для стилизации
  - [x] Установить shadcn-vue и инициализировать компоненты — готовые UI компоненты на базе Radix

- [x] **4.2 Типы и API клиент**
  - [x] Определить TypeScript types — интерфейсы LogEntry, PagedResult, FilterOptions, OpenFileResult
  - [x] Создать axios client с baseURL — настройка базового URL и interceptors
  - [x] Создать API методы — uploadFile, getLogs, openFile, openDirectory, exportLogs, getRecent

- [x] **4.3 State Management (Pinia)**
  - [x] Создать logStore — состояние: sessionId, fileName, logs, totalCount, page, pageSize, isLoading, error
  - [x] Создать filterStore — фильтры: searchText, minLevel, maxLevel, fromDate, toDate, logger
  - [x] Создать recentStore — список недавно открытых файлов и директорий

- [x] **4.4 FileSelector компонент**
  - [x] Кнопка "Выбрать файл" (input type=file) — стилизованный input для выбора файла
  - [x] Валидация расширения файла — проверка .log и .txt перед загрузкой
  - [x] Loading state при загрузке — отображение спиннера и блокировка повторной загрузки

- [x] **4.5 LogTable компонент**
  - [x] Базовая таблица с TanStack Table — настройка колонок и рендеринга строк
  - [x] Колонки: Time, Level, Message, Logger — основные поля записи лога
  - [x] Цветовая индикация уровней логирования — красный для Error, жёлтый для Warn и т.д.
  - [x] Отображение состояния "нет данных" — placeholder при пустом списке

- [x] **4.6 Интеграция и проверка**
  - [x] App.vue с базовой разметкой — layout приложения: header, main, footer
  - [x] Проверка загрузки файла → отображение логов — end-to-end тест основного flow
  - [x] Настройка proxy в vite.config.ts для API — проксирование /api на localhost:5000

**Результат фазы:** Работающее Vue приложение с загрузкой файла и отображением логов. ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] `npm run build` без ошибок
- [x] `npm run dev` запускает dev server на localhost:5173
- [x] Upload файла → логи отображаются в таблице (E2E тест)
- [x] TypeScript: no `any` в production коде (strict mode)
- [x] Все API вызовы через axios client с error handling

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

### Фаза 7: Скрипты запуска и конфигурация
- [x] **7.1 Скрипты запуска**
  - [x] `start-dev.bat` (Windows) — запуск backend + frontend с hot reload в двух терминалах
  - [ ] `start-dev.sh` (Linux/macOS Bash) — аналогичный скрипт для Unix-систем
  - [ ] `stop.bat` / `stop.sh` — остановка всех процессов (опционально)

- [ ] **7.2 Конфигурация production**
  - [ ] `appsettings.Production.json` — настройки для production режима
  - [ ] Сборка frontend в `dist/` — `npm run build` генерирует статику
  - [ ] Раздача статики через Kestrel — UseStaticFiles для `wwwroot/`
  - [ ] Копирование `client/dist/` в `wwwroot/` при production запуске

- [x] **7.3 Единый скрипт сборки**
  - [x] `build.bat` (Windows) — полная сборка проекта
  - [x] Этап 1: `npm run build` — сборка frontend
  - [x] Этап 2: копирование `dist/` → `src/nLogMonitor.Api/wwwroot/`
  - [x] Этап 3: `dotnet publish -c Release` — сборка backend
  - [x] Результат: готовый к запуску `publish/` каталог
  - [ ] `build.sh` (Linux/macOS) — аналогичный скрипт для Unix-систем

- [ ] **7.4 Документация**
  - [ ] Инструкции по запуску в README — шаги для development и production
  - [ ] Описание скриптов и их параметров
  - [ ] Переменные окружения (.env.example) — шаблон с описанием

- [ ] **7.5 Метрики и мониторинг (минимальный набор)**
  - [ ] GET /api/metrics — JSON endpoint с базовыми метриками
  - [ ] Количество активных сессий — `sessions_active_count`
  - [ ] Общий размер данных в памяти — `sessions_memory_bytes` (примерная оценка)
  - [ ] Количество записей по всем сессиям — `logs_total_count`
  - [ ] Uptime сервера — `server_uptime_seconds`
  - [ ] Количество SignalR подключений — `signalr_connections_count`

**Результат фазы:** Приложение запускается одной командой `./start.ps1` или `./start.sh`.

**Definition of Done (DoD):**
- [ ] `./start.ps1` запускает backend и frontend без ошибок (Windows)
- [ ] `./start.sh` запускает backend и frontend без ошибок (Linux/macOS)
- [ ] http://localhost:5173 открывает frontend (dev mode)
- [ ] http://localhost:5000 открывает frontend (production mode, статика из wwwroot)
- [ ] http://localhost:5000/health возвращает 200
- [ ] Upload файла через браузер → логи отображаются

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

## Скрипты запуска

### Команды запуска (Development)
```bash
# Windows (CMD или PowerShell)
start-dev.bat                # Запуск backend + frontend с hot reload

# Или вручную в двух терминалах:
# Терминал 1 (backend):
dotnet watch run --project src/nLogMonitor.Api

# Терминал 2 (frontend):
cd client && npm run dev
```

### Команды запуска (Production)
```bash
# Сборка
dotnet publish src/nLogMonitor.Api -c Release -o publish
cd client && npm run build
# Скопировать client/dist → publish/wwwroot

# Запуск
cd publish
nLogMonitor.Api.exe          # Windows
./nLogMonitor.Api            # Linux/macOS
```

### Структура скриптов
```
nLogMonitor/
└── start-dev.bat           # Windows: запуск dev mode (backend + frontend)
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
