# 🔌 API Reference

## 📋 Содержание

- [Обзор](#-обзор)
- [Аутентификация](#-аутентификация)
- [Endpoints](#-endpoints)
- [Модели данных](#-модели-данных)
- [Коды ошибок](#-коды-ошибок)
- [Примеры](#-примеры)

---

## 📖 Обзор

| Свойство | Значение |
|----------|----------|
| Base URL | `http://localhost:5000/api` |
| Формат | JSON |
| Версия | v1 |
| Документация | `/swagger` |

---

## 🔐 Аутентификация

> ⚠️ В текущей версии аутентификация не требуется. API работает локально.

---

## 📡 Endpoints

### Files (Desktop режим)

#### `POST /api/files/open`

Открытие лог-файла по абсолютному пути (для Desktop режима).

> ⚠️ **Только Desktop режим**: В Web-режиме (`App.Mode: Web`) этот эндпоинт возвращает HTTP 404.

**Request:**

```json
{
  "filePath": "C:\\logs\\app\\2024-01-15.log"
}
```

**Response:** `200 OK`

```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "2024-01-15.log",
  "filePath": "C:\\logs\\app\\2024-01-15.log",
  "totalEntries": 15420,
  "levelCounts": {
    "Trace": 1000,
    "Debug": 5000,
    "Info": 8000,
    "Warn": 1000,
    "Error": 400,
    "Fatal": 20
  }
}
```

**Ошибки:**

| Код | Описание |
|-----|----------|
| 400 | Путь не указан |
| 404 | Файл не найден |

---

#### `POST /api/files/open-directory`

Открытие директории с автоматическим выбором последнего по имени .log файла.

> ⚠️ **Только Desktop режим**: В Web-режиме (`App.Mode: Web`) этот эндпоинт возвращает HTTP 404.

**Request:**

```json
{
  "directoryPath": "C:\\logs\\app"
}
```

**Response:** `200 OK`

```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "2024-01-15.log",
  "filePath": "C:\\logs\\app\\2024-01-15.log",
  "totalEntries": 15420,
  "levelCounts": {
    "Trace": 1000,
    "Debug": 5000,
    "Info": 8000,
    "Warn": 1000,
    "Error": 400,
    "Fatal": 20
  }
}
```

**Ошибки:**

| Код | Описание |
|-----|----------|
| 400 | Путь не указан |
| 404 | Директория не найдена или не содержит .log файлов |

---

#### `POST /api/files/{sessionId}/stop-watching`

Остановка мониторинга изменений файла для указанной сессии.

> ⚠️ **Только Desktop режим**: В Web-режиме (`App.Mode: Web`) этот эндпоинт возвращает HTTP 404.

**Response:** `204 No Content`

**Ошибки:**

| Код | Описание |
|-----|----------|
| 404 | Сессия не найдена или эндпоинт доступен только в Desktop-режиме |

---

### Upload (Web режим)

#### `POST /api/upload`

Загрузка лог-файла и создание сессии.

**Request:**

```http
POST /api/upload HTTP/1.1
Content-Type: multipart/form-data

file: <binary>
```

**Response:** `200 OK`

```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "app.log",
  "filePath": "{tempDirectory}/{sessionId}/app.log",
  "totalEntries": 15420,
  "levelCounts": {
    "Trace": 1000,
    "Debug": 5000,
    "Info": 8000,
    "Warn": 1000,
    "Error": 400,
    "Fatal": 20
  }
}
```

> **Примечание:** `filePath` зависит от настройки `FileSettings.TempDirectory` (по умолчанию `./temp`).

**Ошибки:**

| Код | Описание |
|-----|----------|
| 400 | Файл не предоставлен, пустой, слишком большой или с недопустимым расширением |

---

### Logs

#### `GET /api/logs/{sessionId}`

Получение логов с фильтрацией и пагинацией.

**Parameters:**

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| `sessionId` | Guid | required | ID сессии |
| `page` | int | 1 | Номер страницы |
| `pageSize` | int | 50 | Записей на странице (max: 500) |
| `minLevel` | string | - | Минимальный уровень (Trace, Debug, Info, Warn, Error, Fatal) |
| `maxLevel` | string | - | Максимальный уровень (Trace, Debug, Info, Warn, Error, Fatal) |
| `search` | string | - | Поисковый запрос |
| `logger` | string | - | Фильтр по имени логгера |
| `fromDate` | DateTime | - | Начальная дата |
| `toDate` | DateTime | - | Конечная дата |

**Request:**

```http
GET /api/logs/3fa85f64-5717-4562-b3fc-2c963f66afa6?page=1&pageSize=50&minLevel=Error&search=connection HTTP/1.1
```

**Response:** `200 OK`

```json
{
  "items": [
    {
      "id": 1,
      "timestamp": "2024-01-15T10:30:45.123Z",
      "level": "Error",
      "message": "Connection failed",
      "logger": "MyApp.Database",
      "processId": 1234,
      "threadId": 2,
      "exception": "System.Net.Sockets.SocketException: Connection refused\n   at System.Net.Sockets.Socket.Connect()"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

**Ошибки:**

| Код | Описание |
|-----|----------|
| 404 | Сессия не найдена или истекла |
| 400 | Неверные параметры запроса |

---

#### `GET /api/logs/{sessionId}/stats`

Статистика по сессии.

**Response:** `200 OK`

```json
{
  "totalEntries": 15420,
  "levelCounts": {
    "Trace": 1000,
    "Debug": 5000,
    "Info": 8000,
    "Warn": 1000,
    "Error": 400,
    "Fatal": 20
  },
  "timeRange": {
    "from": "2024-01-15T00:00:00Z",
    "to": "2024-01-15T23:59:59Z"
  },
  "uniqueLoggers": ["MyApp.Program", "MyApp.Database", "MyApp.Service"]
}
```

---

### Export

#### `GET /api/export/{sessionId}`

Экспорт логов в файл.

**Parameters:**

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| `format` | string | json | Формат: `json` или `csv` |
| `minLevel` | string | - | Минимальный уровень (Trace, Debug, Info, Warn, Error, Fatal) |
| `maxLevel` | string | - | Максимальный уровень (Trace, Debug, Info, Warn, Error, Fatal) |
| `fromDate` | DateTime | - | Начальная дата (ISO 8601) |
| `toDate` | DateTime | - | Конечная дата (ISO 8601) |
| `search` | string | - | Поисковый запрос |
| `logger` | string | - | Фильтр по имени логгера |

**Request:**

```http
GET /api/export/3fa85f64-5717-4562-b3fc-2c963f66afa6?format=csv&minLevel=Error&fromDate=2024-01-15T00:00:00Z HTTP/1.1
```

**Response:** `200 OK`

```http
Content-Type: text/csv
Content-Disposition: attachment; filename="logs_{sessionId}_20240115_103045.csv"

Id,Timestamp,Level,Message,Logger,ProcessId,ThreadId,Exception
1,2024-01-15 10:30:45.1234,Error,Connection failed,MyApp.Database,1234,2,"SocketException..."
```

---

### Sessions

#### `GET /api/sessions/{sessionId}`

Информация о сессии.

**Response:** `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "app.log",
  "fileSize": 1048576,
  "totalEntries": 15420,
  "createdAt": "2024-01-15T10:30:00Z",
  "expiresAt": "2024-01-15T11:30:00Z"
}
```

#### `DELETE /api/sessions/{sessionId}`

Удаление сессии.

**Response:** `204 No Content`

---

### Recent

#### `GET /api/recent`

Получение списка недавно открытых файлов и директорий.

**Response:** `200 OK`

```json
[
  {
    "path": "C:\\logs\\app\\2024-01-15.log",
    "isDirectory": false,
    "openedAt": "2024-01-15T10:30:00Z",
    "displayName": "2024-01-15.log"
  },
  {
    "path": "C:\\logs\\app",
    "isDirectory": true,
    "openedAt": "2024-01-14T15:00:00Z",
    "displayName": "app"
  }
]
```

---

#### `DELETE /api/recent`

Очистка списка недавно открытых файлов.

**Response:** `204 No Content`

---

### Client Logs

#### `POST /api/client-logs`

Приём логов с фронтенда (batch отправка). Rate limiting: 100 запросов в минуту на IP.

**Request:**

```json
[
  {
    "level": "Error",
    "message": "Failed to load component",
    "logger": "ClientLogger",
    "timestamp": "2024-01-15T10:30:45.123Z",
    "url": "http://localhost:5173/logs",
    "userAgent": "Mozilla/5.0...",
    "userId": "user-123",
    "version": "1.0.0",
    "sessionId": "abc-123",
    "context": {
      "componentName": "LogTable",
      "action": "fetch"
    },
    "stack": "Error: Failed to load...\n    at Component.vue:42"
  }
]
```

**Поля ClientLogDto:**

| Поле | Тип | Обязательное | Описание |
|------|-----|--------------|----------|
| `level` | string | Да | Уровень лога: Trace, Debug, Info, Warn, Error, Fatal (нормализуется: warning→warn, fatal/critical→error) |
| `message` | string | Да | Сообщение лога (max 10000 символов) |
| `logger` | string | Нет | Имя логгера (max 256 символов, default: "ClientLogger") |
| `timestamp` | string | Нет | ISO 8601 timestamp (default: текущее время сервера) |
| `url` | string | Нет | URL страницы (max 2048 символов) |
| `userAgent` | string | Нет | User-Agent браузера (max 512 символов) |
| `userId` | string | Нет | ID пользователя (max 128 символов) |
| `version` | string | Нет | Версия приложения (max 64 символа) |
| `sessionId` | string | Нет | ID сессии (max 128 символов) |
| `context` | object | Нет | Произвольный контекст (JSON объект, сериализуется в строку) |
| `stack` | string | Нет | Stack trace ошибки (max 32000 символов) |

**Response:** `200 OK`

```json
{
  "processed": 5,
  "failed": 0,
  "errors": []
}
```

При частичной ошибке:

```json
{
  "processed": 3,
  "failed": 2,
  "errors": [
    "[1] Level is required",
    "[4] Message is required"
  ]
}
```

**Ошибки:**

| Код | Описание |
|-----|----------|
| 400 | Пустой массив или некорректный JSON |
| 429 | Превышен лимит запросов (100 req/min per IP) |

---

### SignalR Hub

#### WebSocket Connection

Подключение к SignalR Hub для real-time обновлений логов.

**Hub URL:** `/hubs/logwatcher`

**Библиотека:** `@microsoft/signalr` (frontend)

#### `JoinSession`

Присоединение к сессии для получения real-time обновлений.

**Request (Client → Server):**

```typescript
await connection.invoke('JoinSession', sessionId)
```

**Response:**

```json
{
  "success": true,
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "app.log"
}
```

или при ошибке:

```json
{
  "success": false,
  "error": "Session not found"
}
```

**Описание:** Привязывает SignalR connectionId к sessionId и добавляет клиента в группу сессии. После вызова клиент начинает получать событие `NewLogs`.

---

#### `LeaveSession`

Выход из сессии и остановка получения обновлений.

**Request (Client → Server):**

```typescript
await connection.invoke('LeaveSession', sessionId)
```

**Response:** `void`

**Описание:** Удаляет клиента из группы сессии, отвязывает connectionId и удаляет сессию из хранилища. После вызова клиент больше не получает обновления.

---

#### `NewLogs` (Server → Client Event)

Событие отправляется сервером при обнаружении новых записей в файле лога.

**Подписка (Client):**

```typescript
connection.on('NewLogs', (logs: LogEntry[]) => {
  console.log('Получены новые логи:', logs)
})
```

**Payload:**

```typescript
type NewLogsEvent = LogEntry[]
```

**Описание:** Отправляется всем клиентам в группе сессии. FileWatcherService обнаруживает изменения в файле (debounce 200ms) и через Hub рассылает новые записи.

---

#### Lifecycle управление

**Автоматическое удаление сессии:**
- При закрытии вкладки браузера → `OnDisconnectedAsync` → отвязывание connectionId → удаление сессии
- При явном вызове `LeaveSession` → удаление сессии

**Fallback TTL (5 минут):**
- Страховка для случаев потери соединения (crash браузера, потеря сети)
- Фоновый cleanup timer удаляет сессии, у которых истёк TTL

**Пример использования:**

```typescript
import * as signalR from '@microsoft/signalr'

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/logwatcher')
  .withAutomaticReconnect()
  .build()

await connection.start()

// Присоединиться к сессии
const result = await connection.invoke('JoinSession', sessionId)
if (result.success) {
  console.log(`Joined session for file: ${result.fileName}`)
}

// Подписаться на новые логи
connection.on('NewLogs', (logs) => {
  logs.forEach(log => console.log(log.message))
})

// При закрытии - покинуть сессию
await connection.invoke('LeaveSession', sessionId)
await connection.stop()
```

---

## 📦 Модели данных

### LogEntry

```typescript
interface LogEntry {
  id: number;                 // long in C#
  timestamp: string;          // ISO 8601
  level: LogLevel;
  message: string;
  logger: string;
  processId: number;
  threadId: number;
  exception?: string;         // optional
}

type LogLevel = 'Trace' | 'Debug' | 'Info' | 'Warn' | 'Error' | 'Fatal';
```

### PagedResult

```typescript
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

### OpenFileResultDto

```typescript
interface OpenFileResultDto {
  sessionId: string;
  fileName: string;
  filePath: string;
  totalEntries: number;
  levelCounts: Record<LogLevel, number>;
}
```

### SessionStats

```typescript
interface SessionStats {
  totalEntries: number;
  levelCounts: Record<LogLevel, number>;
  timeRange: {
    from: string;
    to: string;
  };
  uniqueLoggers: string[];
}
```

---

## ❌ Коды ошибок

| HTTP Code | Error Type | Описание |
|-----------|------------|----------|
| 400 | `BadRequest` | Неверные параметры запроса, пустой файл, недопустимый формат |
| 404 | `NotFound` | Сессия не найдена, файл/директория не существует, эндпоинт доступен только в Desktop-режиме |
| 500 | `InternalServerError` | Внутренняя ошибка сервера |
| 501 | `NotImplemented` | Функционал ещё не реализован |

**Формат ошибки (ApiErrorResponse):**

```json
{
  "error": "NotFound",
  "message": "Session with ID '3fa85f64...' not found",
  "details": null,
  "traceId": "00-abc123..."
}
```

> **Примечание:** Поле `details` заполняется только в Development-окружении (stack trace, inner exception).

---

## 💡 Примеры

### cURL

```bash
# Загрузка файла
curl -X POST http://localhost:5000/api/upload \
  -F "file=@/path/to/app.log"

# Получение логов
curl "http://localhost:5000/api/logs/SESSION_ID?page=1&pageSize=50&minLevel=Error"

# Экспорт в CSV
curl -o logs.csv "http://localhost:5000/api/export/SESSION_ID?format=csv"
```

### JavaScript (fetch)

```javascript
// Загрузка файла
const formData = new FormData();
formData.append('file', file);

const response = await fetch('/api/upload', {
  method: 'POST',
  body: formData,
});
const { sessionId } = await response.json();

// Получение логов
const logsResponse = await fetch(
  `/api/logs/${sessionId}?page=1&pageSize=50&minLevel=Error`
);
const { items, totalCount } = await logsResponse.json();
```

### TypeScript (axios)

```typescript
import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
});

// Загрузка
const upload = async (file: File) => {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await api.post<UploadResponse>('/upload', formData);
  return data;
};

// Получение логов
const getLogs = async (
  sessionId: string,
  params: { page: number; pageSize: number; minLevel?: LogLevel; maxLevel?: LogLevel }
) => {
  const { data } = await api.get<PagedResult<LogEntry>>(
    `/logs/${sessionId}`,
    { params }
  );
  return data;
};
```

---

## 🔗 Связанные документы

- [Architecture](ARCHITECTURE.md)
- [Configuration](CONFIGURATION.md)
