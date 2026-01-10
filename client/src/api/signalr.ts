import * as signalR from '@microsoft/signalr'
import { BASE_URL } from './client'
import type { JoinSessionResult, NewLogsEvent, ConnectionState } from '@/types'

/**
 * Тип callback-функции для обработки новых логов
 */
export type NewLogsCallback = (logs: NewLogsEvent) => void

/**
 * Тип callback-функции для обработки изменения состояния подключения
 */
export type ConnectionStateCallback = (state: ConnectionState) => void

/**
 * Singleton менеджер SignalR подключения.
 * Управляет жизненным циклом WebSocket соединения с backend LogWatcherHub.
 * Поддерживает автоматическое переподключение с exponential backoff.
 */
class SignalRManager {
  private connection: signalR.HubConnection | null = null
  private connectionPromise: Promise<void> | null = null
  private currentSessionId: string | null = null
  private newLogsCallbacks: Set<NewLogsCallback> = new Set()
  private connectionStateCallbacks: Set<ConnectionStateCallback> = new Set()

  /**
   * Создаёт и настраивает SignalR подключение с автореконнектом.
   * Использует exponential backoff: 0, 2, 10, 30 секунд.
   */
  private createConnection(): signalR.HubConnection {
    const hubUrl = `${BASE_URL}/hubs/logwatcher`

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, затем постоянно 30s
          if (retryContext.previousRetryCount === 0) {
            return 0 // первая попытка сразу
          } else if (retryContext.previousRetryCount === 1) {
            return 2000 // 2 секунды
          } else if (retryContext.previousRetryCount === 2) {
            return 10000 // 10 секунд
          } else {
            return 30000 // 30 секунд для всех последующих попыток
          }
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build()

    // Подписка на событие NewLogs от сервера
    connection.on('NewLogs', (logs: NewLogsEvent) => {
      this.notifyNewLogs(logs)
    })

    // Отслеживание состояния подключения
    connection.onclose((error) => {
      console.warn('SignalR connection closed', error)
      this.notifyConnectionState('Disconnected')
    })

    connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting...', error)
      this.notifyConnectionState('Reconnecting')
    })

    connection.onreconnected((connectionId) => {
      console.info('SignalR reconnected', connectionId)
      this.notifyConnectionState('Connected')

      // При переподключении нужно заново присоединиться к сессии
      if (this.currentSessionId) {
        this.joinSession(this.currentSessionId).catch((err) => {
          console.error('Failed to rejoin session after reconnect', err)
        })
      }
    })

    return connection
  }

  /**
   * Устанавливает подключение к SignalR Hub.
   * Singleton - повторные вызовы вернут существующее подключение.
   * @returns Promise, который завершается после установки подключения
   */
  public async connect(): Promise<void> {
    // Если уже подключены или подключаемся, возвращаем существующий promise
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve()
    }

    if (this.connectionPromise) {
      return this.connectionPromise
    }

    // Создаём подключение, если его нет
    if (!this.connection) {
      this.connection = this.createConnection()
    }

    // Запускаем подключение
    this.notifyConnectionState('Connecting')
    this.connectionPromise = this.connection
      .start()
      .then(() => {
        console.info('SignalR connected')
        this.notifyConnectionState('Connected')
        this.connectionPromise = null
      })
      .catch((err) => {
        console.error('SignalR connection failed', err)
        this.notifyConnectionState('Disconnected')
        this.connectionPromise = null
        throw err
      })

    return this.connectionPromise
  }

  /**
   * Закрывает подключение к SignalR Hub.
   * Отправляет LeaveSession перед закрытием, если есть активная сессия.
   */
  public async disconnect(): Promise<void> {
    if (!this.connection) {
      return
    }

    this.notifyConnectionState('Disconnecting')

    try {
      // Если есть активная сессия, сначала покидаем её
      if (this.currentSessionId) {
        await this.leaveSession(this.currentSessionId)
      }

      await this.connection.stop()
      console.info('SignalR disconnected')
    } catch (err) {
      console.error('Error during disconnect', err)
    } finally {
      this.connection = null
      this.connectionPromise = null
      this.currentSessionId = null
      this.notifyConnectionState('Disconnected')
    }
  }

  /**
   * Присоединяется к группе сессии логов.
   * После успешного вызова клиент начнёт получать события NewLogs для этой сессии.
   * @param sessionId - ID сессии логов
   * @returns Результат операции JoinSession
   */
  public async joinSession(sessionId: string): Promise<JoinSessionResult> {
    await this.connect()

    if (!this.connection) {
      throw new Error('SignalR connection not established')
    }

    try {
      const result = await this.connection.invoke<JoinSessionResult>('JoinSession', sessionId)

      if (result.success) {
        this.currentSessionId = sessionId
        console.info(`Joined session ${sessionId}`)
      } else {
        console.error(`Failed to join session: ${result.error}`)
      }

      return result
    } catch (err) {
      console.error('Error joining session', err)
      throw err
    }
  }

  /**
   * Покидает группу сессии логов.
   * После вызова клиент перестаёт получать события NewLogs для этой сессии.
   * Сессия будет удалена из хранилища на сервере.
   * @param sessionId - ID сессии логов
   */
  public async leaveSession(sessionId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return
    }

    try {
      await this.connection.invoke('LeaveSession', sessionId)
      console.info(`Left session ${sessionId}`)

      if (this.currentSessionId === sessionId) {
        this.currentSessionId = null
      }
    } catch (err) {
      console.error('Error leaving session', err)
      throw err
    }
  }

  /**
   * Подписывается на событие NewLogs (новые записи логов).
   * @param callback - Функция, которая будет вызвана при получении новых логов
   * @returns Функция для отписки от события
   */
  public onNewLogs(callback: NewLogsCallback): () => void {
    this.newLogsCallbacks.add(callback)

    // Возвращаем функцию для отписки
    return () => {
      this.newLogsCallbacks.delete(callback)
    }
  }

  /**
   * Подписывается на изменение состояния подключения.
   * @param callback - Функция, которая будет вызвана при изменении состояния
   * @returns Функция для отписки от события
   */
  public onConnectionStateChanged(callback: ConnectionStateCallback): () => void {
    this.connectionStateCallbacks.add(callback)

    // Возвращаем функцию для отписки
    return () => {
      this.connectionStateCallbacks.delete(callback)
    }
  }

  /**
   * Получает текущее состояние подключения.
   */
  public getConnectionState(): ConnectionState {
    if (!this.connection) {
      return 'Disconnected'
    }

    switch (this.connection.state) {
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected'
      case signalR.HubConnectionState.Connecting:
        return 'Connecting'
      case signalR.HubConnectionState.Connected:
        return 'Connected'
      case signalR.HubConnectionState.Disconnecting:
        return 'Disconnecting'
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting'
      default:
        return 'Disconnected'
    }
  }

  /**
   * Получает ID текущей активной сессии.
   */
  public getCurrentSessionId(): string | null {
    return this.currentSessionId
  }

  /**
   * Проверяет, установлено ли подключение.
   */
  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected
  }

  /**
   * Уведомляет всех подписчиков о новых логах.
   */
  private notifyNewLogs(logs: NewLogsEvent): void {
    this.newLogsCallbacks.forEach((callback) => {
      try {
        callback(logs)
      } catch (err) {
        console.error('Error in NewLogs callback', err)
      }
    })
  }

  /**
   * Уведомляет всех подписчиков об изменении состояния подключения.
   */
  private notifyConnectionState(state: ConnectionState): void {
    this.connectionStateCallbacks.forEach((callback) => {
      try {
        callback(state)
      } catch (err) {
        console.error('Error in ConnectionState callback', err)
      }
    })
  }
}

// Экспортируем singleton instance
export const signalRManager = new SignalRManager()
