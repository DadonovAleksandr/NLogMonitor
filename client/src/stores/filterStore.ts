import { defineStore } from 'pinia'
import { computed } from 'vue'
import { LogLevel } from '@/types'
import type { FilterOptions } from '@/types'
import { useTabsStore } from './tabsStore'

/**
 * filterStore — UI-state адаптер для работы с фильтрами активной вкладки.
 * Не хранит собственное состояние, а читает/пишет в tabsStore.activeTab.
 * Это позволяет компонентам (FilterPanel, SearchBar) не знать о системе вкладок.
 */
export const useFilterStore = defineStore('filters', () => {
  const tabsStore = useTabsStore()

  // Computed properties — читаем/пишем в активную вкладку
  const searchText = computed({
    get: () => tabsStore.activeTab?.searchText ?? '',
    set: (value: string) => {
      tabsStore.updateTabFilters({ searchText: value })
    }
  })

  const activeLevels = computed({
    get: () => tabsStore.activeTab?.activeLevels ?? new Set<LogLevel>(),
    set: (value: Set<LogLevel>) => {
      tabsStore.updateTabFilters({ activeLevels: value })
    }
  })

  const fromDate = computed({
    get: () => tabsStore.activeTab?.filters.fromDate,
    set: (value: string | undefined) => {
      tabsStore.updateTabFilters({ fromDate: value })
    }
  })

  const toDate = computed({
    get: () => tabsStore.activeTab?.filters.toDate,
    set: (value: string | undefined) => {
      tabsStore.updateTabFilters({ toDate: value })
    }
  })

  const logger = computed({
    get: () => tabsStore.activeTab?.filters.logger,
    set: (value: string | undefined) => {
      tabsStore.updateTabFilters({ logger: value })
    }
  })

  // Getters
  const hasActiveFilters = computed(() => {
    if (!tabsStore.activeTab) return false

    return tabsStore.activeTab.searchText !== '' ||
      tabsStore.activeTab.filters.fromDate !== undefined ||
      tabsStore.activeTab.filters.toDate !== undefined ||
      tabsStore.activeTab.filters.logger !== undefined ||
      tabsStore.activeTab.activeLevels.size < 6
  })

  const filterOptions = computed<FilterOptions>(() => {
    return tabsStore.activeTab?.filters ?? {}
  })

  // Actions
  function setSearchText(text: string) {
    searchText.value = text
  }

  function setDateRange(from: string | undefined, to: string | undefined) {
    fromDate.value = from
    toDate.value = to
  }

  function setLogger(loggerName: string | undefined) {
    logger.value = loggerName
  }

  function toggleLevel(level: LogLevel) {
    if (!tabsStore.activeTab) return

    const newLevels = new Set(tabsStore.activeTab.activeLevels)
    if (newLevels.has(level)) {
      newLevels.delete(level)
    } else {
      newLevels.add(level)
    }
    activeLevels.value = newLevels
  }

  function setAllLevels(active: boolean) {
    if (active) {
      activeLevels.value = new Set([
        LogLevel.Trace,
        LogLevel.Debug,
        LogLevel.Info,
        LogLevel.Warn,
        LogLevel.Error,
        LogLevel.Fatal
      ])
    } else {
      activeLevels.value = new Set()
    }
  }

  function isLevelActive(level: LogLevel): boolean {
    return tabsStore.activeTab?.activeLevels.has(level) ?? false
  }

  function clearFilters() {
    tabsStore.clearTabFilters()
  }

  return {
    // State (computed)
    searchText,
    fromDate,
    toDate,
    logger,
    activeLevels,
    // Getters
    hasActiveFilters,
    filterOptions,
    // Actions
    setSearchText,
    setDateRange,
    setLogger,
    toggleLevel,
    setAllLevels,
    isLevelActive,
    clearFilters
  }
})
