<script setup lang="ts">
import { X, FileText, FolderOpen, FilePlus, FolderPlus, Tag } from 'lucide-vue-next'
import { useTabsStore } from '@/stores'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger
} from '@/components/ui/tooltip'

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
  <header class="header-tabbar">
    <!-- Logo + Title + Version -->
    <div class="header-brand">
      <div class="app-logo">
        <span class="logo-text">N</span>
      </div>
      <span class="app-title">nLogMonitor</span>
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger as-child>
            <div class="app-version">
              <Tag class="version-icon" />
              <span>1.0.0</span>
            </div>
          </TooltipTrigger>
          <TooltipContent side="bottom" :side-offset="4">
            Version: 1.0.0
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    </div>

    <!-- Separator -->
    <div class="header-separator" />

    <!-- Tabs -->
    <TooltipProvider :delay-duration="400">
      <div class="tabs-container">
        <Tooltip v-for="tab in tabsStore.tabs" :key="tab.id">
          <TooltipTrigger as-child>
            <button
              class="tab-button"
              :class="{ 'tab-button-active': isActive(tab.id) }"
              @click="selectTab(tab.id)"
            >
              <!-- Icon -->
              <FileText v-if="tab.type === 'file'" class="tab-icon" />
              <FolderOpen v-else class="tab-icon" />

              <!-- Tab name -->
              <span class="tab-name">{{ tab.fileName }}</span>

              <!-- Close button -->
              <div class="tab-close" @click="(e) => closeTab(tab.id, e)">
                <X class="close-icon" />
              </div>

              <!-- Active indicator -->
              <div v-if="isActive(tab.id)" class="tab-active-indicator" />

              <!-- Loading indicator -->
              <div v-if="tab.isLoading" class="tab-loading-indicator" />
            </button>
          </TooltipTrigger>
          <TooltipContent side="bottom" :side-offset="4">
            {{ tab.filePath }}
          </TooltipContent>
        </Tooltip>

        <!-- Empty state -->
        <div v-if="tabsStore.tabs.length === 0" class="tabs-empty">
          Нет открытых файлов
        </div>
      </div>
    </TooltipProvider>

    <!-- Add buttons -->
    <div class="add-buttons">
      <Tooltip>
        <TooltipTrigger as-child>
          <button class="add-btn" @click="emit('add-file')">
            <FilePlus class="add-icon" />
          </button>
        </TooltipTrigger>
        <TooltipContent side="bottom" :side-offset="4">
          Открыть файл
        </TooltipContent>
      </Tooltip>

      <Tooltip>
        <TooltipTrigger as-child>
          <button class="add-btn" @click="emit('add-folder')">
            <FolderPlus class="add-icon" />
          </button>
        </TooltipTrigger>
        <TooltipContent side="bottom" :side-offset="4">
          Открыть папку
        </TooltipContent>
      </Tooltip>
    </div>
  </header>
</template>

<style scoped>
.header-tabbar {
  display: flex;
  align-items: center;
  height: 32px;
  padding: 0 8px;
  background: linear-gradient(to bottom, #ffffff 0%, #fafafa 100%);
  border-bottom: 1px solid #e5e5e5;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  gap: 8px;
}

/* Brand */
.header-brand {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.app-logo {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  border-radius: 4px;
  box-shadow: 0 1px 3px rgba(59, 130, 246, 0.3);
}

.logo-text {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 12px;
  font-weight: 700;
  color: #ffffff;
}

.app-title {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 13px;
  font-weight: 600;
  color: #171717;
  letter-spacing: -0.3px;
}

/* Separator */
.header-separator {
  width: 1px;
  height: 18px;
  background: #e5e5e5;
  flex-shrink: 0;
}

/* Tabs */
.tabs-container {
  display: flex;
  flex: 1;
  align-items: center;
  overflow-x: auto;
  scrollbar-width: none;
  gap: 2px;
}

.tabs-container::-webkit-scrollbar {
  display: none;
}

.tab-button {
  position: relative;
  display: flex;
  align-items: center;
  gap: 4px;
  height: 24px;
  padding: 0 8px;
  background: transparent;
  border: none;
  border-radius: 4px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  color: #737373;
  cursor: pointer;
  transition: all 0.12s ease;
  flex-shrink: 0;
}

.tab-button:hover {
  background: #f5f5f5;
  color: #525252;
}

.tab-button-active {
  background: #e5e5e5;
  color: #171717;
  font-weight: 600;
}

.tab-icon {
  width: 12px;
  height: 12px;
  flex-shrink: 0;
  opacity: 0.6;
}

.tab-button-active .tab-icon {
  opacity: 1;
  color: #3b82f6;
}

.tab-name {
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tab-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 14px;
  height: 14px;
  margin-left: 2px;
  flex-shrink: 0;
  border-radius: 3px;
  cursor: pointer;
  transition: all 0.12s ease;
  opacity: 0;
}

.tab-button:hover .tab-close {
  opacity: 0.5;
}

.tab-close:hover {
  background: #e5e5e5;
  opacity: 1 !important;
}

.tab-button-active .tab-close:hover {
  background: #fee2e2;
  color: #991b1b;
}

.close-icon {
  width: 10px;
  height: 10px;
}

.tab-active-indicator {
  position: absolute;
  bottom: 0;
  left: 4px;
  right: 4px;
  height: 2px;
  background: #3b82f6;
  border-radius: 1px 1px 0 0;
}

.tab-loading-indicator {
  position: absolute;
  top: 3px;
  right: 3px;
  width: 4px;
  height: 4px;
  background: #3b82f6;
  border-radius: 50%;
  animation: pulse 1.5s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.tabs-empty {
  display: flex;
  align-items: center;
  padding: 0 8px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  color: #a3a3a3;
}

/* Add buttons */
.add-buttons {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}

.add-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  background: transparent;
  border: none;
  border-radius: 4px;
  color: #737373;
  cursor: pointer;
  transition: all 0.12s ease;
}

.add-btn:hover {
  background: #f5f5f5;
  color: #3b82f6;
}

.add-icon {
  width: 14px;
  height: 14px;
}

/* Version */
.app-version {
  display: flex;
  align-items: center;
  gap: 3px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  color: #171717;
  padding: 2px 6px;
  background: #f5f5f5;
  border: 1px solid #e5e5e5;
  border-radius: 3px;
  flex-shrink: 0;
  cursor: default;
  transition: all 0.15s ease;
}

.app-version:hover {
  background: #ebebeb;
  border-color: #d4d4d4;
}

.version-icon {
  width: 9px;
  height: 9px;
}
</style>
