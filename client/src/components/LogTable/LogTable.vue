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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table'

const logStore = useLogStore()

// Format timestamp to DD.MM.YY HH:mm:ss.fff
function formatTimestamp(isoString: string): string {
  try {
    const date = new Date(isoString)
    const day = date.getDate().toString().padStart(2, '0')
    const month = (date.getMonth() + 1).toString().padStart(2, '0')
    const year = date.getFullYear().toString().slice(-2)
    const hours = date.getHours().toString().padStart(2, '0')
    const minutes = date.getMinutes().toString().padStart(2, '0')
    const seconds = date.getSeconds().toString().padStart(2, '0')
    const ms = date.getMilliseconds().toString().padStart(3, '0')
    return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}.${ms}`
  } catch {
    return isoString
  }
}

// Column helper
const columnHelper = createColumnHelper<LogEntry>()

const columns = [
  columnHelper.accessor('timestamp', {
    header: 'Дата и время',
    cell: (info) => formatTimestamp(info.getValue()),
    // Автоматическая ширина по содержимому
  }),
  columnHelper.accessor('level', {
    header: 'Важность',
    cell: (info) => info.getValue(),
    // Автоматическая ширина по содержимому
  }),
  columnHelper.accessor('message', {
    header: 'Описание',
    cell: (info) => info.getValue(),
    size: 9999 // Большое значение - займет все свободное пространство
  }),
  columnHelper.accessor('processId', {
    header: 'Процесс',
    cell: (info) => info.getValue(),
    // Автоматическая ширина по содержимому
  }),
  columnHelper.accessor('threadId', {
    header: 'Поток',
    cell: (info) => info.getValue(),
    // Автоматическая ширина по содержимому
  }),
  columnHelper.accessor('logger', {
    header: 'Источник',
    cell: (info) => {
      const logger = info.getValue()
      // Shorten logger name: MyApp.Services.UserService -> M.S.UserService
      const parts = logger.split('.')
      if (parts.length <= 2) return logger
      const shortened = parts.slice(0, -1).map(p => p[0]).join('.') + '.' + parts[parts.length - 1]
      return shortened
    },
    // Автоматическая ширина по содержимому
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
  <div class="log-table-container relative flex-1 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
    <!-- Subtle paper texture overlay -->
    <div class="pointer-events-none absolute inset-0 z-10 opacity-[0.015] bg-[repeating-linear-gradient(0deg,transparent,transparent_1px,rgba(0,0,0,0.02)_1px,rgba(0,0,0,0.02)_2px)]" />

    <!-- Loading State -->
    <div v-if="showLoading" class="flex h-full flex-col">
      <!-- Header skeleton -->
      <div class="flex items-center gap-4 border-b-2 border-slate-300 bg-slate-50 px-4 py-3">
        <div class="h-4 w-20 animate-pulse rounded bg-slate-200" />
        <div class="h-4 w-16 animate-pulse rounded bg-slate-200" />
        <div class="h-4 flex-1 animate-pulse rounded bg-slate-200" />
        <div class="h-4 w-32 animate-pulse rounded bg-slate-200" />
      </div>
      <!-- Rows skeleton with shimmer -->
      <div class="flex-1 overflow-hidden">
        <div
          v-for="i in 12"
          :key="i"
          class="flex items-center gap-4 border-b border-slate-100 px-4 py-2.5"
          :style="{ animationDelay: `${i * 50}ms` }"
        >
          <div class="h-3.5 w-20 animate-pulse rounded bg-slate-200/60" />
          <div class="h-5 w-12 animate-pulse rounded bg-slate-200/60" />
          <div
            class="h-3.5 flex-1 animate-pulse rounded bg-slate-200/60"
            :style="{ maxWidth: `${60 + Math.random() * 35}%` }"
          />
          <div class="h-3.5 w-28 animate-pulse rounded bg-slate-200/60" />
        </div>
      </div>
      <!-- Loading indicator -->
      <div class="absolute inset-0 flex items-center justify-center bg-white/80 backdrop-blur-sm">
        <div class="flex items-center gap-3 rounded-lg border-2 border-blue-500 bg-white px-5 py-3 shadow-xl">
          <Loader2 class="h-5 w-5 animate-spin text-blue-600" />
          <span class="font-mono text-sm font-medium text-slate-800">Загрузка логов...</span>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="showEmpty" class="flex h-full flex-col items-center justify-center gap-4 p-8">
      <div class="relative">
        <div class="absolute -inset-4 animate-pulse rounded-full bg-slate-200/50" />
        <div class="relative rounded-xl border-2 border-slate-300 bg-slate-50 p-6">
          <FileText class="h-12 w-12 text-slate-400" stroke-width="1.5" />
        </div>
      </div>
      <div class="text-center">
        <h3 class="font-mono text-lg font-semibold text-slate-700">
          {{ logStore.hasSession ? 'Нет записей' : 'Загрузите файл' }}
        </h3>
        <p class="mt-1 font-mono text-sm text-slate-500">
          {{ logStore.hasSession ? 'Попробуйте изменить фильтры' : 'Выберите .log или .txt файл для анализа' }}
        </p>
      </div>
      <div v-if="logStore.hasError" class="mt-2 flex items-center gap-2 rounded-lg border-2 border-red-400 bg-red-50 px-4 py-2">
        <AlertCircle class="h-4 w-4 text-red-600" />
        <span class="font-mono text-sm font-medium text-red-700">{{ logStore.error }}</span>
      </div>
    </div>

    <!-- Data Table -->
    <div v-else-if="showData" class="flex h-full min-h-0 flex-col">
      <!-- Scrollable container with proper height constraints -->
      <div class="flex-1 overflow-auto min-h-0">
        <Table class="log-table">
        <TableHeader class="sticky top-0 z-20 bg-slate-50 backdrop-blur-sm">
          <TableRow
            v-for="headerGroup in table.getHeaderGroups()"
            :key="headerGroup.id"
            class="border-b-2 border-slate-300 hover:bg-transparent"
          >
            <TableHead
              v-for="header in headerGroup.headers"
              :key="header.id"
              class="h-10 px-3 py-2 font-mono text-sm font-semibold text-slate-700"
              :class="{ 'text-center': header.column.id !== 'message' }"
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
        <TableBody>
          <TableRow
            v-for="row in table.getRowModel().rows"
            :key="row.id"
            class="group border-b transition-all duration-200"
            :class="{
              'log-row-trace': row.original.level === 'Trace',
              'log-row-debug': row.original.level === 'Debug',
              'log-row-info': row.original.level === 'Info',
              'log-row-warn': row.original.level === 'Warn',
              'log-row-error': row.original.level === 'Error',
              'log-row-fatal': row.original.level === 'Fatal'
            }"
          >
            <TableCell
              v-for="cell in row.getVisibleCells()"
              :key="cell.id"
              class="px-3 py-1 align-middle font-mono text-sm leading-tight"
              :class="{
                'text-center': cell.column.id !== 'message' && cell.column.id !== 'logger',
                'whitespace-nowrap': cell.column.id === 'processId' || cell.column.id === 'threadId' || cell.column.id === 'timestamp' || cell.column.id === 'level'
              }"
              :style="cell.column.getSize() ? { width: `${cell.column.getSize()}px` } : {}"
            >
              <div
                v-if="cell.column.id === 'message'"
                class="line-clamp-2 max-w-full break-all leading-tight group-hover:line-clamp-none"
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
  </div>
</template>

<style scoped>
.log-table-container {
  font-family: 'Segoe UI';
}

.log-table {
  table-layout: auto;
  width: 100%;
}

/* Log Level Row Styles - Classic nLogViewer Colors */
/* Exact colors from original WPF application */

/* Trace - LightGray (#D3D3D3) */
.log-row-trace {
  @apply border-slate-200;
  background-color: #D3D3D3;
  color: #000000;
}
.log-row-trace:hover {
  background-color: #c8c8c8;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

/* Debug - LightBlue (#ADD8E6) */
.log-row-debug {
  @apply border-blue-200;
  background-color: #ADD8E6;
  color: #000000;
}
.log-row-debug:hover {
  background-color: #9ac9e0;
  box-shadow: 0 1px 4px rgba(0, 0, 255, 0.15);
}

/* Info - White (#FFFFFF) */
.log-row-info {
  @apply border-slate-100;
  background-color: #FFFFFF;
  color: #000000;
}
.log-row-info:hover {
  background-color: #f9fafb;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

/* Warn - Yellow (#FFFF00) */
.log-row-warn {
  @apply border-yellow-300;
  background-color: #FFFF00;
  color: #000000;
  font-weight: 500;
}
.log-row-warn:hover {
  background-color: #f5f500;
  box-shadow: 0 2px 6px rgba(255, 255, 0, 0.3);
}

/* Error - Red (#FF0000) */
.log-row-error {
  @apply border-red-400;
  background-color: #FF0000;
  color: #ffffff;
  font-weight: 600;
}
.log-row-error:hover {
  background-color: #eb0000;
  box-shadow: 0 2px 8px rgba(255, 0, 0, 0.4);
}

/* Fatal - DarkRed (#8B0000) */
.log-row-fatal {
  @apply border-red-600;
  background-color: #8B0000;
  color: #ffffff;
  font-weight: 700;
  position: relative;
}
.log-row-fatal:hover {
  background-color: #7a0000;
  box-shadow:
    0 0 0 1px rgba(139, 0, 0, 0.5),
    0 4px 12px rgba(139, 0, 0, 0.5);
}

/* Subtle highlight animation for fatal errors */
.log-row-fatal::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg,
    transparent 0%,
    rgba(255, 100, 100, 0.8) 50%,
    transparent 100%
  );
  animation: fatal-pulse 2s ease-in-out infinite;
}

@keyframes fatal-pulse {
  0%, 100% {
    opacity: 0.4;
  }
  50% {
    opacity: 0.8;
  }
}

/* Custom scrollbar for clean editorial feel */
.log-table-container ::-webkit-scrollbar {
  width: 10px;
  height: 10px;
}

.log-table-container ::-webkit-scrollbar-track {
  background: #f8fafc; /* slate-50 */
}

.log-table-container ::-webkit-scrollbar-thumb {
  background: #cbd5e1; /* slate-300 */
  border-radius: 5px;
  border: 2px solid #f8fafc;
}

.log-table-container ::-webkit-scrollbar-thumb:hover {
  background: #94a3b8; /* slate-400 */
}
</style>
