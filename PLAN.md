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

**Текущий статус:** Фаза 9 ✅ ЗАВЕРШЕНО (Photino Desktop). Следующая: Фаза 10 (Оптимизация и тестирование).

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
| Real-time | @microsoft/signalr | 10.0 |

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
│   │   │   ├── ClientLogDto.cs
│   │   │   └── AppInfoDto.cs
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
│   │   │   ├── InfoController.cs
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
│   │   │   ├── HeaderTabBar/         # Заголовок + вкладки + версия
│   │   │   ├── StatusBar/            # Статистика + пагинация
│   │   │   ├── Toolbar/              # Панель инструментов (фильтры, поиск, контролы)
│   │   │   ├── LogTable/
│   │   │   ├── SearchBar/
│   │   │   ├── FileSelector/
│   │   │   ├── RecentFiles/
│   │   │   ├── ExportButton/
│   │   │   ├── LiveIndicator/
│   │   │   └── Toast/
│   │   ├── stores/
│   │   │   ├── logStore.ts           # Управление логами
│   │   │   ├── tabsStore.ts          # Управление вкладками
│   │   │   ├── filterStore.ts        # Фильтры
│   │   │   ├── recentStore.ts        # Недавние файлы
│   │   │   └── settingsStore.ts      # Пользовательские настройки
│   │   ├── api/
│   │   │   ├── client.ts
│   │   │   ├── logs.ts
│   │   │   ├── files.ts
│   │   │   ├── info.ts               # Информация о приложении
│   │   │   ├── settings.ts
│   │   │   └── signalr.ts
│   │   ├── services/
│   │   │   └── logger.ts             # Client-side logger
│   │   ├── composables/
│   │   │   ├── useLogs.ts
│   │   │   ├── useFileWatcher.ts
│   │   │   ├── usePhotinoBridge.ts
│   │   │   └── useToast.ts
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
- [x] **5.1 FilterPanel компонент**
  - [x] Кнопки-фильтры по уровням (Trace, Debug, Info, Warn, Error, Fatal) — toggle-кнопки для включения/выключения уровней
  - [x] Подсчёт количества записей каждого уровня — badge с числом на каждой кнопке
  - [x] Активное/неактивное состояние фильтров — визуальное различие выбранных и невыбранных
  - [x] Цветовая индикация уровней — соответствие цветов стандартам NLog

- [x] **5.2 SearchBar компонент**
  - [x] Input с placeholder — подсказка "Поиск по сообщениям..."
  - [x] Debounce 300ms — задержка запроса для оптимизации производительности
  - [x] Иконка поиска — визуальный индикатор назначения поля
  - [x] Кнопка очистки — быстрый сброс поискового запроса

- [x] **5.3 Pagination компонент**
  - [x] Кнопки Previous/Next — навигация между страницами
  - [x] Выбор размера страницы (50, 100, 200) — dropdown для настройки количества записей
  - [x] Отображение текущей страницы и общего количества — "Страница 1 из 100"
  - [x] Прямой переход на страницу (опционально) — input для ввода номера страницы

- [x] **5.4 ExportButton компонент**
  - [x] Dropdown с выбором формата (JSON/CSV) — меню выбора формата экспорта
  - [x] Скачивание файла — автоматическое сохранение через download attribute
  - [x] Loading state — индикатор генерации файла

- [x] **5.5 RecentFiles компонент**
  - [x] Список недавних файлов/директорий — отображение истории с датой открытия
  - [x] Иконки для файла/директории — визуальное различие типа записи
  - [x] Клик для повторного открытия — быстрый доступ к ранее открытым файлам
  - [x] Отображение в начальном экране — показ когда файл не загружен

- [x] **5.6 Улучшение UX**
  - [x] Loading spinner для таблицы — скелетон или спиннер при загрузке данных
  - [x] Error toast/alert — уведомления об ошибках через shadcn Toast
  - [x] Empty state (нет результатов поиска) — информативный placeholder с иконкой
  - [x] Responsive design (адаптивная верстка) — корректное отображение на мобильных

**Результат фазы:** Полнофункциональный UI с фильтрацией, поиском, пагинацией и экспортом. ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] Фильтрация по Level: клик → API запрос → таблица обновляется
- [x] Поиск: ввод текста → debounce 300ms → API запрос
- [x] Пагинация: переключение страниц работает корректно
- [x] Экспорт: скачивается файл с правильным Content-Type
- [x] Responsive: корректное отображение на 320px-1920px

