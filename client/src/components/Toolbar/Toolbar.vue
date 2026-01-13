<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Search, X, ArrowDown, Pause, Play, Trash2 } from 'lucide-vue-next'
import { useTabsStore, useFilterStore } from '@/stores'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger
} from '@/components/ui/tooltip'
import type { LevelCounts } from '@/types'
import { LogLevel, LogLevelNames } from '@/types'

interface LevelFilterButton {
  level: keyof LevelCounts
  label: string
  shortLabel: string
  icon: string
  colorClass: string
  activeClass: string
}

const tabsStore = useTabsStore()
const filterStore = useFilterStore()

const searchText = ref('')

const emit = defineEmits<{
  (e: 'search', value: string): void
  (e: 'filter-levels', levels: Set<string>): void
  (e: 'clear'): void
}>()

const levelButtons: LevelFilterButton[] = [
  {
    level: 'Trace',
    label: 'Trace',
    shortLabel: 'TRC',
    icon: '/images/levels/Trace.png',
    colorClass: 'bg-cyan-950/50 text-cyan-400 border-cyan-800',
    activeClass: 'bg-cyan-600 text-white border-cyan-500 shadow-lg shadow-cyan-900/50'
  },
  {
    level: 'Debug',
    label: 'Debug',
    shortLabel: 'DBG',
    icon: '/images/levels/Debug.png',
    colorClass: 'bg-blue-950/50 text-blue-400 border-blue-800',
    activeClass: 'bg-blue-600 text-white border-blue-500 shadow-lg shadow-blue-900/50'
  },
  {
    level: 'Info',
    label: 'Info',
    shortLabel: 'INF',
    icon: '/images/levels/Info.png',
    colorClass: 'bg-emerald-950/50 text-emerald-400 border-emerald-800',
    activeClass: 'bg-emerald-600 text-white border-emerald-500 shadow-lg shadow-emerald-900/50'
  },
  {
    level: 'Warn',
    label: 'Warn',
    shortLabel: 'WRN',
    icon: '/images/levels/Warning.png',
    colorClass: 'bg-yellow-950/50 text-yellow-400 border-yellow-800',
    activeClass: 'bg-yellow-600 text-white border-yellow-500 shadow-lg shadow-yellow-900/50'
  },
  {
    level: 'Error',
    label: 'Error',
    shortLabel: 'ERR',
    icon: '/images/levels/Error.png',
    colorClass: 'bg-orange-950/50 text-orange-400 border-orange-800',
    activeClass: 'bg-orange-600 text-white border-orange-500 shadow-lg shadow-orange-900/50'
  },
  {
    level: 'Fatal',
    label: 'Fatal',
    shortLabel: 'FTL',
    icon: '/images/levels/Fatal.png',
    colorClass: 'bg-red-950/50 text-red-400 border-red-800',
    activeClass: 'bg-red-600 text-white border-red-500 shadow-lg shadow-red-900/50'
  }
]

const activeTab = computed(() => tabsStore.activeTab)

const levelCounts = computed(() => activeTab.value?.levelCounts || {
  Trace: 0,
  Debug: 0,
  Info: 0,
  Warn: 0,
  Error: 0,
  Fatal: 0
})

const levelNameToEnum: Record<keyof LevelCounts, LogLevel> = {
  'Trace': LogLevel.Trace,
  'Debug': LogLevel.Debug,
  'Info': LogLevel.Info,
  'Warn': LogLevel.Warn,
  'Error': LogLevel.Error,
  'Fatal': LogLevel.Fatal
}

const totalFiltered = computed(() => {
  let total = 0
  const activeLevelsSet = filterStore.activeLevels
  Object.entries(levelCounts.value).forEach(([levelName, count]) => {
    const levelEnum = levelNameToEnum[levelName as keyof LevelCounts]
    if (levelEnum !== undefined && activeLevelsSet.has(levelEnum) && count) {
      total += count
    }
  })
  return total
})

// Table controls
const isAutoscroll = computed(() => activeTab.value?.autoscroll || false)
const isPaused = computed(() => activeTab.value?.isPaused || false)

