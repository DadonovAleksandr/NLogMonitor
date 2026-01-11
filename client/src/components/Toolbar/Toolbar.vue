<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Search, X } from 'lucide-vue-next'
import { Input } from '@/components/ui/input'
import { useTabsStore } from '@/stores'
import type { LevelCounts } from '@/types'

interface LevelFilterButton {
  level: keyof LevelCounts
  label: string
  icon: string
  colorClass: string
  activeClass: string
}

const tabsStore = useTabsStore()

const searchText = ref('')
const activeLevels = ref<Set<string>>(new Set(['Trace', 'Debug', 'Info', 'Warn', 'Error', 'Fatal']))

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

const totalFiltered = computed(() => {
  let total = 0
  activeLevels.value.forEach(level => {
    const count = levelCounts.value[level as keyof LevelCounts]
    if (count) total += count
  })
  return total
})

function toggleLevel(level: string) {
  if (activeLevels.value.has(level)) {
    activeLevels.value.delete(level)
  } else {
    activeLevels.value.add(level)
  }
  // Force reactivity
  activeLevels.value = new Set(activeLevels.value)
}

function isLevelActive(level: string) {
  return activeLevels.value.has(level)
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

watch(activeLevels, (newValue) => {
  emit('filter-levels', newValue)
}, { deep: true })
</script>

<template>
  <div class="flex flex-col gap-3 border-b border-zinc-800 bg-zinc-950 p-3">
    <!-- Search Bar -->
    <div class="relative">
      <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500" />
      <Input
        v-model="searchText"
        type="text"
        placeholder="Поиск по сообщениям..."
        class="border-zinc-800 bg-zinc-900 pl-10 pr-10 font-mono text-sm placeholder:text-zinc-600 focus-visible:border-emerald-700 focus-visible:ring-emerald-900/50"
      />
      <button
        v-if="searchText"
        class="absolute right-3 top-1/2 flex h-5 w-5 -translate-y-1/2 items-center justify-center rounded transition-colors hover:bg-zinc-700"
        @click="clearSearch"
      >
        <X class="h-3.5 w-3.5 text-zinc-500" />
      </button>
    </div>

    <!-- Level Filters -->
    <div class="flex items-center gap-2">
      <span class="font-mono text-xs text-zinc-500">Фильтры:</span>

      <div class="flex flex-wrap gap-2">
        <button
          v-for="btn in levelButtons"
          :key="btn.level"
          class="group relative flex items-center gap-2 border px-3 py-1.5 font-mono text-xs font-medium transition-all duration-150"
          :class="[
            isLevelActive(btn.level) ? btn.activeClass : btn.colorClass
          ]"
          @click="toggleLevel(btn.level)"
        >
          <img
            :src="btn.icon"
            :alt="btn.label"
            class="h-4 w-4 transition-opacity"
            :class="[isLevelActive(btn.level) ? 'opacity-90' : 'opacity-60']"
          />
          <span>{{ btn.label }}</span>
          <span class="ml-2 opacity-70">{{ (levelCounts[btn.level] ?? 0).toLocaleString() }}</span>

          <!-- Glowing effect on active -->
          <div
            v-if="isLevelActive(btn.level)"
            class="absolute inset-0 -z-10 blur-md transition-opacity duration-150"
            :class="btn.activeClass"
          />
        </button>
      </div>

      <!-- Divider -->
      <div class="mx-2 h-6 w-px bg-zinc-700" />

      <!-- Total Count -->
      <div class="flex items-center gap-2 font-mono text-sm">
        <span class="text-zinc-500">Показано:</span>
        <span class="font-semibold text-emerald-400">{{ totalFiltered.toLocaleString() }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Smooth transitions for button states */
button {
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

button:active {
  transform: scale(0.98);
}
</style>
