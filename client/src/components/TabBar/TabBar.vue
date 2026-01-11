<script setup lang="ts">
import { X, FileText, FolderOpen, Plus } from 'lucide-vue-next'
import { useTabsStore } from '@/stores'
import { Button } from '@/components/ui/button'

const tabsStore = useTabsStore()

const emit = defineEmits<{
  (e: 'add-file'): void
  (e: 'add-folder'): void
}>()

function closeTab(tabId: string, event: Event) {
  event.stopPropagation()
  tabsStore.closeTab(tabId)
}

function selectTab(tabId: string) {
  tabsStore.setActiveTab(tabId)
}

const isActive = (tabId: string) => tabsStore.activeTabId === tabId
</script>

<template>
  <div class="flex items-center border-b-2 border-zinc-800 bg-zinc-950">
    <!-- Tabs -->
    <div class="flex flex-1 items-center overflow-x-auto scrollbar-hide">
      <button
        v-for="tab in tabsStore.tabs"
        :key="tab.id"
        class="group relative flex items-center gap-2 border-r border-zinc-800 px-4 py-1.5 transition-all hover:bg-zinc-900/50"
        :class="{
          'bg-zinc-900 text-zinc-100': isActive(tab.id),
          'text-zinc-500 hover:text-zinc-300': !isActive(tab.id)
        }"
        @click="selectTab(tab.id)"
      >
        <!-- Icon -->
        <FileText v-if="tab.type === 'file'" class="h-4 w-4 flex-shrink-0" />
        <FolderOpen v-else class="h-4 w-4 flex-shrink-0" />

        <!-- Tab name -->
        <span class="max-w-[180px] truncate font-mono text-sm" :title="tab.filePath">
          {{ tab.fileName }}
        </span>

        <!-- Close button (div instead of button to avoid nested buttons) -->
        <div
          class="ml-2 flex h-5 w-5 flex-shrink-0 cursor-pointer items-center justify-center rounded transition-colors hover:bg-zinc-700"
          :class="{
            'text-zinc-400 hover:text-zinc-100': isActive(tab.id),
            'text-zinc-600 hover:text-zinc-300': !isActive(tab.id)
          }"
          @click="(e) => closeTab(tab.id, e)"
        >
          <X class="h-3.5 w-3.5" />
        </div>

        <!-- Active indicator -->
        <div
          v-if="isActive(tab.id)"
          class="absolute bottom-0 left-0 right-0 h-0.5 bg-gradient-to-r from-emerald-500 to-cyan-500"
        />

        <!-- Loading indicator -->
        <div
          v-if="tab.isLoading"
          class="absolute right-1 top-1 h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-500"
        />
      </button>

      <!-- Empty state -->
      <div v-if="tabsStore.tabs.length === 0" class="flex h-9 items-center px-4 font-mono text-sm text-zinc-600">
        Нет открытых файлов
      </div>
    </div>

    <!-- Add buttons -->
    <div class="flex items-center gap-1 border-l border-zinc-800 p-1">
      <Button
        variant="ghost"
        size="sm"
        class="gap-2 font-mono text-xs text-zinc-400 hover:bg-zinc-800 hover:text-emerald-400"
        @click="emit('add-file')"
      >
        <Plus class="h-4 w-4" />
        Файл
      </Button>
      <Button
        variant="ghost"
        size="sm"
        class="gap-2 font-mono text-xs text-zinc-400 hover:bg-zinc-800 hover:text-emerald-400"
        @click="emit('add-folder')"
      >
        <Plus class="h-4 w-4" />
        Папку
      </Button>
    </div>
  </div>
</template>

<style scoped>
/* Hide scrollbar but keep functionality */
.scrollbar-hide::-webkit-scrollbar {
  display: none;
}

.scrollbar-hide {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
