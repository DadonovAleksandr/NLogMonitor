<script setup lang="ts">
import { ref, computed } from 'vue'
import { Download, ChevronDown, FileJson, FileText } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { exportApi } from '@/api'
import { useLogStore } from '@/stores/logStore'
import { useFilterStore } from '@/stores/filterStore'
import type { ExportFormat } from '@/types'

// Stores
const logStore = useLogStore()
const filterStore = useFilterStore()

// State
const isOpen = ref(false)
const isExporting = ref(false)

// Computed
const isDisabled = computed(() => !logStore.hasSession || isExporting.value)

// Export options
const exportOptions = [
  { format: 'json' as ExportFormat, label: 'JSON', icon: FileJson },
  { format: 'csv' as ExportFormat, label: 'CSV', icon: FileText }
]

// Methods
async function handleExport(format: ExportFormat) {
  if (!logStore.sessionId) return

  isOpen.value = false
  isExporting.value = true

  try {
    // Используем downloadExport из API, который создаёт <a> и кликает
    exportApi.downloadExport(
      logStore.sessionId,
      format,
      filterStore.filterOptions
    )
  } catch (error) {
    console.error('Export failed:', error)
  } finally {
    // Даём браузеру время на старт загрузки
    setTimeout(() => {
      isExporting.value = false
    }, 1000)
  }
}

function toggleDropdown() {
  if (!isDisabled.value) {
    isOpen.value = !isOpen.value
  }
}

function closeDropdown() {
  isOpen.value = false
}

// Close on click outside
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  if (!target.closest('.export-button-wrapper')) {
    closeDropdown()
  }
}

// Add/remove global click listener
function setupClickOutside() {
  if (isOpen.value) {
    document.addEventListener('click', handleClickOutside)
  } else {
    document.removeEventListener('click', handleClickOutside)
  }
}

// Watch for dropdown open/close
import { watch } from 'vue'
watch(isOpen, setupClickOutside)
</script>

<template>
  <div class="export-button-wrapper relative">
    <!-- Main Button -->
    <Button
      :disabled="isDisabled"
      :class="[
        'font-mono text-sm font-bold uppercase tracking-wide',
        'border-2 border-emerald-500 bg-black text-emerald-500',
        'hover:bg-emerald-500 hover:text-black',
        'disabled:border-gray-600 disabled:text-gray-600 disabled:hover:bg-black',
        'transition-colors duration-200',
        'flex items-center gap-2 px-4 py-2'
      ]"
      @click="toggleDropdown"
    >
      <Download v-if="!isExporting" :size="16" />
      <div
        v-else
        class="w-4 h-4 border-2 border-emerald-500 border-t-transparent rounded-full animate-spin"
      />
      <span>{{ isExporting ? 'EXPORTING...' : 'EXPORT' }}</span>
      <ChevronDown
        :size="16"
        :class="[
          'transition-transform duration-200',
          isOpen ? 'rotate-180' : ''
        ]"
      />
    </Button>

    <!-- Dropdown Menu -->
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 -translate-y-2"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-150 ease-in"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 -translate-y-2"
    >
      <div
        v-if="isOpen"
        class="absolute top-full right-0 mt-2 w-48 z-50"
      >
        <div
          class="border-2 border-emerald-500 bg-black shadow-[4px_4px_0px_0px_rgba(16,185,129,0.5)]"
        >
          <button
            v-for="option in exportOptions"
            :key="option.format"
            type="button"
            class="w-full flex items-center gap-3 px-4 py-3 font-mono text-sm font-bold uppercase tracking-wide text-emerald-500 hover:bg-emerald-500 hover:text-black transition-colors duration-200 border-b-2 border-emerald-500 last:border-b-0"
            @click="handleExport(option.format)"
          >
            <component :is="option.icon" :size="18" />
            <span>{{ option.label }}</span>
          </button>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* Анимация вращения для loading spinner */
@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.animate-spin {
  animation: spin 1s linear infinite;
}
</style>
