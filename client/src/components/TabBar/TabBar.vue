<script setup lang="ts">
import { X, FileText, FolderOpen, Plus } from 'lucide-vue-next'
import { useTabsStore } from '@/stores'

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
  <div class="tab-bar">
    <!-- Tabs -->
    <div class="tabs-container">
      <button
        v-for="tab in tabsStore.tabs"
        :key="tab.id"
        class="tab-button"
        :class="{ 'tab-button-active': isActive(tab.id) }"
        @click="selectTab(tab.id)"
      >
        <!-- Icon -->
        <FileText v-if="tab.type === 'file'" class="tab-icon" />
        <FolderOpen v-else class="tab-icon" />

        <!-- Tab name -->
        <span class="tab-name" :title="tab.filePath">
          {{ tab.fileName }}
        </span>

        <!-- Close button -->
        <div
          class="tab-close"
          @click="(e) => closeTab(tab.id, e)"
        >
          <X class="close-icon" />
        </div>

        <!-- Active indicator -->
        <div v-if="isActive(tab.id)" class="tab-active-indicator" />

        <!-- Loading indicator -->
        <div v-if="tab.isLoading" class="tab-loading-indicator" />
      </button>

      <!-- Empty state -->
      <div v-if="tabsStore.tabs.length === 0" class="tabs-empty">
        Нет открытых файлов
      </div>
    </div>

    <!-- Add buttons -->
    <div class="add-buttons">
      <button class="add-btn" @click="emit('add-file')">
        <Plus class="add-icon" />
        <span>Файл</span>
      </button>
      <button class="add-btn" @click="emit('add-folder')">
        <Plus class="add-icon" />
        <span>Папку</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
/* Import IBM Plex Mono for technical data */
@import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&display=swap');

.tab-bar {
  display: flex;
  align-items: center;
  background: linear-gradient(to bottom, #fafafa 0%, #f5f5f5 100%);
  border-bottom: 1px solid #e5e5e5;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
}

.tabs-container {
  display: flex;
  flex: 1;
  align-items: center;
  overflow-x: auto;
  scrollbar-width: none;
}

.tabs-container::-webkit-scrollbar {
  display: none;
}

.tab-button {
  position: relative;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: transparent;
  border: none;
  border-right: 1px solid #e5e5e5;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 12px;
  font-weight: 500;
  color: #737373;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

.tab-button:hover {
  background: #ffffff;
  color: #171717;
}

.tab-button-active {
  background: #ffffff;
  color: #171717;
  font-weight: 600;
}

.tab-icon {
  width: 14px;
  height: 14px;
  flex-shrink: 0;
  opacity: 0.7;
}

.tab-button-active .tab-icon {
  opacity: 1;
  color: #3b82f6;
}

.tab-name {
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tab-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  margin-left: 8px;
  flex-shrink: 0;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  opacity: 0.5;
}

.tab-close:hover {
  background: #f5f5f5;
  opacity: 1;
}

.tab-button-active .tab-close:hover {
  background: #fee2e2;
  color: #991b1b;
}

.close-icon {
  width: 14px;
  height: 14px;
}

.tab-active-indicator {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: #3b82f6;
}

.tab-loading-indicator {
  position: absolute;
  top: 4px;
  right: 4px;
  width: 6px;
  height: 6px;
  background: #3b82f6;
  border-radius: 50%;
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.tabs-empty {
  display: flex;
  align-items: center;
  height: 36px;
  padding: 0 16px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 12px;
  font-weight: 500;
  color: #a3a3a3;
}

.add-buttons {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px;
  border-left: 1px solid #e5e5e5;
}

.add-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: none;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 500;
  color: #737373;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

.add-btn:hover {
  background: #f5f5f5;
  color: #3b82f6;
}

.add-icon {
  width: 14px;
  height: 14px;
}
</style>
