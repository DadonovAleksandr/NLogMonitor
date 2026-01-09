# ⚙️ Конфигурация

## 📋 Содержание

- [Обзор](#-обзор)
- [Backend конфигурация](#-backend-конфигурация)
- [Frontend конфигурация](#-frontend-конфигурация)
- [Переменные окружения](#-переменные-окружения)
- [NLog конфигурация](#-nlog-конфигурация)

---

## 📖 Обзор

nLogMonitor использует иерархическую систему конфигурации:

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

  "SessionSettings": {
    "FallbackTtlMinutes": 5,
    "CleanupIntervalMinutes": 1
  },

  "FileSettings": {
    "MaxFileSizeMB": 100,
    "AllowedExtensions": [".log", ".txt"]
  },

  "RecentLogsSettings": {
    "MaxRecentCount": 20,
    "StorageFileName": "recent-logs.json"
  },

  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
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
  "SessionSettings": {
    "FallbackTtlMinutes": 10
  }
}
```

---

## 🎨 Frontend конфигурация

### vite.config.ts

```typescript
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import path from 'path';

export default defineConfig({
  plugins: [vue()],
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
VITE_APP_TITLE=nLogMonitor

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
| `SessionSettings__FallbackTtlMinutes` | int | 5 | Fallback TTL сессий (страховка) |
| `SessionSettings__CleanupIntervalMinutes` | int | 1 | Интервал очистки |
| `FileSettings__MaxFileSizeMB` | int | 100 | Макс. размер файла в МБ |
| `RecentLogsSettings__MaxRecentCount` | int | 20 | Макс. количество недавних файлов |
| `Cors__AllowedOrigins__0` | string | - | CORS origin |

### Пример docker-compose.yml

```yaml
services:
  app:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - SessionSettings__FallbackTtlMinutes=10
      - FileSettings__MaxFileSizeMB=200
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

nLogMonitor поддерживает парсинг следующих layout-форматов:

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
  "SessionSettings": {
    "FallbackTtlMinutes": 10
  },
  "FileSettings": {
    "MaxFileSizeMB": 200
  }
}
```

### Memory Limits

> Примечание: Память управляется через TTL сессий. Основной механизм удаления — через SignalR disconnect (см. Фаза 6 в PLAN.md). FallbackTtlMinutes используется как страховка.

---

## 🔗 Связанные документы

- [Deployment](DEPLOYMENT.md)
- [Architecture](ARCHITECTURE.md)
- [API Reference](API.md)
