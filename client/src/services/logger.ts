import { clientLogsApi, type ClientLog } from '@/api/client-logs'

/**
 * Уровни логирования
 */
export type LogLevel = 'Trace' | 'Debug' | 'Info' | 'Warn' | 'Error' | 'Fatal'

/**
 * Контекст лога - произвольные метаданные
 */
export type LogContext = Record<string, unknown>

/**
 * Глобальный контекст, добавляемый ко всем логам
 */
export interface GlobalContext {
  userId?: string
  version?: string
  sessionId?: string
}

/**
 * Конфигурация логгера
 */
export interface LoggerConfig {
  /** Размер буфера для отправки (по умолчанию 10) */
  batchSize: number
  /** Интервал автоматического flush в миллисекундах (по умолчанию 5000) */
  flushIntervalMs: number
  /** Максимальное количество повторных попыток (по умолчанию 3) */
  maxRetries: number
  /** Базовая задержка для exponential backoff в миллисекундах (по умолчанию 1000) */
  baseRetryDelayMs: number
  /** Имя логгера по умолчанию */
  defaultLogger: string
  /** Включить вывод в console (для отладки) */
  consoleOutput: boolean
}

/**
 * Дефолтная конфигурация
 */
const DEFAULT_CONFIG: LoggerConfig = {
  batchSize: 10,
  flushIntervalMs: 5000,
  maxRetries: 3,
  baseRetryDelayMs: 1000,
  defaultLogger: 'ClientLogger',
  consoleOutput: import.meta.env.DEV
}

/**
 * ClientLogger - сервис для отправки клиентских логов на сервер
 *
 * Функционал:
 * - Буферизация логов (batchSize: 10)
 * - Автоматический flush по таймеру (5 сек)
 * - Retry с exponential backoff (maxRetries: 3)
 * - Глобальный контекст (userId, version, sessionId)
 * - Автоматическое добавление url и userAgent
 * - Перехват глобальных ошибок (window.onerror, onunhandledrejection)
 */
class ClientLogger {
  private buffer: ClientLog[] = []
  private flushTimer: ReturnType<typeof setTimeout> | null = null
  private globalContext: GlobalContext = {}
  private config: LoggerConfig
  private isFlushing = false

