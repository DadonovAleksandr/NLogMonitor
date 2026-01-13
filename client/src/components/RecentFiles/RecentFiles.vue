<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { File, Folder, Clock, Loader2, FileX } from 'lucide-vue-next'
import { useRecentStore, useLogStore, useTabsStore } from '@/stores'
import type { RecentLog } from '@/types'

const recentStore = useRecentStore()
const logStore = useLogStore()
const tabsStore = useTabsStore()

// Format timestamp
function formatTimestamp(isoString: string): string {
  try {
    const date = new Date(isoString)
    const now = new Date()
    const diff = now.getTime() - date.getTime()

    // Less than 1 minute
    if (diff < 60 * 1000) {
      return 'только что'
    }

    // Less than 1 hour
    if (diff < 60 * 60 * 1000) {
      const minutes = Math.floor(diff / (60 * 1000))
      return `${minutes} мин назад`
    }

    // Less than 24 hours
    if (diff < 24 * 60 * 60 * 1000) {
      const hours = Math.floor(diff / (60 * 60 * 1000))
      return `${hours} ч назад`
    }

    // Format as DD.MM.YYYY HH:mm
    const day = date.getDate().toString().padStart(2, '0')
    const month = (date.getMonth() + 1).toString().padStart(2, '0')
    const year = date.getFullYear()
    const hours = date.getHours().toString().padStart(2, '0')
    const minutes = date.getMinutes().toString().padStart(2, '0')

    return `${day}.${month}.${year} ${hours}:${minutes}`
  } catch {
    return isoString
  }
}

// Handle click on recent item
async function handleItemClick(item: RecentLog) {
  try {
    if (item.isDirectory) {
      await logStore.openDirectory(item.path)
    } else {
      await logStore.openFile(item.path)
    }
  } catch {
    // Error is already set in logStore
  }
}

const hasItems = computed(() => recentStore.recentLogs.length > 0)
const showEmpty = computed(() => !recentStore.isLoading && !hasItems.value)
const showLoading = computed(() => recentStore.isLoading)
const showData = computed(() => !recentStore.isLoading && hasItems.value)

// Fetch recent logs on mount
onMounted(() => {
  recentStore.fetchRecent()
})
</script>

<template>
  <div class="recent-files flex flex-col gap-3">
    <!-- Header -->
    <div class="flex items-center justify-between border-b border-zinc-800 pb-2">
      <div class="flex items-center gap-2">
        <Clock class="h-4 w-4 text-zinc-500" />
        <h2 class="font-mono text-sm font-medium uppercase tracking-wider text-zinc-400">
          Недавние файлы
        </h2>
      </div>
      <div class="font-mono text-xs text-zinc-600">
        {{ recentStore.recentLogs.length }}
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="showLoading" class="flex flex-col gap-2">
      <div
        v-for="i in 3"
        :key="i"
        class="flex items-center gap-3 rounded-lg border border-zinc-800 bg-zinc-900/30 px-3 py-2.5"
      >
        <div class="h-4 w-4 animate-pulse rounded bg-zinc-800" />
        <div class="flex-1">
          <div class="h-3.5 w-3/4 animate-pulse rounded bg-zinc-800" />
          <div class="mt-1.5 h-2.5 w-1/2 animate-pulse rounded bg-zinc-800/50" />
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="showEmpty"
      class="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-zinc-800 bg-zinc-900/20 px-6 py-8 text-center"
    >
      <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-zinc-900 text-zinc-600">
        <FileX class="h-6 w-6" />
      </div>
      <div>
        <p class="font-mono text-sm font-medium text-zinc-500">
          Нет недавних файлов
        </p>
        <p class="mt-1 font-mono text-xs text-zinc-600">
          Откройте файл или директорию
        </p>
      </div>
    </div>

    <!-- Recent Items List -->
    <div v-else-if="showData" class="flex flex-col gap-1.5">
      <button
        v-for="item in recentStore.recentLogs"
        :key="item.path"
        type="button"
        class="group relative flex items-start gap-3 rounded-lg border border-zinc-800 bg-zinc-900/30 px-3 py-2.5 text-left transition-all hover:border-zinc-700 hover:bg-zinc-900/60 active:scale-[0.99]"
        :disabled="tabsStore.isLoading"
        @click="handleItemClick(item)"
      >
        <!-- Hover glow effect -->
        <div
          class="pointer-events-none absolute inset-0 rounded-lg opacity-0 transition-opacity group-hover:opacity-100"
        >
          <div
            class="absolute inset-0 rounded-lg bg-gradient-to-br from-zinc-700/5 to-zinc-600/5"
          />
        </div>

        <!-- Icon -->
        <div class="relative mt-0.5 flex-shrink-0">
          <Folder
            v-if="item.isDirectory"
            class="h-4 w-4 text-sky-500/80 transition-colors group-hover:text-sky-400"
          />
          <File
            v-else
            class="h-4 w-4 text-emerald-500/80 transition-colors group-hover:text-emerald-400"
          />
        </div>

        <!-- Content -->
        <div class="relative min-w-0 flex-1">
          <!-- Display name -->
          <div
            class="truncate font-mono text-sm font-medium text-zinc-300 transition-colors group-hover:text-zinc-200"
          >
            {{ item.displayName }}
          </div>
          <!-- Path and timestamp -->
          <div class="mt-0.5 flex items-center gap-2 font-mono text-xs text-zinc-600">
            <span class="truncate">{{ item.path }}</span>
            <span class="flex-shrink-0">•</span>
            <span class="flex-shrink-0">{{ formatTimestamp(item.openedAt) }}</span>
          </div>
        </div>

        <!-- Loading indicator when item is being opened -->
        <div
          v-if="tabsStore.isLoading && tabsStore.filePath === item.path"
          class="relative flex-shrink-0"
        >
          <Loader2 class="h-4 w-4 animate-spin text-zinc-500" />
        </div>
      </button>
    </div>

    <!-- Error message -->
    <div
      v-if="recentStore.error"
      class="rounded-lg border border-red-900/50 bg-red-950/30 px-3 py-2 font-mono text-xs text-red-400"
    >
      {{ recentStore.error }}
    </div>
  </div>
</template>

<style scoped>
.recent-files {
  font-family: 'JetBrains Mono', 'Fira Code', 'SF Mono', Consolas, monospace;
}
</style>