---

### Фаза 6: Real-time обновления
- [x] **6.1 Backend - FileWatcher**
  - [x] Реализовать FileWatcherService с FileSystemWatcher — мониторинг изменений лог-файла
  - [x] Debounce событий изменения файла (200ms) — предотвращение множественных срабатываний при записи
  - [x] Отслеживание нескольких сессий одновременно — Dictionary<sessionId, FileWatcher>
  - [x] Корректная остановка при закрытии сессии — освобождение FileSystemWatcher и ресурсов

- [x] **6.2 Backend - SignalR Hub**
  - [x] Создать LogWatcherHub — хаб для real-time коммуникации с клиентами
  - [x] Метод JoinSession(sessionId) — добавление клиента в группу сессии + привязка connectionId к сессии
  - [x] Метод LeaveSession(sessionId) — удаление клиента из группы + отвязка от сессии
  - [x] Событие NewLogs — отправка новых записей всем подписчикам группы (метод SendNewLogs готов для FileWatcherService)
  - [x] Настройка SignalR в Program.cs — регистрация middleware и маршрутов

- [x] **6.3 Backend - Управление жизненным циклом сессий через SignalR**
  - [x] Реализовать OnDisconnectedAsync — удаление сессии при разрыве соединения (закрытие вкладки)
  - [x] Реализовать BindConnectionAsync в ISessionStorage — привязка connectionId к sessionId
  - [x] Реализовать UnbindConnectionAsync — отвязка при явном LeaveSession
  - [x] Реализовать GetSessionByConnectionAsync — получение sessionId по connectionId
  - [x] Хранение маппинга connectionId ↔ sessionId в ConcurrentDictionary
  - [x] Логика: пока SignalR connected → сессия живёт бесконечно; disconnected → сессия удаляется
  - [x] Fallback: если SignalR не подключен (старый клиент) — используется TTL из Фазы 2

- [x] **6.4 Frontend - SignalR клиент**
  - [x] Установить @microsoft/signalr — официальный npm пакет для SignalR
  - [x] Создать signalr.ts — singleton менеджер подключения с автореконнектом
  - [x] Создать composable useFileWatcher(sessionId) — Vue composable для подписки на обновления
  - [x] Автоматическое переподключение при разрыве — exponential backoff при потере соединения

- [x] **6.5 Интеграция**
  - [x] При открытии файла — подписка на обновления — вызов JoinSession после успешного открытия
  - [x] При получении NewLogs — добавление в store — append новых записей без перезагрузки
  - [x] Индикатор "Live" в UI — зелёный badge показывающий активное соединение
  - [x] При закрытии/смене файла — отписка — вызов LeaveSession и очистка состояния

- [x] **6.6 Тестирование SignalR Hub** (Backend)
  - [x] Тест: JoinSession с валидным sessionId — успешное подключение
  - [x] Тест: JoinSession с невалидным sessionId — возврат ошибки
  - [x] Тест: JoinSession с невалидным форматом — возврат ошибки
  - [x] Тест: LeaveSession — удаление сессии и отвязка connectionId
  - [x] Тест: OnDisconnectedAsync — автоматическое удаление сессии при разрыве соединения
  - [x] Тест: несколько клиентов в одной сессии — поддержка multiple connections
  - [x] Тест: привязка connectionId к сессии — корректное связывание
  - [x] Тест: полный lifecycle — connect → join → leave → disconnect
- [x] **6.7 Тестирование FileWatcher и интеграции**
  - [x] Тест: изменение файла → появление новых записей — проверка полного цикла обновления
  - [x] Тест: переподключение при разрыве соединения — симуляция потери сети
  - [x] Нагрузочное тестирование — проверка при частых изменениях файла (10+ в секунду)

**Результат фазы:** Автоматическое обновление логов при изменении файла. Сессии живут пока открыта вкладка (SignalR connected), удаляются при закрытии. ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] Изменение файла → новые записи появляются в UI < 1 сек
- [x] SignalR reconnect: потеря сети → восстановление → данные синхронизированы
- [x] Закрытие вкладки → сессия удаляется (проверить через API)
- [x] Индикатор "Live" отображается при активном соединении
- [x] Нагрузочный тест: 10 изменений/сек → UI не тормозит

---

