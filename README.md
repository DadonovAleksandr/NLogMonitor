# nLogMonitor

Кроссплатформенное веб-приложение для просмотра и анализа NLog-логов.

**Технологии:** .NET 10 + Vue 3 + TypeScript + SignalR

## Features

- Загрузка и парсинг NLog-файлов (drag & drop, до 100MB)
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
start-dev.bat

# Linux/macOS
./start-dev.sh
```

После запуска:
- Frontend: http://localhost:5173
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### Production Build

```bash
# Windows
build.bat

# Linux/macOS
./build.sh

# Результат в папке ./publish
cd publish
./nLogMonitor.Api     # Linux/macOS
nLogMonitor.Api.exe   # Windows
```

Production доступен на http://localhost:5000

## Scripts

| Скрипт | Платформа | Описание |
|--------|-----------|----------|
| `start-dev.bat` | Windows | Запуск backend + frontend в режиме разработки с hot reload |
| `start-dev.sh` | Linux/macOS | Запуск backend + frontend в режиме разработки с hot reload |
| `build.bat` | Windows | Сборка production: frontend -> wwwroot -> publish |
| `build.sh` | Linux/macOS | Сборка production: frontend -> wwwroot -> publish |
| `stop.bat` | Windows | Остановка всех запущенных процессов |
| `stop.sh` | Linux/macOS | Остановка всех запущенных процессов |

### Ручные команды

```bash
# Backend
dotnet build                              # Сборка
dotnet run --project src/nLogMonitor.Api  # Запуск
dotnet watch run --project src/nLogMonitor.Api  # Hot reload
dotnet test                               # Тесты (283)

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
| `/health` | GET | Health check |

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
| `pageSize` | int | Размер страницы (default: 50, max: 200) |

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
│   └── nLogMonitor.Infrastructure/# Реализации (Parser, Storage, Export, FileWatcher)
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
├── start-dev.bat                  # Скрипт запуска для разработки
├── build.bat                      # Скрипт production сборки
└── docs/                          # Документация
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
