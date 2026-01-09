import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { LogEntry, OpenFileResult, FilterOptions, LevelCounts } from '@/types'
import { logsApi, filesApi } from '@/api'

export const useLogStore = defineStore('logs', () => {
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
      await fetchLogs()
    } catch (err: unknown) {
      error.value = (err as { message?: string }).message || 'Failed to upload file'
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
      await fetchLogs()
    } catch (err: unknown) {
      error.value = (err as { message?: string }).message || 'Failed to open file'
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
      await fetchLogs()
    } catch (err: unknown) {
      error.value = (err as { message?: string }).message || 'Failed to open directory'
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
      error.value = (err as { message?: string }).message || 'Failed to fetch logs'
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
    clearError
  }
})
