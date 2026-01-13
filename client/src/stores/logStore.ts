import { defineStore } from 'pinia'
import type { LogEntry, OpenFileResult, FilterOptions } from '@/types'
import { LogLevel } from '@/types'
import { logsApi, filesApi } from '@/api'
import { useToast } from '@/composables/useToast'
import { useTabsStore } from './tabsStore'

/**
 * logStore — сервис для работы с логами.
 *
 * НЕ хранит состояние. Все данные хранятся в tabsStore.
 * Предоставляет методы для загрузки файлов, получения логов,
 * управления пагинацией и добавления новых логов.
 */
export const useLogStore = defineStore('logs', () => {
  const { showToast } = useToast()

  /**
   * Загрузить файл через Web API (upload)
   */
  async function uploadFile(file: File): Promise<OpenFileResult | null> {
    const tabsStore = useTabsStore()
    tabsStore.setLoading(true)
    tabsStore.setError(null)

    try {
      const result = await filesApi.uploadFile(file)
      return result
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to upload file'
      tabsStore.setError(message)
      showToast(message, 'error')
      throw err
    } finally {
      tabsStore.setLoading(false)
    }
  }

  /**
   * Открыть файл по пути (Desktop режим)
   */
  async function openFile(path: string): Promise<OpenFileResult | null> {
    const tabsStore = useTabsStore()
    tabsStore.setLoading(true)
    tabsStore.setError(null)

    try {
      const result = await filesApi.openFile(path)
      return result
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to open file'
      tabsStore.setError(message)
      showToast(message, 'error')
      throw err
    } finally {
      tabsStore.setLoading(false)
    }
  }

  /**
   * Открыть директорию (Desktop режим)
   */
  async function openDirectory(path: string): Promise<OpenFileResult | null> {
    const tabsStore = useTabsStore()
    tabsStore.setLoading(true)
    tabsStore.setError(null)

    try {
      const result = await filesApi.openDirectory(path)
      return result
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to open directory'
      tabsStore.setError(message)
      showToast(message, 'error')
      throw err
    } finally {
      tabsStore.setLoading(false)
    }
  }

  /**
   * Загрузить логи для активной вкладки
   */
  async function fetchLogs(filters?: FilterOptions) {
    const tabsStore = useTabsStore()
    const activeTab = tabsStore.activeTab
    if (!activeTab?.sessionId) return

    tabsStore.setLoading(true)
    tabsStore.setError(null)

    try {
      const result = await logsApi.getLogs(activeTab.sessionId, {
        ...filters,
        page: activeTab.page,
        pageSize: activeTab.pageSize
      })

      tabsStore.updateTab(activeTab.id, {
        logs: result.items,
        totalCount: result.totalCount,
        totalPages: result.totalPages
      })
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to fetch logs'
      tabsStore.setError(message)
      showToast(message, 'error')
      throw err
    } finally {
      tabsStore.setLoading(false)
    }
  }

  /**
   * Изменить страницу и загрузить логи
   */
  async function setPage(newPage: number) {
    const tabsStore = useTabsStore()
    const activeTab = tabsStore.activeTab
    if (!activeTab) return

    tabsStore.setPage(newPage)
    await fetchLogs(activeTab.filters)
  }

  /**
   * Изменить размер страницы и загрузить логи
   */
  async function setPageSize(newSize: number) {
    const tabsStore = useTabsStore()
    const activeTab = tabsStore.activeTab
    if (!activeTab) return

    tabsStore.setPageSize(newSize)
    await fetchLogs(activeTab.filters)
  }

  /**
   * Очистить ошибку
   */
  function clearError() {
    const tabsStore = useTabsStore()
    tabsStore.clearError()
  }

  /**
   * Добавляет новые логи в активную вкладку (для real-time обновлений через SignalR).
   *
   * ВАЖНО:
   * - levelCounts обновляются для ВСЕХ новых логов (это глобальные счётчики файла)
   * - totalCount и totalPages НЕ обновляются здесь — они отражают результат фильтрации
   *   и должны приходить от сервера при следующем fetchLogs
   * - В UI добавляются только логи, соответствующие текущим фильтрам
   * - Размер logs[] ограничен pageSize для предотвращения переполнения страницы
   *
   * @param newLogs - Массив новых LogEntry для добавления
   * @param filters - Фильтры активной вкладки для клиентской фильтрации
   * @param activeLevels - Активные уровни логирования (для быстрой проверки)
   */
  function appendLogs(
    newLogs: LogEntry[],
    filters: FilterOptions,
    activeLevels: Set<LogLevel>
  ) {
    const tabsStore = useTabsStore()
    const activeTab = tabsStore.activeTab
    if (!activeTab?.sessionId || newLogs.length === 0) {
      return
    }

    // 1. Обновляем счётчики по уровням (для badge'ей в FilterPanel)
    // Это глобальные счётчики файла, не зависящие от фильтров
    const updatedLevelCounts = { ...activeTab.levelCounts }
    newLogs.forEach((log) => {
      const currentLevel = log.level
      if (currentLevel && updatedLevelCounts[currentLevel] !== undefined) {
        updatedLevelCounts[currentLevel]++
      }
    })

    // 2. НЕ обновляем totalCount и totalPages — они отражают результат фильтрации
    // и должны приходить от сервера. Клиентский пересчёт будет неточным при активных фильтрах.

    // 3. Если пользователь НЕ на последней странице — не добавляем в UI
    // (новые логи будут на последних страницах)
    if (activeTab.page < activeTab.totalPages) {
      console.info(`Received ${newLogs.length} new log entries, but user is not on last page (page ${activeTab.page}/${activeTab.totalPages})`)
      tabsStore.updateTab(activeTab.id, { levelCounts: updatedLevelCounts })
      return
    }

    // 4. Фильтруем логи по переданным фильтрам
    const filteredLogs = newLogs.filter((log) => {
      // Проверяем уровень через activeLevels
      const levelNumeric = stringLevelToNumeric(log.level)
      if (levelNumeric !== null && !activeLevels.has(levelNumeric)) {
        return false
      }

      // Проверяем searchText (поиск в message, без учёта регистра)
      if (filters.searchText) {
        const searchLower = filters.searchText.toLowerCase()
        if (!log.message.toLowerCase().includes(searchLower)) {
          return false
        }
      }

      // Проверяем logger (если задан)
      if (filters.logger) {
        const loggerLower = filters.logger.toLowerCase()
        if (!log.logger.toLowerCase().includes(loggerLower)) {
          return false
        }
      }

      // Проверяем fromDate
      if (filters.fromDate) {
        const logDate = new Date(log.timestamp)
        const fromDateObj = new Date(filters.fromDate)
        if (logDate < fromDateObj) {
          return false
        }
      }

      // Проверяем toDate
      if (filters.toDate) {
        const logDate = new Date(log.timestamp)
        const toDateObj = new Date(filters.toDate)
        if (logDate > toDateObj) {
          return false
        }
      }

      return true
    })

    // 5. Добавляем отфильтрованные логи в UI с ограничением pageSize
    if (filteredLogs.length > 0) {
      const updatedLogs = [...activeTab.logs, ...filteredLogs]

      // Ограничиваем размер массива до pageSize, удаляя старые записи с начала
      // Это предотвращает переполнение страницы и дубли при навигации
      const finalLogs = updatedLogs.length > activeTab.pageSize
        ? updatedLogs.slice(updatedLogs.length - activeTab.pageSize)
        : updatedLogs

      tabsStore.updateTab(activeTab.id, {
        logs: finalLogs,
        levelCounts: updatedLevelCounts
      })

      console.info(`Appended ${filteredLogs.length}/${newLogs.length} new log entries matching filters (displayed: ${finalLogs.length})`)
    } else {
      tabsStore.updateTab(activeTab.id, { levelCounts: updatedLevelCounts })
      if (newLogs.length > 0) {
        console.info(`Received ${newLogs.length} new log entries, but none match current filters`)
      }
    }
  }

  /**
   * Преобразует строковый уровень лога в числовое значение LogLevel.
   */
  function stringLevelToNumeric(level: string): LogLevel | null {
    const levelMap: Record<string, LogLevel> = {
      Trace: LogLevel.Trace,
      Debug: LogLevel.Debug,
      Info: LogLevel.Info,
      Warn: LogLevel.Warn,
      Error: LogLevel.Error,
      Fatal: LogLevel.Fatal
    }
    return levelMap[level] ?? null
  }

  return {
    // Методы (сервис)
    uploadFile,
    openFile,
    openDirectory,
    fetchLogs,
    setPage,
    setPageSize,
    clearError,
    appendLogs
  }
})
