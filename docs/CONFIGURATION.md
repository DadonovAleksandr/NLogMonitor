# ⚙️ Конфигурация

## 📋 Содержание

- [Обзор](#-обзор)
- [Backend конфигурация](#-backend-конфигурация)
- [Frontend конфигурация](#-frontend-конфигурация)
- [Переменные окружения](#-переменные-окружения)
- [NLog конфигурация](#-nlog-конфигурация)

---

## 📖 Обзор

NLogMonitor использует иерархическую систему конфигурации:

1. **appsettings.json** — базовые настройки
2. **appsettings.{Environment}.json** — переопределения для окружения
3. **Переменные окружения** — высший приоритет

---

## 🖥️ Backend конфигурация

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "SessionStorage": {
    "DefaultTtlMinutes": 60,
    "MaxFileSizeMb": 100,
    "CleanupIntervalMinutes": 15
  },

  "Parser": {
    "DefaultPattern": "${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}",
    "MaxLineLength": 10000,
    "EnableMultilineMessages": true
  },

  "Api": {
    "DefaultPageSize": 100,
    "MaxPageSize": 1000,
    "EnableSwagger": true
  },

  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Api": {
    "EnableSwagger": true
  }
}
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "Api": {
    "EnableSwagger": false
  },
  "SessionStorage": {
    "DefaultTtlMinutes": 120
  }
}
```

---

## 🎨 Frontend конфигурация

### vite.config.ts

```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
  },
});
```

### .env файлы

```bash
# .env (общие)
VITE_APP_TITLE=NLogMonitor

# .env.development
VITE_API_BASE_URL=http://localhost:5000/api
VITE_ENABLE_DEVTOOLS=true

# .env.production
VITE_API_BASE_URL=/api
VITE_ENABLE_DEVTOOLS=false
```

### Использование в коде

```typescript
const config = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL,
  appTitle: import.meta.env.VITE_APP_TITLE,
  enableDevtools: import.meta.env.VITE_ENABLE_DEVTOOLS === 'true',
};
```

---

## 🔧 Переменные окружения

### Backend

| Переменная | Тип | По умолчанию | Описание |
|------------|-----|--------------|----------|
| `ASPNETCORE_ENVIRONMENT` | string | Production | Окружение (Development, Production) |
| `ASPNETCORE_URLS` | string | http://+:5000 | URL для прослушивания |
| `SessionStorage__DefaultTtlMinutes` | int | 60 | TTL сессий в минутах |
| `SessionStorage__MaxFileSizeMb` | int | 100 | Макс. размер файла в МБ |
| `SessionStorage__CleanupIntervalMinutes` | int | 15 | Интервал очистки |
| `Parser__EnableMultilineMessages` | bool | true | Поддержка многострочных сообщений |
| `Api__DefaultPageSize` | int | 100 | Размер страницы по умолчанию |
| `Api__MaxPageSize` | int | 1000 | Максимальный размер страницы |
| `Api__EnableSwagger` | bool | true | Включить Swagger UI |
| `Cors__AllowedOrigins__0` | string | - | CORS origin |

### Пример docker-compose.yml

```yaml
services:
  app:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - SessionStorage__DefaultTtlMinutes=120
      - SessionStorage__MaxFileSizeMb=200
      - Api__EnableSwagger=false
      - Cors__AllowedOrigins__0=https://myapp.com
```

---

## 📝 NLog конфигурация

### NLog.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwConfigExceptions="true">

  <variable name="logDir" value="${basedir}/logs" />

  <targets>
    <!-- Console -->
    <target name="console" xsi:type="Console"
            layout="${longdate}|${level:uppercase=true}|${message}|${logger}|${exception:format=tostring}" />

    <!-- File -->
    <target name="file" xsi:type="File"
            fileName="${logDir}/nlogmonitor-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}|${exception:format=tostring}"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="30"
            concurrentWrites="true" />

    <!-- Errors only -->
    <target name="errors" xsi:type="File"
            fileName="${logDir}/errors-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${message}|${logger}|${exception:format=tostring}" />
  </targets>

  <rules>
    <!-- Development -->
    <logger name="*" minlevel="Debug" writeTo="console" />

    <!-- All logs -->
    <logger name="*" minlevel="Info" writeTo="file" />

    <!-- Errors -->
    <logger name="*" minlevel="Error" writeTo="errors" />

    <!-- Skip Microsoft logs below Warning -->
    <logger name="Microsoft.*" maxlevel="Info" final="true" />
  </rules>
</nlog>
```

### Поддерживаемые форматы логов

NLogMonitor поддерживает парсинг следующих layout-форматов:

```
# Стандартный
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}

# Минимальный
${longdate}|${level:uppercase=true}|${message}

# Расширенный
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}|${exception:format=tostring}
```

---

## 📊 Настройки производительности

### Рекомендации для больших файлов

```json
{
  "SessionStorage": {
    "MaxFileSizeMb": 200,
    "DefaultTtlMinutes": 30
  },
  "Parser": {
    "MaxLineLength": 50000,
    "BufferSize": 65536
  },
  "Api": {
    "DefaultPageSize": 50,
    "MaxPageSize": 500
  }
}
```

### Memory Limits

```json
{
  "MemorySettings": {
    "MaxConcurrentSessions": 10,
    "MaxEntriesPerSession": 1000000,
    "GCThresholdMb": 500
  }
}
```

---

## 🔗 Связанные документы

- [Deployment](DEPLOYMENT.md)
- [Architecture](ARCHITECTURE.md)
- [API Reference](API.md)
