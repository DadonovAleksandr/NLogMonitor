import apiClient from './client'

/**
 * Интерфейс клиентского лога для отправки на сервер
 */
export interface ClientLog {
  level: string
  message: string
  logger?: string
  url?: string
  userAgent?: string
  userId?: string
  version?: string
  sessionId?: string
  context?: Record<string, unknown>
  timestamp: string // ISO format
}

/**
 * API для отправки клиентских логов
 */
export const clientLogsApi = {
  /**
   * Отправить пакет клиентских логов на сервер
   * @param logs - массив логов для отправки
   */
  async sendLogs(logs: ClientLog[]): Promise<void> {
    await apiClient.post('/api/client-logs', logs)
  }
}
