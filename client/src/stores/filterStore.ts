import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { LogLevel } from '@/types'
import type { FilterOptions } from '@/types'

export const useFilterStore = defineStore('filters', () => {
  // State
  const searchText = ref('')
  const minLevel = ref<LogLevel | undefined>(undefined)
  const maxLevel = ref<LogLevel | undefined>(undefined)
  const fromDate = ref<string | undefined>(undefined)
  const toDate = ref<string | undefined>(undefined)
  const logger = ref<string | undefined>(undefined)

  // Активные уровни (для toggle-кнопок)
  const activeLevels = ref<Set<LogLevel>>(new Set([
    LogLevel.Trace,
    LogLevel.Debug,
    LogLevel.Info,
    LogLevel.Warn,
    LogLevel.Error,
    LogLevel.Fatal
  ]))

  // Getters
  const hasActiveFilters = computed(() => {
    return searchText.value !== '' ||
      minLevel.value !== undefined ||
      maxLevel.value !== undefined ||
      fromDate.value !== undefined ||
      toDate.value !== undefined ||
      logger.value !== undefined ||
      activeLevels.value.size < 6
  })

  const filterOptions = computed<FilterOptions>(() => {
    const options: FilterOptions = {}

    if (searchText.value) {
      options.searchText = searchText.value
    }
    if (minLevel.value !== undefined) {
      options.minLevel = minLevel.value
    }
    if (maxLevel.value !== undefined) {
      options.maxLevel = maxLevel.value
    }
    if (fromDate.value) {
      options.fromDate = fromDate.value
    }
    if (toDate.value) {
      options.toDate = toDate.value
    }
    if (logger.value) {
      options.logger = logger.value
    }

    return options
  })

  // Actions
  function setSearchText(text: string) {
    searchText.value = text
  }

  function setMinLevel(level: LogLevel | undefined) {
    minLevel.value = level
  }

  function setMaxLevel(level: LogLevel | undefined) {
    maxLevel.value = level
  }

  function setDateRange(from: string | undefined, to: string | undefined) {
    fromDate.value = from
    toDate.value = to
  }

  function setLogger(loggerName: string | undefined) {
    logger.value = loggerName
  }

  function toggleLevel(level: LogLevel) {
    if (activeLevels.value.has(level)) {
      activeLevels.value.delete(level)
    } else {
      activeLevels.value.add(level)
    }
    // Пересчитываем minLevel/maxLevel на основе активных уровней
    updateLevelRange()
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
      activeLevels.value.clear()
    }
    updateLevelRange()
  }

  function isLevelActive(level: LogLevel): boolean {
    return activeLevels.value.has(level)
  }

  function updateLevelRange() {
    if (activeLevels.value.size === 0 || activeLevels.value.size === 6) {
      // Все или ничего — нет фильтрации по уровню
      minLevel.value = undefined
      maxLevel.value = undefined
      return
    }

    const levels = Array.from(activeLevels.value).sort((a, b) => a - b)
    minLevel.value = levels[0]
    maxLevel.value = levels[levels.length - 1]
  }

  function clearFilters() {
    searchText.value = ''
    minLevel.value = undefined
    maxLevel.value = undefined
    fromDate.value = undefined
    toDate.value = undefined
    logger.value = undefined
    activeLevels.value = new Set([
      LogLevel.Trace,
      LogLevel.Debug,
      LogLevel.Info,
      LogLevel.Warn,
      LogLevel.Error,
      LogLevel.Fatal
    ])
  }

  return {
    // State
    searchText,
    minLevel,
    maxLevel,
    fromDate,
    toDate,
    logger,
    activeLevels,
    // Getters
    hasActiveFilters,
    filterOptions,
    // Actions
    setSearchText,
    setMinLevel,
    setMaxLevel,
    setDateRange,
    setLogger,
    toggleLevel,
    setAllLevels,
    isLevelActive,
    clearFilters
  }
})
