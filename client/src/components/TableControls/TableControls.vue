<script setup lang="ts">
import { computed } from 'vue'
import { ArrowDown, Trash2, Pause, Play } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { useTabsStore } from '@/stores'

const tabsStore = useTabsStore()

const emit = defineEmits<{
  (e: 'clear'): void
}>()

const activeTab = computed(() => tabsStore.activeTab)

const isAutoscroll = computed(() => activeTab.value?.autoscroll || false)
const isPaused = computed(() => activeTab.value?.isPaused || false)

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
</script>

<template>
  <div class="flex items-center justify-between border-b border-zinc-800 bg-zinc-950 px-3 py-2">
    <div class="flex items-center gap-2">
      <!-- Autoscroll Toggle -->
      <Button
        variant="ghost"
        size="sm"
        :class="{
          'bg-emerald-950/50 text-emerald-400 hover:bg-emerald-950 hover:text-emerald-300': isAutoscroll,
          'text-zinc-500 hover:bg-zinc-800 hover:text-zinc-300': !isAutoscroll
        }"
        class="gap-2 font-mono text-xs transition-all"
        @click="toggleAutoscroll"
      >
        <ArrowDown
          class="h-3.5 w-3.5 transition-transform"
          :class="{ 'animate-pulse': isAutoscroll }"
        />
        Автопрокрутка
      </Button>

      <!-- Pause/Resume Toggle -->
      <Button
        variant="ghost"
        size="sm"
        :class="{
          'bg-yellow-950/50 text-yellow-400 hover:bg-yellow-950 hover:text-yellow-300': isPaused,
          'text-zinc-500 hover:bg-zinc-800 hover:text-zinc-300': !isPaused
        }"
        class="gap-2 font-mono text-xs transition-all"
        @click="togglePause"
      >
        <Pause v-if="!isPaused" class="h-3.5 w-3.5" />
        <Play v-else class="h-3.5 w-3.5 animate-pulse" />
        {{ isPaused ? 'Продолжить' : 'Пауза' }}
      </Button>

      <!-- Clear Button -->
      <Button
        variant="ghost"
        size="sm"
        class="gap-2 font-mono text-xs text-zinc-500 hover:bg-zinc-800 hover:text-red-400"
        @click="handleClear"
      >
        <Trash2 class="h-3.5 w-3.5" />
        Очистить
      </Button>
    </div>
  </div>
</template>

<style scoped>
/* Button active states */
button[class*="bg-emerald"] {
  box-shadow: 0 0 20px rgba(16, 185, 129, 0.15);
}

button[class*="bg-yellow"] {
  box-shadow: 0 0 20px rgba(234, 179, 8, 0.15);
}
</style>
