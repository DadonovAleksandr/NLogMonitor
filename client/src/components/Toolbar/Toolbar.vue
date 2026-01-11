<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Search, X } from 'lucide-vue-next'
import { Input } from '@/components/ui/input'
import { useTabsStore, useFilterStore } from '@/stores'
import type { LevelCounts } from '@/types'
import { LogLevel, LogLevelNames } from '@/types'

interface LevelFilterButton {
  level: keyof LevelCounts
  label: string
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
}>()

const levelButtons: LevelFilterButton[] = [
  {
    level: 'Trace',
    label: 'Trace',
    icon: '/images/levels/Trace.png',
    colorClass: 'bg-cyan-950/50 text-cyan-400 border-cyan-800',
    activeClass: 'bg-cyan-600 text-white border-cyan-500 shadow-lg shadow-cyan-900/50'
  },
  {
    level: 'Debug',
    label: 'Debug',
    icon: '/images/levels/Debug.png',
    colorClass: 'bg-blue-950/50 text-blue-400 border-blue-800',
    activeClass: 'bg-blue-600 text-white border-blue-500 shadow-lg shadow-blue-900/50'
  },
  {
    level: 'Info',
    label: 'Info',
    icon: '/images/levels/Info.png',
    colorClass: 'bg-emerald-950/50 text-emerald-400 border-emerald-800',
    activeClass: 'bg-emerald-600 text-white border-emerald-500 shadow-lg shadow-emerald-900/50'
  },
  {
    level: 'Warn',
    label: 'Warn',
    icon: '/images/levels/Warning.png',
    colorClass: 'bg-yellow-950/50 text-yellow-400 border-yellow-800',
    activeClass: 'bg-yellow-600 text-white border-yellow-500 shadow-lg shadow-yellow-900/50'
  },
  {
    level: 'Error',
    label: 'Error',
    icon: '/images/levels/Error.png',
    colorClass: 'bg-orange-950/50 text-orange-400 border-orange-800',
    activeClass: 'bg-orange-600 text-white border-orange-500 shadow-lg shadow-orange-900/50'
  },
  {
    level: 'Fatal',
    label: 'Fatal',
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

// Конвертация названия уровня в LogLevel enum
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
    if (activeLevelsSet.has(levelEnum) && count) {
      total += count
    }
  })
  return total
})

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

// Debounced search
let searchTimeout: ReturnType<typeof setTimeout> | null = null

watch(searchText, (newValue) => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    emit('search', newValue)
  }, 300)
})

// Следим за изменениями activeLevels в filterStore и испускаем событие
watch(() => filterStore.activeLevels, () => {
  // Преобразуем Set<LogLevel> в Set<string> для совместимости с событием
  const levelNames = new Set(
    Array.from(filterStore.activeLevels).map(level => LogLevelNames[level])
  )
  emit('filter-levels', levelNames)
}, { deep: true })
</script>

<template>
  <div class="toolbar">
    <!-- Search Bar -->
    <div class="search-wrapper">
      <Search class="search-icon" />
      <input
        v-model="searchText"
        type="text"
        placeholder="Search in messages..."
        class="search-input"
      />
      <button
        v-if="searchText"
        class="search-clear"
        @click="clearSearch"
      >
        <X class="h-3.5 w-3.5" />
      </button>
    </div>

    <!-- Separator -->
    <div class="toolbar-separator" />

    <!-- Level Filters -->
    <div class="filters-wrapper">
      <span class="filters-label">Filters:</span>

      <div class="filters-buttons">
        <button
          v-for="btn in levelButtons"
          :key="btn.level"
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
          <span class="filter-pill-label">{{ btn.label }}</span>
          <span class="filter-pill-count">{{ (levelCounts[btn.level] ?? 0).toLocaleString() }}</span>
        </button>
      </div>

      <!-- Total Count -->
      <div class="total-count">
        <span class="total-count-label">Showing:</span>
        <span class="total-count-value">{{ totalFiltered.toLocaleString() }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Import IBM Plex Mono for technical data */
@import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&display=swap');

.toolbar {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 10px 16px;
  background: linear-gradient(to bottom, #fafafa 0%, #f5f5f5 100%);
  border-bottom: 1px solid #e5e5e5;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
}

.search-wrapper {
  position: relative;
  min-width: 280px;
}

.search-icon {
  position: absolute;
  left: 10px;
  top: 50%;
  transform: translateY(-50%);
  width: 14px;
  height: 14px;
  color: #a3a3a3;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: 6px 32px 6px 32px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 6px;
  font-size: 12px;
  color: #171717;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
}

.search-input::placeholder {
  color: #a3a3a3;
}

.search-input:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1), 0 1px 2px rgba(0, 0, 0, 0.04);
}

