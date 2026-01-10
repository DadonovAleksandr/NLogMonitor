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
    bg: 'bg-zinc-900/40',
    text: 'text-zinc-400',
    border: 'border-zinc-700/50',
    glow: '',
    glowActive: 'shadow-[0_0_8px_rgba(161,161,170,0.15)]'
  },
  {
    level: LogLevel.Debug,
    name: 'Debug',
    label: 'DBG',
    bg: 'bg-sky-950/40',
    text: 'text-sky-400',
    border: 'border-sky-800/50',
    glow: 'shadow-[0_0_6px_rgba(56,189,248,0.1)]',
    glowActive: 'shadow-[0_0_12px_rgba(56,189,248,0.25)]'
  },
  {
    level: LogLevel.Info,
    name: 'Info',
    label: 'INF',
    bg: 'bg-emerald-950/40',
    text: 'text-emerald-400',
    border: 'border-emerald-800/50',
    glow: 'shadow-[0_0_6px_rgba(52,211,153,0.1)]',
    glowActive: 'shadow-[0_0_12px_rgba(52,211,153,0.25)]'
  },
  {
    level: LogLevel.Warn,
    name: 'Warn',
    label: 'WRN',
    bg: 'bg-amber-950/40',
    text: 'text-amber-400',
    border: 'border-amber-800/50',
    glow: 'shadow-[0_0_6px_rgba(251,191,36,0.12)]',
    glowActive: 'shadow-[0_0_12px_rgba(251,191,36,0.3)]'
  },
  {
    level: LogLevel.Error,
    name: 'Error',
    label: 'ERR',
    bg: 'bg-red-950/50',
    text: 'text-red-400',
    border: 'border-red-800/60',
    glow: 'shadow-[0_0_6px_rgba(248,113,113,0.15)]',
    glowActive: 'shadow-[0_0_14px_rgba(248,113,113,0.35)]'
  },
  {
    level: LogLevel.Fatal,
    name: 'Fatal',
    label: 'FTL',
    bg: 'bg-fuchsia-950/50',
    text: 'text-fuchsia-300',
    border: 'border-fuchsia-800/60',
    glow: 'shadow-[0_0_8px_rgba(232,121,249,0.2)]',
    glowActive: 'shadow-[0_0_16px_rgba(232,121,249,0.4)]'
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
  <div class="flex items-center gap-1.5 p-3 bg-zinc-950/80 border border-zinc-800/60 backdrop-blur-sm">
    <!-- Заголовок панели -->
    <div class="flex items-center gap-2 pr-3 border-r border-zinc-700/50">
      <div class="font-mono text-[10px] tracking-widest text-zinc-500 uppercase">
        LEVEL
      </div>
      <div
        v-if="inactiveCount > 0"
        class="flex items-center justify-center min-w-[20px] h-4 px-1.5 bg-amber-950/60 border border-amber-700/50 font-mono text-[10px] text-amber-400"
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
          'border transition-all duration-150',
          'hover:scale-[1.02] active:scale-[0.98]',
          'font-mono text-[11px] tracking-wider uppercase',
          config.text,
          isActive(config.level)
            ? [config.bg, config.border, config.glowActive, 'border-opacity-100']
            : ['bg-transparent border-zinc-800/40 opacity-40 hover:opacity-60', config.glow]
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
    <div class="flex items-center gap-1 pl-3 ml-auto border-l border-zinc-700/50">
      <button
        @click="clearAll"
        class="px-2.5 py-1 border border-zinc-700/50 bg-zinc-900/40 hover:bg-zinc-800/60 font-mono text-[10px] text-zinc-400 hover:text-zinc-300 tracking-wider uppercase transition-all active:scale-95"
      >
        ALL
      </button>
      <button
        @click="disableAll"
        class="px-2.5 py-1 border border-zinc-700/50 bg-zinc-900/40 hover:bg-zinc-800/60 font-mono text-[10px] text-zinc-400 hover:text-zinc-300 tracking-wider uppercase transition-all active:scale-95"
      >
        NONE
      </button>
    </div>
  </div>
</template>
