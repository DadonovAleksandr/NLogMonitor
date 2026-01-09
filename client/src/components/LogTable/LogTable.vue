<script setup lang="ts">
import { computed, h } from 'vue'
import {
  useVueTable,
  getCoreRowModel,
  createColumnHelper,
  FlexRender
} from '@tanstack/vue-table'
import { FileText, Loader2, AlertCircle } from 'lucide-vue-next'
import type { LogEntry } from '@/types'
import { useLogStore } from '@/stores'
import LogLevelBadge from './LogLevelBadge.vue'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table'

const logStore = useLogStore()

// Format timestamp to HH:mm:ss.fff
function formatTime(isoString: string): string {
  try {
    const date = new Date(isoString)
    const hours = date.getHours().toString().padStart(2, '0')
    const minutes = date.getMinutes().toString().padStart(2, '0')
    const seconds = date.getSeconds().toString().padStart(2, '0')
    const ms = date.getMilliseconds().toString().padStart(3, '0')
    return `${hours}:${minutes}:${seconds}.${ms}`
  } catch {
    return isoString
  }
}

// Column helper
const columnHelper = createColumnHelper<LogEntry>()

const columns = [
  columnHelper.accessor('timestamp', {
    header: 'Time',
    cell: (info) => formatTime(info.getValue()),
    size: 120
  }),
  columnHelper.accessor('level', {
    header: 'Level',
    cell: (info) => h(LogLevelBadge, { level: info.getValue() }),
    size: 70
  }),
  columnHelper.accessor('message', {
    header: 'Message',
    cell: (info) => info.getValue(),
    size: undefined // flex
  }),
  columnHelper.accessor('logger', {
    header: 'Logger',
    cell: (info) => {
      const logger = info.getValue()
      // Shorten logger name: MyApp.Services.UserService -> M.S.UserService
      const parts = logger.split('.')
      if (parts.length <= 2) return logger
      const shortened = parts.slice(0, -1).map(p => p[0]).join('.') + '.' + parts[parts.length - 1]
      return shortened
    },
    size: 180
  })
]

const table = useVueTable({
  get data() {
    return logStore.logs
  },
  columns,
  getCoreRowModel: getCoreRowModel()
})

const showEmpty = computed(() => !logStore.isLoading && !logStore.hasLogs)
const showLoading = computed(() => logStore.isLoading)
const showData = computed(() => !logStore.isLoading && logStore.hasLogs)
</script>

