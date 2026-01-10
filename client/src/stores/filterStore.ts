import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { LogLevel } from '@/types'
import type { FilterOptions } from '@/types'

// Маппинг числовых значений в строковые названия уровней
const LogLevelNames: Record<LogLevel, string> = {
  [LogLevel.Trace]: 'Trace',
  [LogLevel.Debug]: 'Debug',
  [LogLevel.Info]: 'Info',
  [LogLevel.Warn]: 'Warn',
  [LogLevel.Error]: 'Error',
  [LogLevel.Fatal]: 'Fatal'
}

export const useFilterStore = defineStore('filters', () => {
  // State
  const searchText = ref('')
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

    // Отправляем массив активных уровней, если не все 6 уровней выбраны
    // Включая случай когда activeLevels.size === 0 (пустой массив → нет результатов)
    if (activeLevels.value.size < 6) {
      options.levels = Array.from(activeLevels.value)
        .sort((a, b) => a - b)
        .map(level => LogLevelNames[level])
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
  }

  function isLevelActive(level: LogLevel): boolean {
    return activeLevels.value.has(level)
  }

  function clearFilters() {
    searchText.value = ''
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