.search-clear {
  position: absolute;
  right: 8px;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  padding: 0;
  background: none;
  border: none;
  border-radius: 4px;
  color: #a3a3a3;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

.search-clear:hover {
  background: #f5f5f5;
  color: #525252;
}

.toolbar-separator {
  width: 1px;
  height: 24px;
  background: #d4d4d4;
}

.filters-wrapper {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.filters-label {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.5px;
  color: #737373;
  text-transform: uppercase;
}

.filters-buttons {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.filter-pill {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 14px;
  font-size: 11px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.filter-pill:hover {
  background: #fafafa;
  border-color: #a3a3a3;
  box-shadow: 0 2px 3px rgba(0, 0, 0, 0.06);
  transform: translateY(-0.5px);
}

.filter-pill:active {
  transform: translateY(0);
}

/* Active states using existing color scheme */
.filter-pill-active[data-level="trace"] {
  background: #f8f8f8;
  border-color: #cbd5e1;
  color: #52525b;
  box-shadow: 0 0 0 2px #f8f8f8;
}

.filter-pill-active[data-level="debug"] {
  background: #e0f7fa;
  border-color: #67e8f9;
  color: #0e4f5c;
  box-shadow: 0 0 0 2px rgba(103, 232, 249, 0.2);
}

.filter-pill-active[data-level="info"] {
  background: #ffffff;
  border-color: #94a3b8;
  color: #334155;
  box-shadow: 0 0 0 2px rgba(148, 163, 184, 0.2);
}

.filter-pill-active[data-level="warn"] {
  background: #fef3c7;
  border-color: #fbbf24;
  color: #92400e;
  box-shadow: 0 0 0 2px rgba(251, 191, 36, 0.2);
}

.filter-pill-active[data-level="error"] {
  background: #fee2e2;
  border-color: #fca5a5;
  color: #991b1b;
  box-shadow: 0 0 0 2px rgba(252, 165, 165, 0.2);
}

.filter-pill-active[data-level="fatal"] {
  background: #fca5a5;
  border-color: #f87171;
  color: #7f1d1d;
  box-shadow: 0 0 0 2px rgba(248, 113, 113, 0.3);
}

.filter-pill-icon {
  width: 13px;
  height: 13px;
  opacity: 0.7;
}

.filter-pill-active .filter-pill-icon {
  opacity: 1;
}

.filter-pill-label {
  font-weight: 500;
}

.filter-pill-count {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px;
  font-weight: 500;
  padding: 1px 5px;
  background: rgba(0, 0, 0, 0.05);
  border-radius: 6px;
  color: #737373;
  min-width: 24px;
  text-align: center;
  tabular-nums: tabular-nums;
}

.filter-pill-active .filter-pill-count {
  background: rgba(0, 0, 0, 0.08);
  color: currentColor;
  font-weight: 600;
}

.total-count {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-left: auto;
  padding: 4px 12px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 6px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.total-count-label {
  font-size: 11px;
  font-weight: 500;
  color: #737373;
}

.total-count-value {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 12px;
  font-weight: 600;
  color: #171717;
  tabular-nums: tabular-nums;
}

/* Smooth staggered animations */
@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-4px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.filter-pill {
  animation: slideIn 0.2s ease-out backwards;
}

.filter-pill:nth-child(1) { animation-delay: 0ms; }
.filter-pill:nth-child(2) { animation-delay: 30ms; }
.filter-pill:nth-child(3) { animation-delay: 60ms; }
.filter-pill:nth-child(4) { animation-delay: 90ms; }
.filter-pill:nth-child(5) { animation-delay: 120ms; }
.filter-pill:nth-child(6) { animation-delay: 150ms; }
</style>
