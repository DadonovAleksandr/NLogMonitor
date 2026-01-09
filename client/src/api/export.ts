import type { FilterOptions, ExportFormat } from '@/types'

export const exportApi = {
  /**
   * Получить URL для экспорта логов
   */
  getExportUrl(
    sessionId: string,
    format: ExportFormat,
    filters?: FilterOptions
  ): string {
    const params = new URLSearchParams()
    params.set('format', format)
    if (filters?.searchText) params.set('search', filters.searchText)
    if (filters?.minLevel !== undefined)
      params.set('minLevel', String(filters.minLevel))
    if (filters?.maxLevel !== undefined)
      params.set('maxLevel', String(filters.maxLevel))
    if (filters?.fromDate) params.set('fromDate', filters.fromDate)
    if (filters?.toDate) params.set('toDate', filters.toDate)
    if (filters?.logger) params.set('logger', filters.logger)

    return `/api/export/${sessionId}?${params}`
  },

  /**
   * Скачать экспорт логов (через browser download)
   */
  downloadExport(
    sessionId: string,
    format: ExportFormat,
    filters?: FilterOptions
  ): void {
    const url = exportApi.getExportUrl(sessionId, format, filters)
    const link = document.createElement('a')
    link.href = url
    link.download = `logs-${sessionId}.${format}`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
  }
}