function toggleLevel(level: string) {
  const levelEnum = levelNameToEnum[level as keyof LevelCounts]
  if (levelEnum !== undefined) {
    filterStore.toggleLevel(levelEnum)
  }
}

function isLevelActive(level: string) {
  const levelEnum = levelNameToEnum[level as keyof LevelCounts]
  return levelEnum !== undefined && filterStore.isLevelActive(levelEnum)
}

function clearSearch() {
  searchText.value = ''
}

function toggleAutoscroll() {
  tabsStore.toggleAutoscroll()
}

function togglePause() {
  tabsStore.togglePause()
}

function handleClear() {
  emit('clear')
  tabsStore.clearLogs()
}

// Debounced search
let searchTimeout: ReturnType<typeof setTimeout> | null = null

watch(searchText, (newValue) => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    emit('search', newValue)
  }, 300)
})

watch(() => filterStore.activeLevels, () => {
  const levelNames = new Set(
    Array.from(filterStore.activeLevels).map(level => LogLevelNames[level])
  )
  emit('filter-levels', levelNames)
}, { deep: true })
</script>

<template>
  <div class="toolbar">
    <TooltipProvider :delay-duration="300">
      <!-- Level Filters -->
      <div class="filters-wrapper">
        <Tooltip v-for="btn in levelButtons" :key="btn.level">
          <TooltipTrigger as-child>
            <button
              class="filter-pill"
              :class="{ 'filter-pill-active': isLevelActive(btn.level) }"
              :data-level="btn.level.toLowerCase()"
              @click="toggleLevel(btn.level)"
            >
              <img
                :src="btn.icon"
                :alt="btn.label"
                class="filter-pill-icon"
              />
              <span class="filter-pill-count">{{ (levelCounts[btn.level] ?? 0).toLocaleString() }}</span>
            </button>
          </TooltipTrigger>
          <TooltipContent side="bottom" :side-offset="4">
            {{ btn.label }}
          </TooltipContent>
        </Tooltip>
      </div>

      <!-- Separator -->
      <div class="toolbar-separator" />

      <!-- Search Bar -->
      <div class="search-wrapper">
        <Search class="search-icon" />
        <input
          v-model="searchText"
          type="text"
          placeholder="Search..."
          class="search-input"
        />
        <button
          v-if="searchText"
          class="search-clear"
          @click="clearSearch"
        >
          <X class="h-3 w-3" />
        </button>
      </div>

      <!-- Separator -->
      <div class="toolbar-separator" />

      <!-- Table Controls -->
      <div class="controls-wrapper">
        <!-- Autoscroll -->
        <Tooltip>
          <TooltipTrigger as-child>
            <button
              class="control-btn"
              :class="{ 'control-btn-active': isAutoscroll }"
              @click="toggleAutoscroll"
            >
              <ArrowDown class="control-icon" :class="{ 'animate-pulse': isAutoscroll }" />
            </button>
          </TooltipTrigger>
          <TooltipContent side="bottom" :side-offset="4">
            Автопрокрутка
          </TooltipContent>
        </Tooltip>

        <!-- Pause -->
        <Tooltip>
          <TooltipTrigger as-child>
            <button
              class="control-btn"
              :class="{ 'control-btn-paused': isPaused }"
              @click="togglePause"
            >
              <Pause v-if="!isPaused" class="control-icon" />
              <Play v-else class="control-icon animate-pulse" />
            </button>
          </TooltipTrigger>
          <TooltipContent side="bottom" :side-offset="4">
            {{ isPaused ? 'Продолжить' : 'Пауза' }}
          </TooltipContent>
        </Tooltip>

        <!-- Clear -->
        <Tooltip>
          <TooltipTrigger as-child>
            <button class="control-btn control-btn-clear" @click="handleClear">
              <Trash2 class="control-icon" />
            </button>
          </TooltipTrigger>
          <TooltipContent side="bottom" :side-offset="4">
            Очистить
          </TooltipContent>
        </Tooltip>
      </div>

      <!-- Total Count -->
      <div class="total-count">
        <span class="total-count-value">{{ totalFiltered.toLocaleString() }}</span>
      </div>
    </TooltipProvider>
  </div>
