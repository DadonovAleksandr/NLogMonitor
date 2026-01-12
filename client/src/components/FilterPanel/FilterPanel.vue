<script setup lang="ts">
import { computed } from 'vue'
import { useFilterStore } from '@/stores/filterStore'
import { useLogStore } from '@/stores/logStore'
import { useTabsStore } from '@/stores/tabsStore'
import { LogLevel } from '@/types'

const filterStore = useFilterStore()
const logStore = useLogStore()
const tabsStore = useTabsStore()

// Проверка наличия активной вкладки
const hasActiveTab = computed(() => tabsStore.activeTab !== null)

// Конфигурация уровней с цветами и иконками из LogLevelBadge
interface LevelConfig {
  level: LogLevel
  name: string
  label: string
  icon: string
  bg: string
  text: string
  border: string
  glow: string
  glowActive: string
}

const levels: LevelConfig[] = [
  {
    level: LogLevel.Trace,
    name: 'Trace',
    label: 'TRC',
    icon: '/images/levels/Trace.png',
    bg: 'bg-[#f8f8f8]',
    text: 'text-zinc-600',
    border: 'border-slate-300',
    glow: '',
    glowActive: 'shadow-sm'
  },
  {
    level: LogLevel.Debug,
    name: 'Debug',
    label: 'DBG',
    icon: '/images/levels/Debug.png',
    bg: 'bg-[#e0f7fa]',
    text: 'text-[#0e4f5c]',
    border: 'border-cyan-300',
    glow: '',
    glowActive: 'shadow-md shadow-cyan-200'
  },
  {
    level: LogLevel.Info,
    name: 'Info',
    label: 'INF',
    icon: '/images/levels/Info.png',
    bg: 'bg-white',
    text: 'text-slate-700',
    border: 'border-slate-200',
    glow: '',
    glowActive: 'shadow-sm'
  },
  {
    level: LogLevel.Warn,
    name: 'Warn',
    label: 'WRN',
    icon: '/images/levels/Warning.png',
    bg: 'bg-[#fef3c7]',
    text: 'text-[#92400e]',
    border: 'border-amber-300',
    glow: '',
    glowActive: 'shadow-md shadow-amber-200'
  },
  {
    level: LogLevel.Error,
    name: 'Error',
    label: 'ERR',
    icon: '/images/levels/Error.png',
    bg: 'bg-[#fee2e2]',
    text: 'text-[#991b1b]',
    border: 'border-red-300',
    glow: '',
    glowActive: 'shadow-md shadow-red-200'
  },
  {
    level: LogLevel.Fatal,
    name: 'Fatal',
    label: 'FTL',
    icon: '/images/levels/Fatal.png',
    bg: 'bg-[#fca5a5]',
    text: 'text-[#7f1d1d]',
    border: 'border-rose-400',
    glow: '',
    glowActive: 'shadow-lg shadow-rose-300'
  }
]

// Проверка активности уровня
const isActive = (level: LogLevel) => filterStore.isLevelActive(level)

// Получение счетчика для уровня
const getCount = (levelName: string) => logStore.levelCounts[levelName] || 0

// Форматирование счетчика
const formatCount = (count: number) => {
  return count.toString()
}

// Обработка клика по кнопке
const handleToggle = (level: LogLevel) => {
  filterStore.toggleLevel(level)
}

// Количество отключённых фильтров
const inactiveCount = computed(() => {
  return levels.filter(l => !isActive(l.level)).length
})

// Сброс всех фильтров
const clearAll = () => {
  filterStore.setAllLevels(true)
}

// Отключить все
const disableAll = () => {
  filterStore.setAllLevels(false)
}
</script>

