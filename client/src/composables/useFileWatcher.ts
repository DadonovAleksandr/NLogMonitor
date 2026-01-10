import { ref, onUnmounted, type Ref } from 'vue'
import { signalRManager } from '@/api/signalr'
import type { NewLogsEvent, ConnectionState } from '@/types'

/**
 * Callback-функция для обработки новых логов
 */
export type OnNewLogsCallback = (logs: NewLogsEvent) => void

/**
 * Результат работы composable useFileWatcher
 */
export interface UseFileWatcherReturn {
  /** Состояние подключения SignalR */
  connectionState: Ref<ConnectionState>
  /** Флаг установленного подключения */
  isConnected: Ref<boolean>
  /** Флаг активного отслеживания файла */
  isWatching: Ref<boolean>
  /** Текущий ID сессии, которую отслеживаем */
  currentSessionId: Ref<string | null>
  /** Начать отслеживание файла по sessionId */
  startWatching: (sessionId: string, onNewLogs?: OnNewLogsCallback) => Promise<void>
  /** Остановить отслеживание текущего файла */
  stopWatching: () => Promise<void>
}

/**
 * Vue composable для работы с SignalR file watcher.
 * Управляет жизненным циклом подключения и подписки на обновления логов.
 * Автоматически отписывается при размонтировании компонента.
 *
 * @example
 * ```ts
 * const { startWatching, stopWatching, isConnected, isWatching } = useFileWatcher()
 *
 * // Начать отслеживание
 * await startWatching(sessionId, (newLogs) => {
 *   console.log('Получены новые логи:', newLogs)
 * })
 *
 * // Остановить отслеживание
 * await stopWatching()
 * ```
 */
export function useFileWatcher(): UseFileWatcherReturn {
  const connectionState = ref<ConnectionState>(signalRManager.getConnectionState())
  const isConnected = ref<boolean>(signalRManager.isConnected())
  const isWatching = ref<boolean>(false)
  const currentSessionId = ref<string | null>(null)

  // Callback-функции для отписки
  let unsubscribeConnectionState: (() => void) | null = null
  let unsubscribeNewLogs: (() => void) | null = null

  // Подписываемся на изменение состояния подключения
  unsubscribeConnectionState = signalRManager.onConnectionStateChanged((state) => {
    connectionState.value = state
    isConnected.value = state === 'Connected'
  })

  /**
   * Начинает отслеживание файла логов.
   * Устанавливает подключение SignalR, присоединяется к сессии и подписывается на события NewLogs.
   *
   * @param sessionId - ID сессии логов для отслеживания
   * @param onNewLogs - Callback-функция для обработки новых логов (опционально)
   * @throws Error если не удалось присоединиться к сессии
   */
  const startWatching = async (
    sessionId: string,
    onNewLogs?: OnNewLogsCallback
  ): Promise<void> => {
    try {
      // Если уже отслеживаем эту же сессию, ничего не делаем
      if (isWatching.value && currentSessionId.value === sessionId) {
        console.info(`Already watching session ${sessionId}`)
        return
      }

      // Если отслеживаем другую сессию, сначала останавливаем
      if (isWatching.value && currentSessionId.value) {
        await stopWatching()
      }

      // Присоединяемся к сессии
      const result = await signalRManager.joinSession(sessionId)

      if (!result.success) {
        throw new Error(result.error || 'Failed to join session')
      }

      // Подписываемся на новые логи
      if (onNewLogs) {
        unsubscribeNewLogs = signalRManager.onNewLogs(onNewLogs)
      }

      currentSessionId.value = sessionId
      isWatching.value = true

      console.info(`Started watching session ${sessionId} (file: ${result.fileName})`)
    } catch (err) {
      console.error('Failed to start watching', err)
      isWatching.value = false
      currentSessionId.value = null
      throw err
    }
  }

  /**
   * Останавливает отслеживание текущего файла логов.
   * Покидает группу сессии и отписывается от событий NewLogs.
   */
  const stopWatching = async (): Promise<void> => {
    if (!isWatching.value || !currentSessionId.value) {
      return
    }

    try {
      // Отписываемся от событий NewLogs
      if (unsubscribeNewLogs) {
        unsubscribeNewLogs()
        unsubscribeNewLogs = null
      }

      // Покидаем сессию
      await signalRManager.leaveSession(currentSessionId.value)

      console.info(`Stopped watching session ${currentSessionId.value}`)
    } catch (err) {
      console.error('Failed to stop watching', err)
      throw err
    } finally {
      isWatching.value = false
      currentSessionId.value = null
    }
  }

  /**
   * Автоматическая очистка при размонтировании компонента.
   * Останавливает отслеживание и отписывается от событий.
   */
  onUnmounted(async () => {
    // Останавливаем отслеживание
    if (isWatching.value) {
      await stopWatching()
    }

    // Отписываемся от событий изменения состояния
    if (unsubscribeConnectionState) {
      unsubscribeConnectionState()
      unsubscribeConnectionState = null
    }
  })

  return {
    connectionState,
    isConnected,
    isWatching,
    currentSessionId,
    startWatching,
    stopWatching
  }
}