### Фаза 7: Скрипты запуска и конфигурация
- [x] **7.1 Скрипты запуска**
  - [x] `start-dev.bat` (Windows) — запуск backend + frontend с hot reload в двух терминалах
  - [x] `build.bat` (Windows) — полная сборка проекта (frontend + backend)
  - [x] `start-dev.sh` (Linux Bash) — аналогичный скрипт для Unix-систем
  - [x] `stop.bat` / `stop.sh` — остановка всех процессов

- [x] **7.2 Конфигурация production**
  - [x] `appsettings.Production.json` — настройки для production режима
  - [x] Сборка frontend в `dist/` — `npm run build` генерирует статику
  - [x] Раздача статики через Kestrel — UseStaticFiles и UseDefaultFiles для `wwwroot/`
  - [x] Копирование `client/dist/` в `wwwroot/` при production запуске

- [x] **7.3 Единый скрипт сборки**
  - [x] `build.bat` (Windows) — полная сборка проекта
  - [x] Этап 1: `npm run build` — сборка frontend
  - [x] Этап 2: копирование `dist/` → `src/nLogMonitor.Api/wwwroot/`
  - [x] Этап 3: `dotnet publish -c Release` — сборка backend
  - [x] Результат: готовый к запуску `publish/` каталог
  - [x] `build.sh` (Linux) — аналогичный скрипт для Unix-систем

- [x] **7.4 Документация**
  - [x] Инструкции по запуску в README — шаги для development и production
  - [x] Описание скриптов и их параметров
  - [x] Переменные окружения (.env.example) — шаблон с описанием

- [x] **7.5 Метрики и мониторинг (минимальный набор)**
  - [x] GET /api/metrics — JSON endpoint с базовыми метриками
  - [x] Количество активных сессий — `sessions_active_count`
  - [x] Общий размер данных в памяти — `sessions_memory_bytes` (примерная оценка)
  - [x] Количество записей по всем сессиям — `logs_total_count`
  - [x] Uptime сервера — `server_uptime_seconds`
  - [x] Количество SignalR подключений — `signalr_connections_count`

**Результат фазы:** Приложение запускается одной командой `start-dev.bat` / `start-dev.sh` (dev) или собирается через `build.bat` / `build.sh` (production). ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] `start-dev.bat` запускает backend и frontend без ошибок (Windows)
- [x] `start-dev.sh` запускает backend и frontend без ошибок (Linux)
- [x] http://localhost:5173 открывает frontend (dev mode)
- [x] http://localhost:5000 открывает frontend (production mode, статика из wwwroot)
- [x] http://localhost:5000/health возвращает 200
- [x] GET /api/metrics возвращает JSON с метриками сервера

---

### Фаза 8: Client-side Logging
- [x] **8.1 Backend - ClientLogsController**
  - [x] POST /api/client-logs — приём batch логов с фронтенда в JSON формате
  - [x] Валидация Level (обязательное поле) — проверка наличия уровня логирования
  - [x] Нормализация алиасов — преобразование warning→warn, fatal/critical→error
  - [x] Rate limiting для защиты от спама — ограничение количества запросов на IP (100 req/min per IP)

- [x] **8.2 Backend - логирование**
  - [x] Запись в общий лог-файл с префиксом [CLIENT] — идентификация источника логов
  - [x] Structured logging с контекстом — добавление userId, version, url в записи
  - [x] Санитизация входных данных — защита от XSS и инъекций в логах

- [x] **8.3 Frontend - ClientLogger service**
  - [x] Методы: trace, debug, info, warn, error, fatal, exception — API аналогичный NLog
  - [x] Буферизация логов (batchSize: 10) — накопление перед отправкой
  - [x] Автоматический flush по таймеру (5 сек) — гарантия отправки даже при малом трафике
  - [x] Retry с exponential backoff (maxRetries: 3) — повтор при ошибках сети

- [x] **8.4 Frontend - глобальный контекст**
  - [x] setGlobalContext({ userId, version, sessionId }) — установка метаданных для всех логов
  - [x] Автоматическое добавление url, userAgent — сбор информации о браузере

- [x] **8.5 Frontend - error handlers**
  - [x] window.onerror — перехват глобальных ошибок JavaScript
  - [x] window.onunhandledrejection — перехват необработанных Promise rejection
  - [x] Vue Error Boundary (errorCaptured hook) — перехват ошибок в компонентах Vue (app.config.errorHandler)

