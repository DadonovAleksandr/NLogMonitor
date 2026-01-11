import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { LogEntry, LevelCounts, FilterOptions, TabSetting, UserSettings } from '@/types'

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
    // Settings sync
    exportToSettings,
    importFromSettings
  }
})
