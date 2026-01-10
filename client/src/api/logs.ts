import apiClient from './client'
import type { PagedResult, LogEntry, FilterOptions } from '@/types'

export const logsApi = {
  /**
   * Получить логи сессии с фильтрацией и пагинацией
   */
  async getLogs(
    sessionId: string,
    filters?: FilterOptions
  ): Promise<PagedResult<LogEntry>> {
    const params = new URLSearchParams()
    if (filters?.searchText) params.set('searchText', filters.searchText)

    // Обработка levels[] (массив конкретных уровней)
    // ВАЖНО: отправляем даже пустой массив (режим NONE)
    if (filters?.levels !== undefined) {
      if (filters.levels.length === 0) {
        // Пустой массив: добавляем пустое значение, чтобы backend понял, что это фильтр по пустому массиву
        params.append('levels', '')
      } else {
        filters.levels.forEach(level => params.append('levels', level))
      }
    }

    // Fallback на minLevel/maxLevel (устарело, но оставлено для совместимости)
    if (filters?.minLevel !== undefined)
      params.set('minLevel', String(filters.minLevel))
    if (filters?.maxLevel !== undefined)
      params.set('maxLevel', String(filters.maxLevel))

    if (filters?.fromDate) params.set('fromDate', filters.fromDate)
    if (filters?.toDate) params.set('toDate', filters.toDate)
    if (filters?.logger) params.set('logger', filters.logger)
    if (filters?.page) params.set('page', String(filters.page))
    if (filters?.pageSize) params.set('pageSize', String(filters.pageSize))

    const response = await apiClient.get<PagedResult<LogEntry>>(
      `/api/logs/${sessionId}?${params}`
    )
    return response.data
  }
}