- [x] **8.6 Тестирование**
  - [x] Integration tests для /api/client-logs — 23 интеграционных теста
  - [x] Тесты валидации (Level, Message обязательные, лимиты длины)
  - [x] Тесты нормализации уровней (warning→warn, fatal→error, critical→error)

**Результат фазы:** Ошибки с фронтенда автоматически отправляются на сервер. ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] console.error в браузере → запись в серверном логе с [CLIENT] префиксом
- [x] Unhandled Promise rejection → логируется на сервере
- [x] Rate limiting: >100 req/min от одного IP → 429 Too Many Requests
- [x] Batch отправка: 10 логов накапливаются → один POST запрос

**Статистика тестов:** 306 тестов (Infrastructure: 134, Application: 28, Api: 144)

---

### Фаза 9: Photino Desktop
- [x] **9.1 Создание Desktop проекта**
  - [x] Создать nLogMonitor.Desktop (console → winexe) — проект без консольного окна
  - [x] Добавить Photino.NET NuGet пакет — кроссплатформенный WebView wrapper
  - [x] Reference на nLogMonitor.Application и nLogMonitor.Infrastructure — использование embedded web server

- [x] **9.2 Program.cs - основа**
  - [x] Запуск embedded ASP.NET Core в фоновом потоке — self-hosted Kestrel внутри приложения
  - [x] Создание PhotinoWindow — нативное окно с WebView
  - [x] Загрузка index.html (production) или localhost:5173 (dev) — переключение по конфигурации
  - [x] RegisterWebMessageReceivedHandler для IPC — обработка сообщений от JavaScript

- [x] **9.3 Нативные диалоги (Photino built-in, кроссплатформенные)**
  - [x] ShowOpenFileDialog — `PhotinoWindow.ShowOpenFile()` с фильтрами `[("Log files", "*.log"), ("All files", "*.*")]`
  - [x] ShowOpenFolderDialog — `PhotinoWindow.ShowOpenFolder()` для выбора директории
  - [x] Обработка результата — проверка на null (пользователь отменил диалог)

- [x] **9.4 JS ↔ .NET Bridge**
  - [x] Message handler для команд — обработка showOpenFile, showOpenFolder, isDesktop, getServerPort
  - [x] JSON сериализация запросов/ответов — структурированный обмен данными (BridgeRequest/BridgeResponse)
  - [x] Отправка результата обратно в WebView — callback через SendWebMessage

- [x] **9.5 Frontend - usePhotinoBridge composable**
  - [x] Определение режима (isDesktop) — проверка наличия window.external.sendMessage
  - [x] showOpenFileDialog() → Promise<string | null> — async wrapper над нативным диалогом
  - [x] showOpenFolderDialog() → Promise<string | null> — async wrapper над нативным диалогом
  - [x] Fallback на web-версию если не desktop — graceful degradation

- [x] **9.6 FileSelector - режимы работы**
  - [x] Web: input[type=file] — стандартный input для выбора файла с drag & drop
  - [x] Desktop: нативные кнопки "Открыть файл" / "Открыть директорию" — системные диалоги
  - [x] Переключение на основе isDesktop — автоматическое определение режима

- [x] **9.7 Сборка и публикация**
  - [x] npm run build → client/dist — production сборка фронтенда
  - [x] Копирование dist в wwwroot — встраивание статики в Desktop приложение
  - [x] dotnet publish -c Release -r win-x64 --self-contained — публикация автономного exe (~50 MB)
  - [x] Скрипты сборки: build-desktop.bat (Windows), build-desktop.sh (Linux)

**Результат фазы:** Desktop приложение с нативными диалогами. ✅ ЗАВЕРШЕНО

**Definition of Done (DoD):**
- [x] Windows: exe создан, готов к запуску (~50 MB)
- [ ] Linux: AppImage/deb запускается на Ubuntu 22.04 (требует тестирования на Linux)
- [x] Все функции Web-версии работают в Desktop (код реализован)
- [x] Размер exe (self-contained) < 100MB (~50 MB)

**Статистика тестов:** 306 тестов (Infrastructure: 134, Application: 28, Api: 144)

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
  - [ ] Cross-platform testing Desktop (Windows, Linux) — проверка Photino на поддерживаемых ОС

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
./nLogMonitor.Api            # Linux
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
  - [ ] Шифрование сохранённых учётных данных — DPAPI (Windows) / Secret Service (Linux)
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
