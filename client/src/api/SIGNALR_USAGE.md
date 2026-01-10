# SignalR Client Usage

## Обзор

Frontend SignalR клиент для real-time обновлений логов. Состоит из двух компонентов:

1. **signalr.ts** - Singleton менеджер подключения с автореконнектом
2. **useFileWatcher.ts** - Vue composable для удобной работы в компонентах

## Быстрый старт

### Вариант 1: Использование composable (рекомендуется)

```vue
<script setup lang="ts">
import { useFileWatcher } from '@/composables'
import { onMounted } from 'vue'

const props = defineProps<{ sessionId: string }>()

const { isConnected, isWatching, startWatching, stopWatching } = useFileWatcher()

// Обработчик новых логов
const handleNewLogs = (logs: LogEntry[]) => {
  console.log('Получено новых записей:', logs.length)
  // Добавить логи в таблицу, обновить UI и т.д.
}

onMounted(async () => {
  await startWatching(props.sessionId, handleNewLogs)
})

// Composable автоматически отпишется при размонтировании компонента
</script>

<template>
  <div>
    <p>Подключение: {{ isConnected ? 'Активно' : 'Отключено' }}</p>
    <p>Отслеживание: {{ isWatching ? 'Активно' : 'Неактивно' }}</p>
  </div>
</template>
```

### Вариант 2: Прямое использование signalRManager

```typescript
import { signalRManager } from '@/api'

// Подключение
await signalRManager.connect()

// Присоединиться к сессии
const result = await signalRManager.joinSession(sessionId)
if (result.success) {
  console.log(`Joined session for file: ${result.fileName}`)
}

// Подписка на новые логи
const unsubscribe = signalRManager.onNewLogs((logs) => {
  console.log('New logs received:', logs)
})

// Покинуть сессию (важно вызвать при закрытии!)
await signalRManager.leaveSession(sessionId)

// Отписаться от событий
unsubscribe()

// Отключение
await signalRManager.disconnect()
```

## API

### SignalRManager

**Методы:**
- `connect(): Promise<void>` - Установить подключение
- `disconnect(): Promise<void>` - Закрыть подключение
- `joinSession(sessionId): Promise<JoinSessionResult>` - Присоединиться к сессии
- `leaveSession(sessionId): Promise<void>` - Покинуть сессию
- `onNewLogs(callback): () => void` - Подписаться на новые логи (возвращает функцию отписки)
- `onConnectionStateChanged(callback): () => void` - Подписаться на изменение состояния
- `getConnectionState(): ConnectionState` - Получить текущее состояние
- `getCurrentSessionId(): string | null` - Получить ID активной сессии
- `isConnected(): boolean` - Проверить подключение

### useFileWatcher Composable

**Возвращаемые значения:**
- `connectionState: Ref<ConnectionState>` - Реактивное состояние подключения
- `isConnected: Ref<boolean>` - Флаг подключения
- `isWatching: Ref<boolean>` - Флаг активного отслеживания
- `currentSessionId: Ref<string | null>` - ID текущей сессии
- `startWatching(sessionId, callback?): Promise<void>` - Начать отслеживание
- `stopWatching(): Promise<void>` - Остановить отслеживание

## Особенности

### Автоматическое переподключение

SignalR клиент автоматически переподключается при потере соединения с exponential backoff:
- 1-я попытка: сразу (0ms)
- 2-я попытка: через 2 секунды (2000ms)
- 3-я попытка: через 10 секунд (10000ms)
- 4-я и последующие: через 30 секунд (30000ms)

При переподключении клиент автоматически заново присоединяется к активной сессии через вызов `joinSession(currentSessionId)`.

### Singleton паттерн

`signalRManager` - это singleton. Повторные вызовы `connect()` вернут существующее подключение.

### Управление жизненным циклом

При использовании `useFileWatcher` composable:
- Автоматическая отписка от событий при `onUnmounted`
- Автоматическое покидание сессии при размонтировании компонента

### Состояния подключения

- `Disconnected` - Отключено
- `Connecting` - Подключение...
- `Connected` - Подключено
- `Disconnecting` - Отключение...
- `Reconnecting` - Переподключение...

## События от сервера

### NewLogs

Отправляется сервером при обнаружении новых записей в файле лога.

**Формат:**
```typescript
type NewLogsEvent = LogEntry[]

interface LogEntry {
  id: number
  timestamp: string
  level: string
  message: string
  logger: string
  processId: number
  threadId: number
  exception?: string
}
```

## Примеры интеграции

### С Pinia Store

```typescript
// stores/logStore.ts
import { signalRManager } from '@/api'

export const useLogStore = defineStore('log', () => {
  const logs = ref<LogEntry[]>([])

  const initWatcher = async (sessionId: string) => {
    await signalRManager.connect()
    const result = await signalRManager.joinSession(sessionId)

    if (result.success) {
      signalRManager.onNewLogs((newLogs) => {
        logs.value.push(...newLogs)
      })
    }
  }

  return { logs, initWatcher }
})
```

### Обработка ошибок

```typescript
const { startWatching } = useFileWatcher()

try {
  await startWatching(sessionId, handleNewLogs)
} catch (err) {
  console.error('Failed to start watching:', err)
  // Показать toast с ошибкой
  showToast('Не удалось подключиться к файлу', 'error')
}
```