  constructor(config: Partial<LoggerConfig> = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config }
  }

  /**
   * Установить глобальный контекст, добавляемый ко всем логам
   */
  setGlobalContext(context: GlobalContext): void {
    this.globalContext = { ...this.globalContext, ...context }
  }

  /**
   * Получить текущий глобальный контекст
   */
  getGlobalContext(): GlobalContext {
    return { ...this.globalContext }
  }

  /**
   * Очистить глобальный контекст
   */
  clearGlobalContext(): void {
    this.globalContext = {}
  }

  /**
   * Логирование на уровне Trace
   */
  trace(message: string, context?: LogContext): void {
    this.log('Trace', message, context)
  }

  /**
   * Логирование на уровне Debug
   */
  debug(message: string, context?: LogContext): void {
    this.log('Debug', message, context)
  }

  /**
   * Логирование на уровне Info
   */
  info(message: string, context?: LogContext): void {
    this.log('Info', message, context)
  }

  /**
   * Логирование на уровне Warn
   */
  warn(message: string, context?: LogContext): void {
    this.log('Warn', message, context)
  }

  /**
   * Логирование на уровне Error
   */
  error(message: string, context?: LogContext): void {
    this.log('Error', message, context)
  }

  /**
   * Логирование на уровне Fatal
   */
  fatal(message: string, context?: LogContext): void {
    this.log('Fatal', message, context)
  }

  /**
   * Логирование исключения с stack trace
   */
  exception(error: Error, context?: LogContext): void {
    const errorContext: LogContext = {
      ...context,
      errorName: error.name,
      errorMessage: error.message,
      stack: error.stack
    }
    this.log('Error', `Exception: ${error.message}`, errorContext)
  }

  /**
   * Основной метод логирования
   */
  private log(level: LogLevel, message: string, context?: LogContext): void {
    const logEntry = this.createLogEntry(level, message, context)

    // Вывод в console при необходимости
    if (this.config.consoleOutput) {
      this.logToConsole(level, message, context)
    }

    this.buffer.push(logEntry)
    this.scheduleFlush()

    // Немедленный flush при достижении batchSize
    if (this.buffer.length >= this.config.batchSize) {
      this.flush()
    }
  }

  /**
   * Создание записи лога с метаданными
   */
  private createLogEntry(level: LogLevel, message: string, context?: LogContext): ClientLog {
    return {
      level,
      message,
      logger: this.config.defaultLogger,
      url: typeof window !== 'undefined' ? window.location.href : undefined,
      userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : undefined,
      userId: this.globalContext.userId,
      version: this.globalContext.version,
      sessionId: this.globalContext.sessionId,
      context: context ? { ...context } : undefined,
      timestamp: new Date().toISOString()
    }
  }

  /**
   * Вывод в console для отладки
   */
  private logToConsole(level: LogLevel, message: string, context?: LogContext): void {
    const prefix = `[${level.toUpperCase()}]`
    const args = context ? [prefix, message, context] : [prefix, message]

    switch (level) {
      case 'Trace':
      case 'Debug':
        console.debug(...args)
        break
      case 'Info':
        console.info(...args)
        break
      case 'Warn':
        console.warn(...args)
        break
      case 'Error':
      case 'Fatal':
        console.error(...args)
        break
    }
  }

  /**
   * Планирование автоматического flush
   */
  private scheduleFlush(): void {
    if (this.flushTimer !== null) return

    this.flushTimer = setTimeout(() => {
      this.flush()
    }, this.config.flushIntervalMs)
  }

  /**
   * Отправка буфера на сервер
   */
  async flush(): Promise<void> {
    // Предотвращаем параллельные flush операции
    if (this.isFlushing || this.buffer.length === 0) return

    this.isFlushing = true
    this.clearFlushTimer()

    const logsToSend = [...this.buffer]
    this.buffer = []

    try {
      await this.sendWithRetry(logsToSend)
    } catch (error) {
      // После всех неудачных попыток - записать в console.error и очистить
      // Используем console.error здесь намеренно, т.к. logger сам не может отправить
      console.error('[ClientLogger] Failed to send logs after all retries:', error, {
        logsCount: logsToSend.length
      })
    } finally {
      this.isFlushing = false
      // Если за время отправки накопились новые логи - планируем следующий flush
      if (this.buffer.length > 0) {
        this.scheduleFlush()
      }
    }
  }

  /**
   * Отправка с exponential backoff retry
   */
  private async sendWithRetry(logs: ClientLog[]): Promise<void> {
    let lastError: unknown

    for (let attempt = 0; attempt < this.config.maxRetries; attempt++) {
      try {
        await clientLogsApi.sendLogs(logs)
        return // Успешная отправка
      } catch (error) {
        lastError = error

        // Если это не последняя попытка - ждем с exponential backoff
        if (attempt < this.config.maxRetries - 1) {
          const delay = this.config.baseRetryDelayMs * Math.pow(2, attempt)
          await this.sleep(delay)
        }
      }
    }

    throw lastError
  }

  /**
   * Утилита для задержки
   */
  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms))
  }

  /**
   * Очистка таймера flush
   */
  private clearFlushTimer(): void {
    if (this.flushTimer !== null) {
      clearTimeout(this.flushTimer)
      this.flushTimer = null
    }
  }

  /**
   * Инициализация глобальных error handlers
   */
  initErrorHandlers(): void {
    if (typeof window === 'undefined') return

    // Перехват глобальных ошибок JavaScript
    window.onerror = (
      message: string | Event,
      source?: string,
      lineno?: number,
      colno?: number,
      error?: Error
    ) => {
      const errorMessage = typeof message === 'string' ? message : 'Unknown error'

      this.error('Uncaught error', {
        message: errorMessage,
        source,
        lineno,
        colno,
        stack: error?.stack
      })

      // Возвращаем false, чтобы ошибка также попала в стандартный обработчик
      return false
    }

    // Перехват необработанных Promise rejection
    window.onunhandledrejection = (event: PromiseRejectionEvent) => {
      const reason = event.reason

      this.error('Unhandled Promise rejection', {
        reason: reason?.message || String(reason),
        stack: reason?.stack
      })
    }
  }

  /**
   * Создание Vue error handler для app.config.errorHandler
   */
  createVueErrorHandler(): (err: unknown, instance: unknown, info: string) => void {
    return (err: unknown, _instance: unknown, info: string) => {
      const error = err instanceof Error ? err : new Error(String(err))

      this.exception(error, {
        vueInfo: info,
        componentName: _instance && typeof _instance === 'object' && '$options' in _instance
          ? (_instance as { $options?: { name?: string } }).$options?.name
          : undefined
      })
    }
  }

  /**
   * Принудительная отправка всех логов (например, при закрытии страницы)
   */
  async forceFlush(): Promise<void> {
    this.clearFlushTimer()
    if (this.buffer.length > 0) {
      const logsToSend = [...this.buffer]
      this.buffer = []

      try {
        // Используем sendBeacon для гарантированной отправки при закрытии
        if (typeof navigator !== 'undefined' && navigator.sendBeacon) {
          const blob = new Blob([JSON.stringify(logsToSend)], { type: 'application/json' })
          const url = `${import.meta.env.VITE_API_URL || 'http://localhost:5000'}/api/client-logs`
          navigator.sendBeacon(url, blob)
        } else {
          await clientLogsApi.sendLogs(logsToSend)
        }
      } catch (error) {
        console.error('[ClientLogger] Failed to force flush logs:', error)
      }
    }
  }

  /**
   * Получить текущий размер буфера
   */
  getBufferSize(): number {
    return this.buffer.length
  }

  /**
   * Изменить конфигурацию
   */
  configure(config: Partial<LoggerConfig>): void {
    this.config = { ...this.config, ...config }
  }
}

// Singleton instance
export const logger = new ClientLogger()

// Экспорт класса для создания кастомных экземпляров
export { ClientLogger }
