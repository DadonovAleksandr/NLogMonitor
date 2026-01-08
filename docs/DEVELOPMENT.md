# 👩‍💻 Руководство по разработке

## 📋 Содержание

- [Требования](#-требования)
- [Быстрый старт](#-быстрый-старт)
- [Структура проекта](#-структура-проекта)
- [Команды разработки](#-команды-разработки)
- [Тестирование](#-тестирование)
- [Code Style](#-code-style)
- [Отладка](#-отладка)

---

## 📦 Требования

| Инструмент | Версия | Назначение |
|------------|--------|------------|
| .NET SDK | 9.0+ | Backend runtime |
| Node.js | 20+ | Frontend runtime |
| npm/pnpm | latest | Package manager |
| Git | 2.40+ | Version control |
| VS Code / Rider | latest | IDE (рекомендуется) |

### Рекомендуемые расширения VS Code

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
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
git clone https://github.com/YOUR_USERNAME/NLogMonitor.git
cd NLogMonitor
```

### 2. Backend

```bash
# Восстановление зависимостей
dotnet restore

# Запуск в режиме разработки
dotnet run --project src/NLogMonitor.Api

# С hot reload
dotnet watch run --project src/NLogMonitor.Api
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
NLogMonitor/
├── src/                          # Backend source
│   ├── NLogMonitor.Domain/       # Entities, Enums
│   ├── NLogMonitor.Application/  # Services, DTOs
│   ├── NLogMonitor.Infrastructure/ # Implementations
│   └── NLogMonitor.Api/          # Controllers
├── client/                       # Frontend source
│   ├── src/
│   │   ├── components/           # React components
│   │   ├── store/                # Zustand store
│   │   ├── api/                  # API client
│   │   └── types/                # TypeScript types
│   └── public/
├── tests/                        # Unit/Integration tests
└── docs/                         # Documentation
```

---

## 🔧 Команды разработки

### Backend

| Команда | Описание |
|---------|----------|
| `dotnet build` | Сборка проекта |
| `dotnet run --project src/NLogMonitor.Api` | Запуск API |
| `dotnet watch run --project src/NLogMonitor.Api` | Запуск с hot reload |
| `dotnet test` | Запуск всех тестов |
| `dotnet format` | Форматирование кода |

### Frontend

| Команда | Описание |
|---------|----------|
| `npm run dev` | Dev-сервер с HMR |
| `npm run build` | Production сборка |
| `npm run preview` | Превью production сборки |
| `npm run lint` | ESLint проверка |
| `npm run lint:fix` | ESLint с автоисправлением |
| `npm run type-check` | TypeScript проверка |

---

## 🧪 Тестирование

### Backend тесты

```bash
# Все тесты
dotnet test

# С покрытием
dotnet test --collect:"XPlat Code Coverage"

# Конкретный проект
dotnet test tests/NLogMonitor.Application.Tests

# Фильтрация по имени
dotnet test --filter "FullyQualifiedName~LogService"
```

Структура тестов:

```
tests/
├── NLogMonitor.Domain.Tests/
│   └── Entities/
│       └── LogEntryTests.cs
├── NLogMonitor.Application.Tests/
│   └── Services/
│       └── LogServiceTests.cs
└── NLogMonitor.Infrastructure.Tests/
    └── Parser/
        └── NLogParserTests.cs
```

### Frontend тесты

```bash
cd client

# Unit тесты
npm run test

# Watch mode
npm run test:watch

# Coverage
npm run test:coverage
```

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
    "plugin:react-hooks/recommended",
    "prettier"
  ],
  "rules": {
    "@typescript-eslint/no-unused-vars": "error",
    "react-hooks/exhaustive-deps": "warn"
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
      "program": "${workspaceFolder}/src/NLogMonitor.Api/bin/Debug/net9.0/NLogMonitor.Api.dll",
      "cwd": "${workspaceFolder}/src/NLogMonitor.Api",
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
    console.debug(`[NLogMonitor] ${msg}`, ...args),
  error: (msg: string, ...args: unknown[]) =>
    console.error(`[NLogMonitor] ${msg}`, ...args),
};
```

---

## 🔗 Связанные документы

- [Architecture](ARCHITECTURE.md)
- [API Reference](API.md)
- [Contributing](CONTRIBUTING.md)
