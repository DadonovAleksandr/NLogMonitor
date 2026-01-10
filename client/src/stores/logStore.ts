import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { LogEntry, OpenFileResult, FilterOptions, LevelCounts } from '@/types'
import { LogLevel } from '@/types'
import { logsApi, filesApi } from '@/api'
import { useToast } from '@/composables/useToast'
import { useFilterStore } from './filterStore'

export const useLogStore = defineStore('logs', () => {
  const { showToast } = useToast()

  // State
  const sessionId = ref<string | null>(null)
  const fileName = ref<string>('')
  const filePath = ref<string>('')
  const logs = ref<LogEntry[]>([])
  const totalCount = ref(0)
  const page = ref(1)
  const pageSize = ref(50)
  const totalPages = ref(0)
  const levelCounts = ref<LevelCounts>({
    Trace: 0,
    Debug: 0,
    Info: 0,
    Warn: 0,
    Error: 0,
    Fatal: 0
  })
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Getters
  const hasSession = computed(() => sessionId.value !== null)
  const hasLogs = computed(() => logs.value.length > 0)
  const hasError = computed(() => error.value !== null)
  const canPreviousPage = computed(() => page.value > 1)
  const canNextPage = computed(() => page.value < totalPages.value)

  // Actions
  async function uploadFile(file: File) {
    isLoading.value = true
    error.value = null

    try {
      const result = await filesApi.uploadFile(file)
      setSessionData(result)
      // Загружаем все логи без фильтров при открытии нового файла
      await fetchLogs({})
      showToast(`File "${file.name}" loaded successfully`, 'success')
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to upload file'
      error.value = message
      showToast(message, 'error')
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function openFile(path: string) {
    isLoading.value = true
    error.value = null

    try {
      const result = await filesApi.openFile(path)
      setSessionData(result)
      // Загружаем все логи без фильтров при открытии нового файла
      await fetchLogs({})
      showToast(`File "${result.fileName}" loaded successfully`, 'success')
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to open file'
      error.value = message
      showToast(message, 'error')
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function openDirectory(path: string) {
    isLoading.value = true
    error.value = null

    try {
      const result = await filesApi.openDirectory(path)
      setSessionData(result)
      // Загружаем все логи без фильтров при открытии нового файла
      await fetchLogs({})
      showToast(`Directory "${result.fileName}" loaded successfully`, 'success')
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to open directory'
      error.value = message
      showToast(message, 'error')
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function setSessionData(result: OpenFileResult) {
    sessionId.value = result.sessionId
    fileName.value = result.fileName
    filePath.value = result.filePath
    totalCount.value = result.totalEntries
    levelCounts.value = result.levelCounts
  }

  async function fetchLogs(filters?: FilterOptions) {
    if (!sessionId.value) return

    isLoading.value = true
    error.value = null

    try {
      const result = await logsApi.getLogs(sessionId.value, {
        ...filters,
        page: page.value,
        pageSize: pageSize.value
      })

      logs.value = result.items
      totalCount.value = result.totalCount
      totalPages.value = result.totalPages
    } catch (err: unknown) {
      const message = (err as { message?: string }).message || 'Failed to fetch logs'
      error.value = message
      showToast(message, 'error')
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function setPage(newPage: number) {
    page.value = newPage
  }

  function setPageSize(newSize: number) {
    pageSize.value = newSize
    page.value = 1 // Reset to first page
  }

  function clearSession() {
    sessionId.value = null
    fileName.value = ''
    filePath.value = ''
    logs.value = []
    totalCount.value = 0
    page.value = 1
    totalPages.value = 0
    isLoading.value = false
    error.value = null
    levelCounts.value = {
      Trace: 0,
      Debug: 0,
      Info: 0,
      Warn: 0,
      Error: 0,
      Fatal: 0
    }
  }

  function clearError() {
    error.value = null
  }

  /**
   * Добавляет новые логи в текущую коллекцию (для real-time обновлений через SignalR).
   * Обновляет totalCount и levelCounts для ВСЕХ новых логов.
   * В UI добавляются только логи, соответствующие текущим фильтрам, и только если пользователь на последней странице.
   *
   * @param newLogs - Массив новых LogEntry для добавления
   */
  function appendLogs(newLogs: LogEntry[]) {
    if (!sessionId.value || newLogs.length === 0) {
      return
    }

    const filterStore = useFilterStore()

    // 1. Всегда обновляем общее количество записей
    totalCount.value += newLogs.length

    // 2. Всегда обновляем счётчики по уровням (для badge'ей)
    newLogs.forEach((log) => {
      const currentLevel = log.level
      if (currentLevel && levelCounts.value[currentLevel] !== undefined) {
        levelCounts.value[currentLevel]++
      }
    })

    // 3. Запоминаем старое totalPages для проверки "на последней странице"
    const wasOnLastPage = page.value >= totalPages.value

    // 4. Пересчитываем общее количество страниц
    totalPages.value = Math.ceil(totalCount.value / pageSize.value)

    // 5. Если пользователь НЕ на последней странице - не добавляем в UI
    if (!wasOnLastPage) {
      console.info(`Received ${newLogs.length} new log entries, but user is not on last page (page ${page.value}/${totalPages.value})`)
      return
    }

    // 6. Фильтруем логи по текущим фильтрам
    const filteredLogs = newLogs.filter((log) => {
      // Проверяем уровень
      const levelNumeric = stringLevelToNumeric(log.level)
      if (levelNumeric !== null && !filterStore.activeLevels.has(levelNumeric)) {
        return false
      }

      // Проверяем searchText (поиск в message, без учёта регистра)
      if (filterStore.searchText) {
        const searchLower = filterStore.searchText.toLowerCase()
        if (!log.message.toLowerCase().includes(searchLower)) {
          return false
        }
      }

      // Проверяем logger (если задан)
      if (filterStore.logger) {
        const loggerLower = filterStore.logger.toLowerCase()
        if (!log.logger.toLowerCase().includes(loggerLower)) {
          return false
        }
      }

      // Проверяем fromDate
      if (filterStore.fromDate) {
        const logDate = new Date(log.timestamp)
        const fromDateObj = new Date(filterStore.fromDate)
        if (logDate < fromDateObj) {
          return false
        }
      }

      // Проверяем toDate
      if (filterStore.toDate) {
        const logDate = new Date(log.timestamp)
        const toDateObj = new Date(filterStore.toDate)
        if (logDate > toDateObj) {
          return false
        }
      }

      return true
    })

    // 7. Добавляем отфильтрованные логи в UI
    if (filteredLogs.length > 0) {
      logs.value.push(...filteredLogs)
      console.info(`Appended ${filteredLogs.length}/${newLogs.length} new log entries matching filters (total: ${totalCount.value})`)
    } else if (newLogs.length > 0) {
      console.info(`Received ${newLogs.length} new log entries, but none match current filters`)
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
    // State
    sessionId,
    fileName,
    filePath,
    logs,
    totalCount,
    page,
    pageSize,
    totalPages,
    levelCounts,
    isLoading,
    error,
    // Getters
    hasSession,
    hasLogs,
    hasError,
    canPreviousPage,
    canNextPage,
    // Actions
    uploadFile,
    openFile,
    openDirectory,
    fetchLogs,
    setPage,
    setPageSize,
    clearSession,
    clearError,
    appendLogs
  }
})
