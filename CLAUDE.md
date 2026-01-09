# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NLogMonitor — кроссплатформенное приложение для просмотра и анализа NLog-логов. Full-stack проект с Clean Architecture: .NET 10 Backend + Vue 3/TypeScript Frontend. Работает в двух режимах: Web (Docker) и Desktop (Photino).

**Текущий статус:** Фаза 1 завершена — базовая инфраструктура проекта (Clean Architecture, решение, проекты).

## Build & Run Commands

```bash
# Backend
dotnet restore                            # Восстановить зависимости
dotnet build                              # Сборка всего solution
dotnet run --project src/NLogMonitor.Api  # Запуск API (localhost:5000)
dotnet watch run --project src/NLogMonitor.Api  # Hot reload

# Tests (NUnit)
dotnet test                               # Все тесты
dotnet test tests/NLogMonitor.Application.Tests  # Конкретный проект
dotnet test --filter "FullyQualifiedName~TestMethodName"  # Один тест по имени

# Frontend (после создания client/)
cd client && npm install                  # Установить зависимости
npm run dev                               # Dev сервер (localhost:5173)
npm run build                             # Production сборка
npm run test                              # Frontend тесты

# Docker (после настройки)
docker-compose up -d --build              # Production запуск
docker-compose logs -f api                # Просмотр логов API
docker-compose down                       # Остановка

# Desktop Photino (после создания NLogMonitor.Desktop)
dotnet publish src/NLogMonitor.Desktop -c Release -r win-x64 --self-contained
```

## Architecture

**Clean Architecture** с 4 слоями (зависимости только внутрь):

```
Presentation (Api, client/, Desktop)
    ↓
Application (Services, DTOs, Interfaces)
    ↓
Domain (Entities: LogEntry, LogSession, LogLevel, RecentLogEntry)
    ↑
Infrastructure (Parser, Storage, Export, FileWatcher) - реализует интерфейсы Application
```

### Backend (src/)
- **NLogMonitor.Domain** — сущности без зависимостей (LogEntry, LogSession, LogLevel enum, RecentLogEntry)
- **NLogMonitor.Application** — интерфейсы (ILogParser, ISessionStorage, IFileWatcherService), сервисы, DTOs
- **NLogMonitor.Infrastructure** — реализации: NLogParser (regex), InMemorySessionStorage (TTL 1 час), FileWatcherService, Json/CsvExporter
- **NLogMonitor.Api** — контроллеры (Files, Upload, Logs, Export, Recent, ClientLogs), SignalR Hub, DI в Program.cs
- **NLogMonitor.Desktop** — Photino shell с нативными диалогами

### Frontend (client/src/)
- **components/** — Vue компоненты (FileSelector, LogTable, FilterPanel, SearchBar) + shadcn-vue
- **stores/** — Pinia stores (logStore, filterStore, recentStore)
- **api/** — Axios клиент, API методы, SignalR клиент
- **composables/** — Vue composables (useLogs, useFileWatcher, usePhotinoBridge)
- **services/** — ClientLogger для отправки логов на сервер

## Key Patterns

- **IAsyncEnumerable** для streaming парсинга больших файлов
- **Pinia** для state management на фронтенде
- **TTL сессии** — автоматическое удаление через 1 час
- **Virtual scrolling** — таблица для миллионов записей
- **Debounce 300ms** — для поиска
- **SignalR** — real-time обновления при изменении файла
- **FileSystemWatcher** — мониторинг изменений лог-файла

## NLog Format

Поддерживаемый формат:
```
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
```

**Важно:** Сообщение может быть **многострочным** (stack traces, многострочные данные). Парсинг многострочных записей:
- Новая запись определяется по дате в начале строки (`^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4}`)
- Разделители `|` ищутся **с конца** строки (logger, processid, threadid фиксированы)
- Message может содержать `\n` и `|`

Пример однострочной: `2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1`

Имя файла: `${shortdate}.log` (например: `2024-01-15.log`)

## API Endpoints

- `POST /api/files/open` — открытие файла по пути (Desktop)
- `POST /api/files/open-directory` — открытие директории (выбор последнего по имени файла)
- `POST /api/upload` — загрузка файла через браузер (Web, max 100MB)
- `GET /api/logs/{sessionId}` — получение логов с фильтрацией и пагинацией
- `GET /api/export/{sessionId}?format=json|csv` — экспорт
- `GET /api/recent` — список недавних файлов
- `POST /api/client-logs` — приём логов с фронтенда

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, SignalR, FluentValidation, NLog
- **Frontend:** Vue 3, TypeScript 5, Vite, Pinia, TanStack Table, Tailwind CSS, shadcn-vue
- **Desktop:** Photino.NET
- **Testing:** NUnit 3.x, Moq, coverlet (code coverage)
- **Infrastructure:** Docker, docker-compose, Nginx

## Development Status

Проект в активной разработке. Полный план — см. `PLAN.md`.

**Завершённые фазы:**
- [x] Фаза 1: Базовая инфраструктура (Clean Architecture, solution structure)

**Следующие фазы:**
- [ ] Фаза 2: Парсинг и хранение логов
- [ ] Фаза 3: REST API
- [ ] Фаза 4-5: Frontend (Vue 3)
- [ ] Фаза 6: Оптимизация производительности
- [ ] Фаза 7: Docker и CI/CD

**Будущие возможности (Фазы 11-12):**
- Удалённый доступ по SSH (SSH.NET, SFTP, `tail -f`)
- Dashboard с мониторингом нескольких лог-файлов
