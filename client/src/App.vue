<script setup lang="ts">
import { computed, watch } from 'vue'
import { FileSelector } from '@/components/FileSelector'
import { LogTable } from '@/components/LogTable'
import { Pagination } from '@/components/Pagination'
import { RecentFiles } from '@/components/RecentFiles'
import { ExportButton } from '@/components/ExportButton'
import { FilterPanel } from '@/components/FilterPanel'
import { SearchBar } from '@/components/SearchBar'
import { Toast } from '@/components/Toast'
import { LiveIndicator } from '@/components/LiveIndicator'
import { useLogStore, useFilterStore } from '@/stores'
import { useFileWatcher } from '@/composables/useFileWatcher'

const logStore = useLogStore()
const filterStore = useFilterStore()

// SignalR file watcher для real-time обновлений
const { connectionState, isWatching, startWatching, stopWatching } = useFileWatcher()

const showFileSelector = computed(() => !logStore.hasSession && !logStore.isLoading)
const showLogTable = computed(() => logStore.hasSession || logStore.isLoading)

// Автоматическая перезагрузка логов при изменении фильтров
watch(
  () => filterStore.filterOptions,
  async () => {
    if (logStore.hasSession) {
      // Сбрасываем страницу на первую при изменении фильтров
      logStore.setPage(1)
      await logStore.fetchLogs(filterStore.filterOptions)
    }
  },
  { deep: true }
)

// Автоматическая перезагрузка при изменении страницы или размера
watch(
  [() => logStore.page, () => logStore.pageSize],
  async () => {
    if (logStore.hasSession) {
      await logStore.fetchLogs(filterStore.filterOptions)
    }
  }
)

// Отслеживание изменений sessionId для управления SignalR подпиской
watch(
  () => logStore.sessionId,
  async (newSessionId, oldSessionId) => {
    // Если была старая сессия, останавливаем отслеживание
    if (oldSessionId && oldSessionId !== newSessionId) {
      await stopWatching()
    }

    // Если появилась новая сессия, начинаем отслеживание
    if (newSessionId) {
      // Сбрасываем фильтры при открытии нового файла
      filterStore.clearFilters()

      try {
        await startWatching(newSessionId, (newLogs) => {
          // Callback для обработки новых логов
          logStore.appendLogs(newLogs)
        })
      } catch (err) {
        console.error('Failed to start watching session:', err)
      }
    }
  }
)
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100">
    <!-- Header -->
    <header class="border-b border-zinc-800 bg-zinc-900/50 backdrop-blur-sm">
      <div class="mx-auto flex h-14 max-w-7xl items-center justify-between px-4">
        <div class="flex items-center gap-3">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-600">
            <span class="font-mono text-sm font-bold text-white">N</span>
          </div>
          <h1 class="font-mono text-lg font-semibold tracking-tight">
            nLogMonitor
          </h1>
        </div>

        <!-- Session info -->
        <div v-if="logStore.hasSession" class="flex items-center gap-4">
          <!-- Live Indicator -->
          <LiveIndicator :connection-state="connectionState" :is-watching="isWatching" />

          <div class="h-4 w-px bg-zinc-700" />

          <div class="flex items-center gap-2">
            <span class="font-mono text-xs text-zinc-500">File:</span>
            <span class="font-mono text-sm text-zinc-300">{{ logStore.fileName }}</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="font-mono text-xs text-zinc-500">Entries:</span>
            <span class="font-mono text-sm text-emerald-400">{{ logStore.totalCount.toLocaleString() }}</span>
          </div>
          <ExportButton />
          <button
            class="rounded-md px-3 py-1.5 font-mono text-xs text-zinc-400 transition-colors hover:bg-zinc-800 hover:text-zinc-200"
            @click="logStore.clearSession()"
          >
            Close
          </button>
        </div>
      </div>
    </header>

    <!-- Main content -->
    <main class="mx-auto max-w-7xl px-4 py-6">
      <!-- File Selector (when no session) -->
      <div v-if="showFileSelector" class="flex min-h-[60vh] gap-6">
        <!-- File Selector - Main area -->
        <div class="flex flex-1 items-center justify-center">
          <div class="w-full max-w-xl">
            <FileSelector />
          </div>
        </div>

        <!-- Recent Files - Sidebar -->
        <div class="w-80 flex-shrink-0">
          <RecentFiles />
        </div>
      </div>

      <!-- Log Table (when session active) -->
      <div v-if="showLogTable" class="flex h-[calc(100vh-8rem)] flex-col gap-3">
        <!-- Search and Filters -->
        <div v-if="logStore.hasSession" class="flex flex-col gap-2">
          <!-- Search Bar -->
          <SearchBar />

          <!-- Filter Panel -->
          <FilterPanel />
        </div>

        <!-- Table -->
        <div class="flex-1 min-h-0">
          <LogTable />
        </div>

        <!-- Pagination -->
        <Pagination v-if="logStore.hasSession" />
      </div>
    </main>

    <!-- Footer -->
    <footer class="fixed bottom-0 left-0 right-0 border-t border-zinc-800 bg-zinc-900/80 backdrop-blur-sm">
      <div class="mx-auto flex h-8 max-w-7xl items-center justify-between px-4">
        <span class="font-mono text-xs text-zinc-600">nLogMonitor v0.5.0</span>
        <span class="font-mono text-xs text-zinc-600">Phase 5 - UI Components ✓</span>
      </div>
    </footer>

    <!-- Toast notifications -->
    <Toast />
  </div>
</template>
