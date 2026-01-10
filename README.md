# nLogMonitor

Кроссплатформенное приложение для просмотра и анализа NLog-логов. Работает в двух режимах: Web (браузер) и Desktop (нативное приложение с Photino.NET).

**Технологии:** .NET 10 + Vue 3 + TypeScript + SignalR + Photino.NET

## Features

- Загрузка и парсинг NLog-файлов (drag & drop, до 100MB)
- Desktop режим с нативным окном (Photino.NET)
- Нативные диалоги для открытия файлов и директорий (Desktop)
- Прямой доступ к файловой системе без загрузки (Desktop)
- Поддержка директорий с автовыбором последнего .log файла (Desktop)
- Фильтрация по уровням: Trace, Debug, Info, Warn, Error, Fatal
- Полнотекстовый поиск с debounce 300ms
- Серверная пагинация для больших файлов (100K+ записей)
- Real-time обновления через SignalR с инкрементальным чтением
- Экспорт в JSON/CSV (потоковая генерация)
- История недавних файлов (до 20 записей)
- Dark theme по умолчанию

## Requirements

- Node.js 20+
- .NET 10 SDK

## Quick Start

### Development Mode

```bash
# Windows
scripts\start-dev.bat

# Linux/macOS
./scripts/start-dev.sh
```

После запуска:
- Frontend: http://localhost:5173
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### Production Build

```bash
# Windows
scripts\build.bat

# Linux/macOS
./scripts/build.sh

# Результат в папке ./publish
cd publish
./nLogMonitor.Api     # Linux/macOS
nLogMonitor.Api.exe   # Windows
```

Production доступен на http://localhost:5000

### Desktop Build

```bash
# Windows
scripts\build-desktop.bat

# Linux/macOS
./scripts/build-desktop.sh

# Результат в папке ./publish/desktop/win-x64 (или linux-x64)
cd publish/desktop/win-x64
nLogMonitor.Desktop.exe   # Windows
# или
cd publish/desktop/linux-x64
./nLogMonitor.Desktop      # Linux
```

Desktop приложение (~50 MB) включает встроенный веб-сервер и WebView.

### Desktop Development Mode (Hot Reload)

```bash
# Windows — запуск Vite dev server + Desktop с hot reload
scripts\start-desktop-dev.bat

# Остановка всех Desktop dev процессов
scripts\stop-desktop-dev.bat
```