<template>
  <div class="filter-panel">
    <!-- Заголовок панели -->
    <div class="panel-header">
      <div class="header-label">
        LOG LEVELS
      </div>
      <div
        v-if="inactiveCount > 0"
        class="inactive-badge"
      >
        {{ inactiveCount }} hidden
      </div>
    </div>

    <!-- Кнопки фильтров -->
    <div class="filters-group">
      <button
        v-for="config in levels"
        :key="config.level"
        :disabled="!hasActiveTab"
        @click="handleToggle(config.level)"
        :class="[
          'filter-btn',
          !hasActiveTab && 'filter-btn-disabled',
          hasActiveTab && isActive(config.level) && 'filter-btn-active'
        ]"
        :data-level="config.name.toLowerCase()"
      >
        <!-- Icon -->
        <img
          :src="config.icon"
          :alt="config.name"
          class="filter-icon"
        />

        <!-- Label -->
        <span class="filter-label">
          {{ config.name }}
        </span>

        <!-- Счетчик -->
        <span class="filter-count">
          {{ formatCount(getCount(config.name)) }}
        </span>
      </button>
    </div>

    <!-- Быстрые действия -->
    <div class="actions-group">
      <button
        :disabled="!hasActiveTab"
        @click="clearAll"
        :class="['action-btn', !hasActiveTab && 'action-btn-disabled']"
      >
        Select All
      </button>
      <button
        :disabled="!hasActiveTab"
        @click="disableAll"
        :class="['action-btn', !hasActiveTab && 'action-btn-disabled']"
      >
        Clear All
      </button>
    </div>
  </div>
</template>

<style scoped>
/* Import IBM Plex Mono for technical data */
@import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&display=swap');

.filter-panel {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 10px 16px;
  background: linear-gradient(to bottom, #fafafa 0%, #f5f5f5 100%);
  border-bottom: 1px solid #e5e5e5;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
}

.panel-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-right: 16px;
  border-right: 1px solid #d4d4d4;
}

.header-label {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.5px;
  color: #737373;
  text-transform: uppercase;
}

.inactive-badge {
  padding: 2px 8px;
  background: #fef3c7;
  border: 1px solid #fbbf24;
  border-radius: 10px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px;
  font-weight: 500;
  color: #92400e;
}

.filters-group {
  display: flex;
  align-items: center;
  gap: 6px;
  flex: 1;
}

.filter-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 5px 12px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

.filter-btn:hover:not(.filter-btn-disabled):not(.filter-btn-active) {
  background: #f5f5f5;
  border-color: #a3a3a3;
}

.filter-btn-disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

/* Active states for each level - exact LogTable row colors */
.filter-btn-active[data-level="trace"] {
  background: #D3D3D3;
  border-color: #b0b0b0;
  color: #000000;
}

.filter-btn-active[data-level="debug"] {
  background: #ADD8E6;
  border-color: #8ec8dc;
  color: #000000;
}

.filter-btn-active[data-level="info"] {
  background: #ffffff;
  border-color: #94a3b8;
  color: #000000;
}

.filter-btn-active[data-level="warn"] {
  background: #FFFF00;
  border-color: #d4d400;
  color: #000000;
}

.filter-btn-active[data-level="error"] {
  background: #FF0000;
  border-color: #cc0000;
  color: #ffffff;
}

.filter-btn-active[data-level="fatal"] {
  background: #8B0000;
  border-color: #600000;
  color: #ffffff;
}

.filter-icon {
  width: 14px;
  height: 14px;
  opacity: 0.8;
}

.filter-btn-active .filter-icon {
  opacity: 1;
}

.filter-label {
  font-weight: 500;
}

.filter-count {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  padding: 1px 6px;
  background: rgba(0, 0, 0, 0.05);
  border-radius: 3px;
  color: #737373;
  min-width: 28px;
  text-align: center;
}

.filter-btn-active .filter-count {
  background: rgba(0, 0, 0, 0.08);
  color: currentColor;
  font-weight: 600;
}

.actions-group {
  display: flex;
  align-items: center;
  gap: 6px;
  padding-left: 16px;
  border-left: 1px solid #d4d4d4;
  margin-left: auto;
}

.action-btn {
  padding: 5px 12px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.action-btn:hover:not(.action-btn-disabled) {
  background: #fafafa;
  border-color: #a3a3a3;
  color: #171717;
  box-shadow: 0 2px 3px rgba(0, 0, 0, 0.06);
}

.action-btn:active:not(.action-btn-disabled) {
  transform: scale(0.98);
}

.action-btn-disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

/* Smooth animations */
@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(-2px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.filter-btn {
  animation: fadeIn 0.2s ease-out backwards;
}

.filter-btn:nth-child(1) { animation-delay: 0ms; }
.filter-btn:nth-child(2) { animation-delay: 30ms; }
.filter-btn:nth-child(3) { animation-delay: 60ms; }
.filter-btn:nth-child(4) { animation-delay: 90ms; }
.filter-btn:nth-child(5) { animation-delay: 120ms; }
.filter-btn:nth-child(6) { animation-delay: 150ms; }
</style>