<template>
  <div class="log-table-container relative flex-1 overflow-hidden rounded-lg border border-zinc-800 bg-zinc-950">
    <!-- Scanline overlay for terminal effect -->
    <div class="pointer-events-none absolute inset-0 z-10 bg-[repeating-linear-gradient(0deg,transparent,transparent_2px,rgba(0,0,0,0.03)_2px,rgba(0,0,0,0.03)_4px)]" />

    <!-- Loading State -->
    <div v-if="showLoading" class="flex h-full flex-col">
      <!-- Header skeleton -->
      <div class="flex items-center gap-4 border-b border-zinc-800 bg-zinc-900/50 px-4 py-3">
        <div class="h-4 w-20 animate-pulse rounded bg-zinc-800" />
        <div class="h-4 w-16 animate-pulse rounded bg-zinc-800" />
        <div class="h-4 flex-1 animate-pulse rounded bg-zinc-800" />
        <div class="h-4 w-32 animate-pulse rounded bg-zinc-800" />
      </div>
      <!-- Rows skeleton with shimmer -->
      <div class="flex-1 overflow-hidden">
        <div
          v-for="i in 12"
          :key="i"
          class="flex items-center gap-4 border-b border-zinc-800/50 px-4 py-2.5"
          :style="{ animationDelay: `${i * 50}ms` }"
        >
          <div class="h-3.5 w-20 animate-pulse rounded bg-zinc-800/80" />
          <div class="h-5 w-12 animate-pulse rounded bg-zinc-800/80" />
          <div
            class="h-3.5 flex-1 animate-pulse rounded bg-zinc-800/80"
            :style="{ maxWidth: `${60 + Math.random() * 35}%` }"
          />
          <div class="h-3.5 w-28 animate-pulse rounded bg-zinc-800/80" />
        </div>
      </div>
      <!-- Loading indicator -->
      <div class="absolute inset-0 flex items-center justify-center bg-zinc-950/60">
        <div class="flex items-center gap-3 rounded-lg border border-zinc-700 bg-zinc-900 px-5 py-3 shadow-2xl">
          <Loader2 class="h-5 w-5 animate-spin text-emerald-500" />
          <span class="font-mono text-sm text-zinc-300">Загрузка логов...</span>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="showEmpty" class="flex h-full flex-col items-center justify-center gap-4 p-8">
      <div class="relative">
        <div class="absolute -inset-4 animate-pulse rounded-full bg-zinc-800/30" />
        <div class="relative rounded-xl border border-zinc-700 bg-zinc-900 p-6">
          <FileText class="h-12 w-12 text-zinc-600" stroke-width="1.5" />
        </div>
      </div>
      <div class="text-center">
        <h3 class="font-mono text-lg font-medium text-zinc-400">
          {{ logStore.hasSession ? 'Нет записей' : 'Загрузите файл' }}
        </h3>
        <p class="mt-1 font-mono text-sm text-zinc-600">
          {{ logStore.hasSession ? 'Попробуйте изменить фильтры' : 'Выберите .log или .txt файл для анализа' }}
        </p>
      </div>
      <div v-if="logStore.hasError" class="mt-2 flex items-center gap-2 rounded-lg border border-red-900/50 bg-red-950/30 px-4 py-2">
        <AlertCircle class="h-4 w-4 text-red-500" />
        <span class="font-mono text-sm text-red-400">{{ logStore.error }}</span>
      </div>
    </div>

    <!-- Data Table -->
    <div v-else-if="showData" class="flex h-full flex-col overflow-hidden">
      <Table class="log-table">
        <TableHeader class="sticky top-0 z-20 bg-zinc-900/95 backdrop-blur-sm">
          <TableRow
            v-for="headerGroup in table.getHeaderGroups()"
            :key="headerGroup.id"
            class="border-b-2 border-zinc-700 hover:bg-transparent"
          >
            <TableHead
              v-for="header in headerGroup.headers"
              :key="header.id"
              class="h-10 px-3 font-mono text-xs font-semibold uppercase tracking-wider text-zinc-400"
              :style="header.column.getSize() ? { width: `${header.column.getSize()}px` } : {}"
            >
              <FlexRender
                v-if="!header.isPlaceholder"
                :render="header.column.columnDef.header"
                :props="header.getContext()"
              />
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody class="overflow-auto">
          <TableRow
            v-for="row in table.getRowModel().rows"
            :key="row.id"
            class="group border-b border-zinc-800/50 transition-colors hover:bg-zinc-800/30"
            :class="{
              'bg-red-950/20 hover:bg-red-950/30': row.original.level === 'Error' || row.original.level === 'Fatal',
              'bg-amber-950/10 hover:bg-amber-950/20': row.original.level === 'Warn'
            }"
          >
            <TableCell
              v-for="cell in row.getVisibleCells()"
              :key="cell.id"
              class="px-3 py-2 align-top"
              :class="{
                'font-mono text-xs text-zinc-500': cell.column.id === 'timestamp' || cell.column.id === 'logger',
                'text-sm text-zinc-300': cell.column.id === 'message'
              }"
              :style="cell.column.getSize() ? { width: `${cell.column.getSize()}px` } : {}"
            >
              <div
                v-if="cell.column.id === 'message'"
                class="line-clamp-2 max-w-full break-all group-hover:line-clamp-none"
                :title="row.original.message"
              >
                <FlexRender :render="cell.column.columnDef.cell" :props="cell.getContext()" />
              </div>
              <div
                v-else-if="cell.column.id === 'logger'"
                class="truncate"
                :title="row.original.logger"
              >
                <FlexRender :render="cell.column.columnDef.cell" :props="cell.getContext()" />
              </div>
              <template v-else>
                <FlexRender :render="cell.column.columnDef.cell" :props="cell.getContext()" />
              </template>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>
  </div>
</template>

<style scoped>
.log-table-container {
  font-family: 'JetBrains Mono', 'Fira Code', 'SF Mono', Consolas, monospace;
}

.log-table {
  table-layout: fixed;
  width: 100%;
}

/* Custom scrollbar for terminal feel */
.log-table-container ::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.log-table-container ::-webkit-scrollbar-track {
  background: hsl(var(--background));
}

.log-table-container ::-webkit-scrollbar-thumb {
  background: hsl(var(--muted));
  border-radius: 4px;
}

.log-table-container ::-webkit-scrollbar-thumb:hover {
  background: hsl(var(--muted-foreground));
}
</style>