В этом режиме:
- Vite dev server запускается в отдельном окне (http://localhost:5173)
- Desktop приложение загружает frontend с Vite вместо embedded статики
- Изменения в Vue компонентах применяются мгновенно (HMR)

## Scripts

Все скрипты находятся в папке `scripts/`.

| Скрипт | Платформа | Описание |
|--------|-----------|----------|
| `start-dev.bat` | Windows | Запуск backend + frontend в режиме разработки с hot reload |
| `start-dev.sh` | Linux/macOS | Запуск backend + frontend в режиме разработки с hot reload |
| `start-desktop-dev.bat` | Windows | Запуск Desktop + Vite dev server с hot reload |
| `stop-desktop-dev.bat` | Windows | Остановка Desktop dev процессов |
| `build.bat` | Windows | Сборка production: frontend → wwwroot → publish |
| `build.sh` | Linux/macOS | Сборка production: frontend → wwwroot → publish |
| `build-desktop.bat` | Windows | Сборка Desktop приложения (frontend + self-contained exe) |
| `build-desktop.sh` | Linux/macOS | Сборка Desktop приложения (frontend + self-contained binary) |
| `stop.bat` | Windows | Остановка всех запущенных процессов (Web) |
| `stop.sh` | Linux/macOS | Остановка всех запущенных процессов (Web) |

### Ручные команды

```bash
# Backend
dotnet build                              # Сборка
dotnet run --project src/nLogMonitor.Api  # Запуск
dotnet watch run --project src/nLogMonitor.Api  # Hot reload
dotnet test                               # Тесты (306)

# Frontend
cd client
npm install    # Установка зависимостей
npm run dev    # Dev server
npm run build  # Production build
```

## API Endpoints

| Endpoint | Метод | Описание |
|----------|-------|----------|
| `/api/upload` | POST | Загрузка лог-файла (multipart/form-data, max 100MB) |
| `/api/logs/{sessionId}` | GET | Получение логов с фильтрацией и пагинацией |
| `/api/export/{sessionId}` | GET | Экспорт логов (?format=json\|csv) |
| `/api/recent` | GET | Список недавних файлов |
| `/api/recent` | DELETE | Очистка истории |
| `/api/files/open` | POST | Открытие файла по пути (Desktop-only) |
| `/api/files/open-directory` | POST | Открытие директории (Desktop-only) |
| `/api/metrics` | GET | Метрики сервера (sessions, logs, memory, uptime, connections) |
| `/api/client-logs` | POST | Приём логов с фронтенда (batch, rate limit: 100 req/min per IP) |
| `/health` | GET | Health check |

**Примечание:** Эндпоинты `/api/files/open` и `/api/files/open-directory` доступны только в Desktop режиме. В Web режиме возвращают 404.

### SignalR Hub

- Endpoint: `/hubs/log-watcher`
- Методы: `JoinSession`, `LeaveSession`
- События: `NewLogs` (получение новых записей в реальном времени)

### Query Parameters для /api/logs/{sessionId}

| Параметр | Тип | Описание |
|----------|-----|----------|
| `levels` | string[] | Фильтр по уровням (Trace, Debug, Info, Warn, Error, Fatal) |
| `search` | string | Полнотекстовый поиск по сообщению |
| `page` | int | Номер страницы (default: 1) |
| `pageSize` | int | Размер страницы (default: 50, max: 500) |

## Configuration

### appsettings.json

```json
{
  "App": {
    "Mode": "Web"
  },
  "SessionSettings": {
    "FallbackTtlMinutes": 5,
    "CleanupIntervalMinutes": 1
  },
  "FileSettings": {
    "MaxFileSizeMB": 100,
    "AllowedExtensions": [".log", ".txt"],
    "TempDirectory": "./temp"
  },
  "RecentLogsSettings": {
    "MaxEntries": 20
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

### Переменные окружения

| Переменная | Описание | Default |
|------------|----------|---------|
| `ASPNETCORE_URLS` | URL для API сервера | http://localhost:5000 |
| `ASPNETCORE_ENVIRONMENT` | Окружение (Development/Production) | Production |

## Project Structure

```
nLogMonitor/
├── src/
│   ├── nLogMonitor.Api/           # ASP.NET Core Web API + SignalR Hub
│   ├── nLogMonitor.Application/   # Интерфейсы, DTOs, Services
│   ├── nLogMonitor.Domain/        # Entities (LogEntry, LogSession, LogLevel)
│   ├── nLogMonitor.Infrastructure/# Реализации (Parser, Storage, Export, FileWatcher)
│   └── nLogMonitor.Desktop/       # Photino Desktop приложение (embedded ASP.NET Core)
├── tests/
│   ├── nLogMonitor.Api.Tests/     # Integration + Unit тесты API
│   ├── nLogMonitor.Application.Tests/
│   └── nLogMonitor.Infrastructure.Tests/
├── client/                        # Vue 3 + Vite + TypeScript frontend
│   ├── src/
│   │   ├── api/                   # Axios клиент + SignalR
│   │   ├── components/            # UI компоненты
│   │   ├── composables/           # Vue composables (useFileWatcher)
│   │   ├── stores/                # Pinia stores
│   │   └── types/                 # TypeScript типы
│   └── ...
├── scripts/                       # Скрипты запуска и сборки
│   ├── start-dev.bat/sh           # Запуск dev mode (backend + frontend)
│   ├── stop.bat/sh                # Остановка серверов
│   ├── build.bat/sh               # Production сборка (Web)
│   ├── build-desktop.bat/sh       # Сборка Desktop приложения
│   ├── start-desktop-dev.bat      # Desktop dev mode с hot reload
│   └── stop-desktop-dev.bat       # Остановка Desktop dev
└── docs/                          # Документация
```

## Desktop Mode

Desktop приложение создано на базе Photino.NET и включает:

- **Embedded ASP.NET Core** — встроенный веб-сервер работает в фоновом потоке
- **Native WebView** — использует Edge WebView2 (Windows), WebKit (Linux/macOS)
- **System Dialogs** — нативные диалоги открытия файлов через Photino API
- **Direct File Access** — прямой доступ к файловой системе без ограничений на размер
- **Directory Support** — открытие директории с автоматическим выбором последнего .log файла
- **Bridge API** — JS ↔ .NET взаимодействие через window.external

### Отличия от Web режима

| Функция | Web | Desktop |
|---------|:---:|:-------:|
| Открытие файла | Upload (до 100MB) | Системный диалог (без ограничений) |
| Открытие директории | ❌ | ✅ |
| Real-time мониторинг | ✅ | ✅ |
| Экспорт | ✅ | ✅ |
| История файлов | Per-session | Persistent (AppData) |

### Сборка для других платформ

```bash
# Linux x64
dotnet publish src/nLogMonitor.Desktop -c Release -r linux-x64 --self-contained -o publish/desktop/linux-x64

# macOS x64
dotnet publish src/nLogMonitor.Desktop -c Release -r osx-x64 --self-contained -o publish/desktop/osx-x64

# macOS ARM64 (Apple Silicon)
dotnet publish src/nLogMonitor.Desktop -c Release -r osx-arm64 --self-contained -o publish/desktop/osx-arm64
```

## NLog Format

Поддерживаемый формат лог-записей:

```
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
```

Пример:
```
2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1
2024-01-15 10:30:46.5678|ERROR|Connection failed|MyApp.Database|1234|2
System.Exception: Connection refused
   at MyApp.Database.Connect()
```

Парсер корректно обрабатывает многострочные записи (stack traces).

## License

MIT