</template>

<style scoped>
.toolbar {
  display: flex;
  align-items: center;
  height: 32px;
  padding: 0 8px;
  background: linear-gradient(to bottom, #fafafa 0%, #f5f5f5 100%);
  border-bottom: 1px solid #e5e5e5;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  gap: 8px;
}

/* Search */
.search-wrapper {
  position: relative;
  flex: 1;
  min-width: 120px;
}

.search-icon {
  position: absolute;
  left: 8px;
  top: 50%;
  transform: translateY(-50%);
  width: 12px;
  height: 12px;
  color: #a3a3a3;
  pointer-events: none;
}

.search-input {
  width: 100%;
  height: 24px;
  padding: 0 24px 0 26px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 4px;
  font-size: 11px;
  color: #171717;
  transition: all 0.12s ease;
}

.search-input::placeholder {
  color: #a3a3a3;
}

.search-input:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
}

.search-clear {
  position: absolute;
  right: 4px;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  padding: 0;
  background: none;
  border: none;
  border-radius: 3px;
  color: #a3a3a3;
  cursor: pointer;
  transition: all 0.12s ease;
}

.search-clear:hover {
  background: #f5f5f5;
  color: #525252;
}

/* Separator */
.toolbar-separator {
  width: 1px;
  height: 18px;
  background: #e5e5e5;
  flex-shrink: 0;
}

/* Filters */
.filters-wrapper {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.filter-pill {
  display: flex;
  align-items: center;
  gap: 4px;
  height: 22px;
  padding: 0 6px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 3px;
  font-size: 11px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.12s ease;
}

.filter-pill:hover:not(.filter-pill-active) {
  background: #f5f5f5;
  border-color: #a3a3a3;
}

/* Active states */
.filter-pill-active[data-level="trace"] {
  background: #D3D3D3;
  border-color: #b0b0b0;
  color: #000000;
}

.filter-pill-active[data-level="debug"] {
  background: #ADD8E6;
  border-color: #8ec8dc;
  color: #000000;
}

.filter-pill-active[data-level="info"] {
  background: #ffffff;
  border-color: #3b82f6;
  color: #000000;
  box-shadow: 0 0 0 1px rgba(59, 130, 246, 0.2);
}

.filter-pill-active[data-level="warn"] {
  background: #FFFF00;
  border-color: #d4d400;
  color: #000000;
}

.filter-pill-active[data-level="error"] {
  background: #FF0000;
  border-color: #cc0000;
  color: #ffffff;
}

.filter-pill-active[data-level="fatal"] {
  background: #8B0000;
  border-color: #600000;
  color: #ffffff;
}

.filter-pill-icon {
  width: 12px;
  height: 12px;
  opacity: 0.7;
}

.filter-pill-active .filter-pill-icon {
  opacity: 1;
}

.filter-pill-count {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  min-width: 16px;
  text-align: center;
  font-variant-numeric: tabular-nums;
}

/* Controls */
.controls-wrapper {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}

.control-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 4px;
  color: #525252;
  cursor: pointer;
  transition: all 0.12s ease;
}

.control-btn:hover {
  background: #f5f5f5;
  border-color: #a3a3a3;
}

.control-btn-active {
  background: #dbeafe;
  border-color: #60a5fa;
  color: #1e40af;
}

.control-btn-paused {
  background: #fef3c7;
  border-color: #fbbf24;
  color: #92400e;
}

.control-btn-clear:hover {
  background: #fee2e2;
  border-color: #fca5a5;
  color: #991b1b;
}

.control-icon {
  width: 12px;
  height: 12px;
}

/* Total count */
.total-count {
  display: flex;
  align-items: center;
  height: 22px;
  padding: 0 8px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 4px;
  flex-shrink: 0;
}

.total-count-value {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 600;
  color: #171717;
  font-variant-numeric: tabular-nums;
}
</style>
