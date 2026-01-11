import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { LogEntry, LevelCounts, FilterOptions, TabSetting, UserSettings } from '@/types'
import { LogLevel } from '@/types'

// Маппинг числовых значений в строковые названия уровней (для API)
const LogLevelNames: Record<LogLevel, string> = {
  [LogLevel.Trace]: 'Trace',
  [LogLevel.Debug]: 'Debug',
  [LogLevel.Info]: 'Info',
  [LogLevel.Warn]: 'Warn',
  [LogLevel.Error]: 'Error',
  [LogLevel.Fatal]: 'Fatal'
}

export interface Tab {
  id: string
  type: 'file' | 'directory'
  fileName: string
  filePath: string
  sessionId: string | null
  logs: LogEntry[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  levelCounts: LevelCounts
  isLoading: boolean
  error: string | null
  // Filters specific to this tab
  filters: FilterOptions
  searchText: string
  // NEW: activeLevels для UI FilterPanel (числовые значения LogLevel)
  activeLevels: Set<LogLevel>
  // Controls
  autoscroll: boolean
  isPaused: boolean
}

export const useTabsStore = defineStore('tabs', () => {
  // State
  const tabs = ref<Tab[]>([])
  const activeTabId = ref<string | null>(null)

  // Getters
  const activeTab = computed(() =>
    tabs.value.find(tab => tab.id === activeTabId.value) || null
  )

  const hasTabs = computed(() => tabs.value.length > 0)

  // Actions
  function createTab(type: 'file' | 'directory', fileName: string, filePath: string): Tab {
    const newTab: Tab = {
      id: `tab-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      type,
      fileName,
      filePath,
      sessionId: null,
      logs: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
      totalPages: 0,
      levelCounts: {
        Trace: 0,
        Debug: 0,
        Info: 0,
        Warn: 0,
        Error: 0,
        Fatal: 0
      },
      isLoading: false,
      error: null,
      filters: {},
      searchText: '',
      // Инициализируем activeLevels со всеми 6 уровнями (все включены по умолчанию)
      activeLevels: new Set([
        LogLevel.Trace,
        LogLevel.Debug,
        LogLevel.Info,
        LogLevel.Warn,
        LogLevel.Error,
        LogLevel.Fatal
      ]),
      autoscroll: true,
      isPaused: false
    }

    tabs.value.push(newTab)
    activeTabId.value = newTab.id

    return newTab
  }

  function addTab(type: 'file' | 'directory', fileName: string, filePath: string, sessionId?: string) {
    const tab = createTab(type, fileName, filePath)
    if (sessionId) {
      tab.sessionId = sessionId
    }
    return tab
  }

  function closeTab(tabId: string) {
    const index = tabs.value.findIndex(tab => tab.id === tabId)
    if (index === -1) return

    tabs.value.splice(index, 1)

    // If we closed the active tab, switch to another tab
    if (activeTabId.value === tabId) {
      if (tabs.value.length > 0) {
        // Switch to the tab before the closed one, or the first tab
        const newIndex = Math.max(0, index - 1)
        activeTabId.value = tabs.value[newIndex]?.id || null
      } else {
        activeTabId.value = null
      }
    }
  }

  function setActiveTab(tabId: string) {
    if (tabs.value.some(tab => tab.id === tabId)) {
      activeTabId.value = tabId
    }
  }

  function updateTab(tabId: string, updates: Partial<Tab>) {
    const tab = tabs.value.find(t => t.id === tabId)
    if (tab) {
      Object.assign(tab, updates)
    }
  }

  function getTab(tabId: string): Tab | null {
    return tabs.value.find(tab => tab.id === tabId) || null
  }

  function clearAllTabs() {
    tabs.value = []
    activeTabId.value = null
  }

  // Toggle controls for active tab
  function toggleAutoscroll() {
    if (activeTab.value) {
      activeTab.value.autoscroll = !activeTab.value.autoscroll
    }
  }

  function togglePause() {
    if (activeTab.value) {
      activeTab.value.isPaused = !activeTab.value.isPaused
    }
  }

  function clearLogs() {
    if (activeTab.value) {
      activeTab.value.logs = []
      activeTab.value.totalCount = 0
      activeTab.value.page = 1
      activeTab.value.totalPages = 0
    }
  }

  /**
   * Экспортировать вкладки в формат настроек
   *
   * Преобразует текущие вкладки в формат UserSettings для сохранения на сервер.
   * @returns Объект с массивом вкладок и индексом активной вкладки
   */
  function exportToSettings(): UserSettings {
    const openedTabs: TabSetting[] = tabs.value.map(tab => ({
      type: tab.type,
      path: tab.filePath,
      displayName: tab.fileName
    }))

    const lastActiveTabIndex = activeTabId.value
      ? tabs.value.findIndex(t => t.id === activeTabId.value)
      : 0

    return {
      openedTabs,
      lastActiveTabIndex: Math.max(0, lastActiveTabIndex)
    }
  }

  /**
   * Импортировать вкладки из настроек
   *
   * Возвращает массив настроек вкладок для восстановления.
   * Сами вкладки создаются через addTab после открытия файлов/директорий.
   * @param settings - Пользовательские настройки
   * @returns Массив настроек вкладок для восстановления
   */
  function importFromSettings(settings: UserSettings): TabSetting[] {
    return settings.openedTabs || []
  }

  /**
   * Обновить фильтры активной вкладки и пересчитать filters.levels для API
   *
   * @param updates - Объект с обновлениями для фильтров активной вкладки
   */
  function updateTabFilters(updates: {
    searchText?: string
    activeLevels?: Set<LogLevel>
    fromDate?: string | undefined
    toDate?: string | undefined
    logger?: string | undefined
  }) {
    if (!activeTab.value) return

    // Обновляем поля
    if (updates.searchText !== undefined) {
      activeTab.value.searchText = updates.searchText
    }
    if (updates.activeLevels !== undefined) {
      activeTab.value.activeLevels = updates.activeLevels
    }
    if (updates.fromDate !== undefined) {
      activeTab.value.filters.fromDate = updates.fromDate
    }
    if (updates.toDate !== undefined) {
      activeTab.value.filters.toDate = updates.toDate
    }
    if (updates.logger !== undefined) {
      activeTab.value.filters.logger = updates.logger
    }

    // Пересчитываем filters для API
    rebuildActiveTabFilters()
  }

  /**
   * Пересчитать filters.levels и filters.searchText из activeLevels и searchText
   */
  function rebuildActiveTabFilters() {
    if (!activeTab.value) return

    const filters: FilterOptions = {}

    // Добавить searchText
    if (activeTab.value.searchText) {
      filters.searchText = activeTab.value.searchText
    }

    // Добавить levels (если не все 6 уровней выбраны)
    if (activeTab.value.activeLevels.size < 6) {
      filters.levels = Array.from(activeTab.value.activeLevels)
        .sort((a, b) => a - b)
        .map(level => LogLevelNames[level])
    }

    // Копируем остальные поля из текущих filters
    if (activeTab.value.filters.fromDate) {
      filters.fromDate = activeTab.value.filters.fromDate
    }
    if (activeTab.value.filters.toDate) {
      filters.toDate = activeTab.value.filters.toDate
    }
    if (activeTab.value.filters.logger) {
      filters.logger = activeTab.value.filters.logger
    }

    activeTab.value.filters = filters
  }

  /**
   * Очистить фильтры активной вкладки (сбросить к дефолтным значениям)
   */
  function clearTabFilters() {
    if (!activeTab.value) return

    activeTab.value.searchText = ''
    activeTab.value.activeLevels = new Set([
      LogLevel.Trace,
      LogLevel.Debug,
      LogLevel.Info,
      LogLevel.Warn,
      LogLevel.Error,
      LogLevel.Fatal
    ])
    activeTab.value.filters = {}
  }

  return {
    // State
    tabs,
    activeTabId,
    // Getters
    activeTab,
    hasTabs,
    // Actions
    createTab,
    addTab,
    closeTab,
    setActiveTab,
    updateTab,
    getTab,
    clearAllTabs,
    toggleAutoscroll,
    togglePause,
    clearLogs,
    // Filter management
    updateTabFilters,
    clearTabFilters,
    // Settings sync
    exportToSettings,
    importFromSettings
  }
})
