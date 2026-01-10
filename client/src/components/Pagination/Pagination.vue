<script setup lang="ts">
import { ref, computed } from 'vue'
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { useLogStore } from '@/stores'

const logStore = useLogStore()

// Page size options
const pageSizeOptions = [50, 100, 200]

// Dropdown state
const isDropdownOpen = ref(false)

// Toggle dropdown
function toggleDropdown() {
  isDropdownOpen.value = !isDropdownOpen.value
}

// Close dropdown when clicking outside
function closeDropdown() {
  isDropdownOpen.value = false
}

// Select page size
function selectPageSize(size: number) {
  logStore.setPageSize(size)
  closeDropdown()
}

// Navigate to previous page
function previousPage() {
  if (logStore.canPreviousPage) {
    logStore.setPage(logStore.page - 1)
  }
}

// Navigate to next page
function nextPage() {
  if (logStore.canNextPage) {
    logStore.setPage(logStore.page + 1)
  }
}

// Page info text
const pageInfo = computed(() => {
  if (logStore.totalPages === 0) {
    return 'Страница 0 из 0'
  }
  return `Страница ${logStore.page} из ${logStore.totalPages}`
})

// Total entries info
const entriesInfo = computed(() => {
  if (logStore.totalCount === 0) {
    return '0-0 из 0'
  }
  const start = (logStore.page - 1) * logStore.pageSize + 1
  const end = Math.min(logStore.page * logStore.pageSize, logStore.totalCount)
  return `${start}-${end} из ${logStore.totalCount}`
})
</script>

<template>
  <div class="pagination-container flex items-center justify-between gap-4 border-t border-zinc-800 bg-zinc-950 px-4 py-3 font-mono text-sm">
    <!-- Left side: Page size selector -->
    <div class="flex items-center gap-3">
      <span class="text-zinc-500">Показать:</span>
      <div class="relative">
        <button
          type="button"
          class="flex h-8 min-w-[70px] items-center justify-between gap-2 rounded border border-zinc-700 bg-zinc-900 px-2.5 py-1.5 text-zinc-300 transition-colors hover:border-zinc-600 hover:bg-zinc-800 focus:outline-none focus:ring-1 focus:ring-emerald-500"
          @click="toggleDropdown"
          @blur="closeDropdown"
        >
          <span>{{ logStore.pageSize }}</span>
          <svg
            class="h-3 w-3 transition-transform"
            :class="{ 'rotate-180': isDropdownOpen }"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </button>

        <!-- Dropdown menu -->
        <div
          v-if="isDropdownOpen"
          class="absolute bottom-full left-0 z-50 mb-1 min-w-[70px] overflow-hidden rounded border border-zinc-700 bg-zinc-900 shadow-xl"
        >
          <button
            v-for="size in pageSizeOptions"
            :key="size"
            type="button"
            class="flex w-full items-center justify-center px-3 py-2 text-center transition-colors hover:bg-zinc-800"
            :class="{
              'bg-emerald-950/30 text-emerald-400': size === logStore.pageSize,
              'text-zinc-300': size !== logStore.pageSize
            }"
            @mousedown.prevent="selectPageSize(size)"
          >
            {{ size }}
          </button>
        </div>
      </div>
    </div>

    <!-- Center: Page info -->
    <div class="flex flex-col items-center gap-0.5">
      <span class="text-zinc-400">{{ pageInfo }}</span>
      <span class="text-xs text-zinc-600">{{ entriesInfo }}</span>
    </div>

    <!-- Right side: Navigation buttons -->
    <div class="flex items-center gap-2">
      <Button
        variant="outline"
        size="sm"
        :disabled="!logStore.canPreviousPage"
        class="h-8 gap-1.5 border-zinc-700 bg-zinc-900 font-mono text-xs text-zinc-300 hover:bg-zinc-800 disabled:opacity-30"
        @click="previousPage"
      >
        <ChevronLeft class="h-3.5 w-3.5" />
        <span>Назад</span>
      </Button>
      <Button
        variant="outline"
        size="sm"
        :disabled="!logStore.canNextPage"
        class="h-8 gap-1.5 border-zinc-700 bg-zinc-900 font-mono text-xs text-zinc-300 hover:bg-zinc-800 disabled:opacity-30"
        @click="nextPage"
      >
        <span>Вперёд</span>
        <ChevronRight class="h-3.5 w-3.5" />
      </Button>
    </div>
  </div>
</template>

<style scoped>
.pagination-container {
  font-family: 'JetBrains Mono', 'Fira Code', 'SF Mono', Consolas, monospace;
}

/* Custom focus states for brutalist aesthetic */
button:focus-visible {
  outline: none;
  box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.3);
}

/* Dropdown animation */
@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(4px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.absolute.bottom-full {
  animation: slideDown 0.1s ease-out;
}
</style>
