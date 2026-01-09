# 👩‍💻 Руководство по разработке

## 📋 Содержание

- [Требования](#-требования)
- [Быстрый старт](#-быстрый-старт)
- [Структура проекта](#-структура-проекта)
- [Команды разработки](#-команды-разработки)
- [Тестирование](#-тестирование)
- [Code Style](#-code-style)
- [Ключевые компоненты](#️-ключевые-компоненты)
- [Отладка](#-отладка)

---

## 📦 Требования

| Инструмент | Версия | Назначение |
|------------|--------|------------|
| .NET SDK | 10.0+ | Backend runtime |
| Node.js | 20+ | Frontend runtime |
| npm/pnpm | latest | Package manager |
| Git | 2.40+ | Version control |
| VS Code / Rider | latest | IDE (рекомендуется) |

### Рекомендуемые расширения VS Code

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
    "vue.volar",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "bradlc.vscode-tailwindcss"
  ]
}
```

---

## 🚀 Быстрый старт

### 1. Клонирование

```bash
git clone https://github.com/YOUR_USERNAME/nLogMonitor.git
cd nLogMonitor
```

### 2. Backend

```bash
# Восстановление зависимостей
dotnet restore

# Запуск в режиме разработки
dotnet run --project src/nLogMonitor.Api

# С hot reload
dotnet watch run --project src/nLogMonitor.Api
```

API будет доступен на `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

### 3. Frontend

```bash
cd client

# Установка зависимостей
npm install

# Запуск dev-сервера
npm run dev
```

UI будет доступен на `http://localhost:5173`

### 4. Одновременный запуск (опционально)

Создайте `docker-compose.dev.yml`:

```yaml
version: '3.8'
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile.dev
    ports:
      - "5000:5000"
    volumes:
      - ./src:/app/src
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

  client:
    build:
      context: ./client
      dockerfile: Dockerfile.dev
    ports:
      - "5173:5173"
    volumes:
      - ./client/src:/app/src
```

---

## 📁 Структура проекта

```
nLogMonitor/
├── src/                          # Backend source
│   ├── nLogMonitor.Domain/       # Entities, Enums
│   │   └── Entities/             # LogEntry, LogSession, LogLevel, RecentLogEntry
│   ├── nLogMonitor.Application/  # Interfaces, DTOs, Services
│   │   ├── Configuration/        # SessionSettings, FileSettings
│   │   ├── DTOs/                 # LogEntryDto, FilterOptionsDto, PagedResultDto, etc.
│   │   ├── Exceptions/           # NoLogFilesFoundException
│   │   ├── Interfaces/           # ILogParser, ISessionStorage, ILogService, etc.
│   │   └── Services/             # LogService
│   ├── nLogMonitor.Infrastructure/ # Implementations
│   │   ├── Parsing/              # NLogParser
│   │   ├── Storage/              # InMemorySessionStorage
│   │   └── FileSystem/           # DirectoryScanner
│   ├── nLogMonitor.Api/          # Controllers, Hubs
│   └── nLogMonitor.Desktop/      # Photino shell (планируется)
├── client/                       # Frontend source (Vue 3)
│   ├── src/
│   │   ├── components/           # Vue components (ui/, LogTable/, FileSelector/)
│   │   ├── stores/               # Pinia stores (logStore, filterStore, recentStore)
│   │   ├── api/                  # Axios API client
│   │   ├── lib/                  # Utility functions
│   │   └── types/                # TypeScript types
│   └── public/
├── tests/                        # Unit/Integration tests (240 тестов)
│   ├── nLogMonitor.Infrastructure.Tests/  # 113 тестов
│   │   ├── Parsing/              # NLogParserTests
│   │   ├── Storage/              # InMemorySessionStorageTests, RecentLogsFileRepositoryTests
│   │   ├── FileSystem/           # DirectoryScannerTests
│   │   └── Export/               # JsonExporterTests, CsvExporterTests
│   ├── nLogMonitor.Application.Tests/     # 28 тестов
│   │   └── Services/             # LogServiceTests
│   └── nLogMonitor.Api.Tests/             # 99 тестов
│       ├── Controllers/          # LogsControllerTests
│       ├── Validators/           # FilterOptionsValidatorTests
│       └── Integration/          # Интеграционные тесты (WebApplicationFactory)
└── docs/                         # Documentation
```

---

## 🔧 Команды разработки

### Backend

| Команда | Описание |
|---------|----------|
| `dotnet build` | Сборка проекта |
| `dotnet run --project src/nLogMonitor.Api` | Запуск API |
| `dotnet watch run --project src/nLogMonitor.Api` | Запуск с hot reload |
| `dotnet test` | Запуск всех тестов |
| `dotnet format` | Форматирование кода |

### Frontend

| Команда | Описание |
|---------|----------|
| `npm run dev` | Dev-сервер с HMR |
| `npm run build` | Production сборка (включает vue-tsc проверку типов) |
| `npm run preview` | Превью production сборки |

---

## 🧪 Тестирование

### Тестовые проекты

| Проект | Тестов | Описание |
|--------|--------|----------|
| `nLogMonitor.Infrastructure.Tests` | 113 | Тесты парсера, хранилища, файловой системы, экспортеров |
| `nLogMonitor.Application.Tests` | 28 | Тесты LogService (бизнес-логика) |
| `nLogMonitor.Api.Tests` | 99 | Unit тесты контроллеров + Integration тесты с WebApplicationFactory |
| **Всего** | **240** | |

**Фреймворк:** NUnit 3.x + Moq

### Команды запуска тестов

```bash
# Все тесты
dotnet test

# С подробным выводом
dotnet test -v normal

# С покрытием (coverlet)
dotnet test --collect:"XPlat Code Coverage"

# Конкретный тестовый проект
dotnet test tests/nLogMonitor.Infrastructure.Tests
dotnet test tests/nLogMonitor.Application.Tests

# Фильтрация по имени теста
dotnet test --filter "FullyQualifiedName~LogService"
dotnet test --filter "FullyQualifiedName~NLogParser"
dotnet test --filter "Name~ParseAsync"

# Фильтрация по категории (если используются атрибуты)
dotnet test --filter "Category=UnitTest"
```

### Структура тестов

```
tests/
├── nLogMonitor.Infrastructure.Tests/
│   ├── Parsing/
│   │   └── NLogParserTests.cs           # 17 тестов парсера NLog
│   ├── Storage/
│   │   ├── InMemorySessionStorageTests.cs  # 19 тестов хранилища сессий
│   │   └── RecentLogsFileRepositoryTests.cs # 18 тестов репозитория недавних
│   ├── FileSystem/
│   │   └── DirectoryScannerTests.cs     # 20 тестов сканера директорий
│   └── Export/
│       ├── JsonExporterTests.cs         # Тесты JSON экспортера
│       └── CsvExporterTests.cs          # Тесты CSV экспортера
├── nLogMonitor.Application.Tests/
│   └── Services/
│       └── LogServiceTests.cs           # 28 тестов бизнес-логики
└── nLogMonitor.Api.Tests/
    ├── Controllers/
    │   └── LogsControllerTests.cs       # 17 тестов контроллера логов
    ├── Validators/
    │   └── FilterOptionsValidatorTests.cs # 39 тестов валидатора
    └── Integration/                     # 24 интеграционных теста
        ├── WebApplicationTestBase.cs    # Базовый класс с WebApplicationFactory
        ├── FilesControllerIntegrationTests.cs  # Desktop-only эндпоинты
        ├── UploadControllerIntegrationTests.cs # Path traversal защита
        ├── ExportControllerIntegrationTests.cs # Потоковый экспорт
        └── HealthCheckIntegrationTests.cs      # Health endpoint
```

### Что покрывают тесты

- **NLogParserTests:** Парсинг разных форматов, многострочные записи, edge cases, производительность
- **InMemorySessionStorageTests:** CRUD операций, TTL/sliding expiration, очистка сессий, connection binding
- **RecentLogsFileRepositoryTests:** Хранение, лимиты, персистентность, thread-safety
- **DirectoryScannerTests:** Поиск файлов, сортировка по имени, фильтрация по расширениям
- **JsonExporterTests / CsvExporterTests:** Экспорт с фильтрацией, форматирование, кодировки
- **LogServiceTests:** Открытие файлов/директорий, фильтрация, пагинация, обработка ошибок
- **LogsControllerTests:** Маппинг DTO, обработка ошибок, интеграция с сервисом
- **FilterOptionsValidatorTests:** Валидация всех параметров фильтрации, граничные случаи

### Frontend тесты

> **Примечание:** Frontend тестирование планируется в Фазе 10 (E2E тесты с Playwright).

---

## 🎨 Code Style

### C# (Backend)

Используем `.editorconfig` и `dotnet format`:

```ini
# .editorconfig
[*.cs]
indent_style = space
indent_size = 4
csharp_style_namespace_declarations = file_scoped:suggestion
csharp_style_var_for_built_in_types = true:suggestion
```

### TypeScript (Frontend)

ESLint + Prettier:

```json
// .eslintrc.json
{
  "extends": [
    "eslint:recommended",
    "plugin:@typescript-eslint/recommended",
    "plugin:vue/vue3-recommended",
    "prettier"
  ],
  "rules": {
    "@typescript-eslint/no-unused-vars": "error",
    "vue/multi-word-component-names": "off"
  }
}
```

```json
// .prettierrc
{
  "semi": true,
  "singleQuote": true,
  "tabWidth": 2,
  "trailingComma": "es5"
}
```

### Git Hooks (Husky)

```bash
cd client
npm install husky lint-staged -D
npx husky install
npx husky add .husky/pre-commit "npx lint-staged"
```

```json
// package.json
{
  "lint-staged": {
    "*.{ts,tsx}": ["eslint --fix", "prettier --write"],
    "*.{json,md}": ["prettier --write"]
  }
}
```

---

## 🏗️ Ключевые компоненты

Краткое описание основных классов для разработчиков.

### NLogParser

**Путь:** `src/nLogMonitor.Infrastructure/Parsing/NLogParser.cs`

Высокопроизводительный парсер NLog-файлов с поддержкой многострочных записей.

**Особенности:**
- Поддержка формата: `${longdate}|${level}|${message}|${logger}|${processid}|${threadid}`
- Поиск разделителей **с конца строки** (logger, processid, threadid фиксированы)
- Поддержка многострочных сообщений (message может содержать `\n` и `|`)
- Два режима парсинга: быстрый (Span-based) и fallback (regex)
- `IAsyncEnumerable<LogEntry>` для streaming больших файлов
- 64KB буфер для оптимального I/O

```csharp
// Пример использования
await foreach (var entry in parser.ParseAsync(filePath, cancellationToken))
{
    // Обработка LogEntry
}
```

### InMemorySessionStorage

**Путь:** `src/nLogMonitor.Infrastructure/Storage/InMemorySessionStorage.cs`

In-memory хранилище сессий логов с автоматической очисткой по TTL.

**Особенности:**
- Thread-safe на базе `ConcurrentDictionary`
- Sliding expiration (TTL продлевается при каждом доступе)
- Автоматическая очистка просроченных сессий по таймеру
- Binding SignalR connectionId -> sessionId
- Настраиваемый TTL и интервал очистки через `SessionSettings`

```csharp
// Конфигурация (appsettings.json)
"SessionSettings": {
    "FallbackTtlMinutes": 5,
    "CleanupIntervalMinutes": 1
}
```

### LogService

**Путь:** `src/nLogMonitor.Application/Services/LogService.cs`

Основной сервис бизнес-логики для работы с логами.

**Методы:**
- `OpenFileAsync(filePath)` — открытие файла, парсинг, создание сессии
- `OpenDirectoryAsync(directoryPath)` — поиск последнего лог-файла и его открытие
- `GetLogsAsync(sessionId, filters, pagination)` — получение логов с фильтрацией и пагинацией
- `GetSessionAsync(sessionId)` — получение метаданных сессии

**Особенности:**
- Серверная фильтрация (LINQ over in-memory collection)
- Подсчёт записей по уровням (`LevelCounts`)
- Логирование всех операций

### DirectoryScanner

**Путь:** `src/nLogMonitor.Infrastructure/FileSystem/DirectoryScanner.cs`

Сканер директорий для поиска лог-файлов.

**Методы:**
- `FindLastLogFileByNameAsync(directoryPath)` — поиск последнего файла по имени (сортировка Z-A)

**Особенности:**
- Фильтрация по расширениям (`.log`, `.txt` по умолчанию)
- Поддержка настройки через `FileSettings`

---

## 🐛 Отладка

### VS Code Launch Configuration

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/nLogMonitor.Api/bin/Debug/net10.0/nLogMonitor.Api.dll",
      "cwd": "${workspaceFolder}/src/nLogMonitor.Api",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    {
      "name": "Launch Chrome",
      "type": "chrome",
      "request": "launch",
      "url": "http://localhost:5173",
      "webRoot": "${workspaceFolder}/client/src"
    }
  ],
  "compounds": [
    {
      "name": "Full Stack",
      "configurations": ["Launch API", "Launch Chrome"]
    }
  ]
}
```

### Логирование

Backend использует NLog:

```xml
<!-- NLog.config -->
<targets>
  <target name="console" xsi:type="Console"
          layout="${longdate}|${level:uppercase=true}|${message}|${logger}" />
</targets>
<rules>
  <logger name="*" minlevel="Debug" writeTo="console" />
</rules>
```

Frontend использует console с prefix:

```typescript
const logger = {
  debug: (msg: string, ...args: unknown[]) =>
    console.debug(`[nLogMonitor] ${msg}`, ...args),
  error: (msg: string, ...args: unknown[]) =>
    console.error(`[nLogMonitor] ${msg}`, ...args),
};
```

---

## 🔗 Связанные документы

- [Architecture](ARCHITECTURE.md)
- [API Reference](API.md)
- [Contributing](CONTRIBUTING.md)
