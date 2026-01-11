<script setup lang="ts">
import { computed } from 'vue'
import { useFilterStore } from '@/stores/filterStore'
import { useLogStore } from '@/stores/logStore'
import { LogLevel } from '@/types'

const filterStore = useFilterStore()
const logStore = useLogStore()

// Конфигурация уровней с цветами из LogLevelBadge
interface LevelConfig {
  level: LogLevel
  name: string
  label: string
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
  <div class="flex items-center gap-1.5 p-3 bg-slate-50 border border-slate-200 shadow-sm">
    <!-- Заголовок панели -->
    <div class="flex items-center gap-2 pr-3 border-r border-slate-300">
      <div class="font-mono text-[10px] tracking-widest text-slate-600 uppercase font-bold">
        LEVEL
      </div>
      <div
        v-if="inactiveCount > 0"
        class="flex items-center justify-center min-w-[20px] h-4 px-1.5 bg-amber-100 border border-amber-400 font-mono text-[10px] text-amber-800 font-semibold"
      >
        -{{ inactiveCount }}
      </div>
    </div>

    <!-- Кнопки фильтров -->
    <div class="flex items-center gap-1">
      <button
        v-for="config in levels"
        :key="config.level"
        @click="handleToggle(config.level)"
        :class="[
          'group relative',
          'flex items-center gap-2',
          'px-3 py-1.5',
          'border-2 transition-all duration-150',
          'hover:scale-[1.02] active:scale-[0.98]',
          'font-mono text-[11px] tracking-wider uppercase font-semibold',
          config.text,
          isActive(config.level)
            ? [config.bg, config.border, config.glowActive]
            : ['bg-white border-slate-200 opacity-50 hover:opacity-75 hover:border-slate-300', config.glow]
        ]"
      >
        <!-- Label -->
        <span class="font-bold">
          {{ config.label }}
        </span>

        <!-- Разделитель -->
        <span
          :class="[
            'w-px h-3 transition-opacity',
            isActive(config.level) ? 'opacity-30' : 'opacity-20'
          ]"
          :style="{ backgroundColor: 'currentColor' }"
        />

        <!-- Счетчик -->
        <span
          :class="[
            'tabular-nums text-[10px] tracking-wide min-w-[3ch] text-right inline-block',
            isActive(config.level) ? 'opacity-90' : 'opacity-50'
          ]"
        >
          {{ formatCount(getCount(config.name)) }}
        </span>

        <!-- Индикатор активности (subtle underline) -->
        <div
          v-if="isActive(config.level)"
          :class="[
            'absolute bottom-0 left-0 right-0 h-[2px]',
            'transition-all duration-200',
            config.text.replace('text-', 'bg-'),
            'opacity-60'
          ]"
        />
      </button>
    </div>

    <!-- Быстрые действия -->
    <div class="flex items-center gap-1 pl-3 ml-auto border-l border-slate-300">
      <button
        @click="clearAll"
        class="px-2.5 py-1 border-2 border-slate-300 bg-white hover:bg-slate-100 hover:border-slate-400 font-mono text-[10px] text-slate-700 hover:text-slate-900 tracking-wider uppercase font-semibold transition-all active:scale-95 shadow-sm hover:shadow"
      >
        ALL
      </button>
      <button
        @click="disableAll"
        class="px-2.5 py-1 border-2 border-slate-300 bg-white hover:bg-slate-100 hover:border-slate-400 font-mono text-[10px] text-slate-700 hover:text-slate-900 tracking-wider uppercase font-semibold transition-all active:scale-95 shadow-sm hover:shadow"
      >
        NONE
      </button>
    </div>
  </div>
</template>
