<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { TabBar } from '@/components/TabBar'
import { Toolbar } from '@/components/Toolbar'
import { TableControls } from '@/components/TableControls'
import { LogTable } from '@/components/LogTable'
import { Toast } from '@/components/Toast'
import { useTabsStore, useLogStore, useSettingsStore } from '@/stores'
import { usePhotinoBridge, useSettings } from '@/composables'
import { logger } from '@/services/logger'

const tabsStore = useTabsStore()
const logStore = useLogStore()
const settingsStore = useSettingsStore()
const photinoBridge = usePhotinoBridge()

const isDesktop = ref(false)
const isRestoring = ref(false)

// Подключаем автосохранение настроек при изменении вкладок
useSettings()

onMounted(async () => {
  // Check if running in Desktop mode
  isDesktop.value = photinoBridge.isDesktop.value

  // Загрузка настроек и восстановление вкладок
  await restoreTabs()
})

/**
 * Восстановление вкладок из сохраненных настроек
 */
async function restoreTabs() {
  isRestoring.value = true

  try {
    // Загружаем настройки
    await settingsStore.loadSettings()

    const savedTabs = tabsStore.importFromSettings(settingsStore.settings)

    if (savedTabs.length === 0) {
      logger.info('No saved tabs to restore')
      return
    }

    logger.info(`Restoring ${savedTabs.length} tabs`)

    // Восстанавливаем каждую вкладку
    for (const tabSetting of savedTabs) {
      try {
        if (tabSetting.type === 'file') {
          await openFileByPath(tabSetting.path, tabSetting.displayName)
        } else if (tabSetting.type === 'directory') {
          await openDirectoryByPath(tabSetting.path, tabSetting.displayName)
        }
      } catch (error) {
        logger.error(`Failed to restore tab: ${tabSetting.path}`, { error })
        // Продолжаем восстановление остальных вкладок
      }
    }

    // Восстанавливаем активную вкладку
    const lastActiveIndex = settingsStore.settings.lastActiveTabIndex
    if (lastActiveIndex >= 0 && lastActiveIndex < tabsStore.tabs.length) {
      const activeTab = tabsStore.tabs[lastActiveIndex]
      if (activeTab) {
        tabsStore.setActiveTab(activeTab.id)
      }
    }

    logger.info('Tabs restored successfully')
  } catch (error) {
    logger.error('Failed to restore tabs', { error })
  } finally {
    isRestoring.value = false
  }
}

/**
 * Открытие файла по пути (для восстановления из настроек)
 */
async function openFileByPath(path: string, displayName: string) {
  if (isDesktop.value) {
    // Desktop mode: открываем через API
    await logStore.openFile(path)
    if (logStore.sessionId) {
      const tab = tabsStore.addTab('file', displayName, path, logStore.sessionId)
      // Копируем данные из logStore в восстановленную вкладку
      syncTabWithLogStore(tab.id)
    }
  } else {
    // Web mode: файлы нельзя открыть по пути, пропускаем
    logger.warn(`Cannot restore file in Web mode: ${path}`)
  }
}

/**
 * Открытие директории по пути (для восстановления из настроек)
 */
async function openDirectoryByPath(path: string, displayName: string) {
  if (isDesktop.value) {
    // Desktop mode: открываем через API
    await logStore.openDirectory(path)
    if (logStore.sessionId) {
      const tab = tabsStore.addTab('directory', displayName, path, logStore.sessionId)
      // Копируем данные из logStore в восстановленную вкладку
      syncTabWithLogStore(tab.id)
    }
  } else {
    // Web mode: директории нельзя открыть, пропускаем
    logger.warn(`Cannot restore directory in Web mode: ${path}`)
  }
}

async function handleAddFile() {
  if (isDesktop.value) {
    const filePath = await photinoBridge.showOpenFileDialog()
    if (filePath) {
      try {
        await logStore.openFile(filePath)
        if (logStore.sessionId) {
          const tab = tabsStore.addTab('file', logStore.fileName, filePath, logStore.sessionId)
          // Копируем данные из logStore в новую вкладку
          syncTabWithLogStore(tab.id)
          logger.info(`File opened: ${logStore.fileName}`)
        }
      } catch (err) {
        logger.error('Failed to open file', { error: err })
      }
    }
  } else {
    // Web mode: trigger file input
    const input = document.createElement('input')
    input.type = 'file'
    input.accept = '.log,.txt'
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0]
      if (file) {
        try {
          await logStore.uploadFile(file)
          if (logStore.sessionId) {
            const tab = tabsStore.addTab('file', file.name, file.name, logStore.sessionId)
            // Копируем данные из logStore в новую вкладку
            syncTabWithLogStore(tab.id)
            logger.info(`File uploaded: ${file.name}`)
          }
        } catch (err) {
          logger.error('Failed to upload file', { error: err })
        }
      }
    }
    input.click()
  }
}

async function handleAddFolder() {
  if (isDesktop.value) {
    const folderPath = await photinoBridge.showOpenFolderDialog()
    if (folderPath) {
      try {
        await logStore.openDirectory(folderPath)
        if (logStore.sessionId) {
          const folderName = folderPath.split(/[/\\]/).pop() || folderPath
          const tab = tabsStore.addTab('directory', folderName, folderPath, logStore.sessionId)
          // Копируем данные из logStore в новую вкладку
          syncTabWithLogStore(tab.id)
          logger.info(`Directory opened: ${folderName}`)
        }
      } catch (err) {
        logger.error('Failed to open folder', { error: err })
      }
    }
  }
}

/**
 * Синхронизация данных вкладки с logStore
 */
function syncTabWithLogStore(tabId: string) {
  tabsStore.updateTab(tabId, {
    logs: logStore.logs,
    totalCount: logStore.totalCount,
    page: logStore.page,
    pageSize: logStore.pageSize,
    totalPages: logStore.totalPages,
    levelCounts: logStore.levelCounts
  })
}

function handleSearch(searchText: string) {
  if (tabsStore.activeTab) {
    tabsStore.activeTab.searchText = searchText
    // TODO: Trigger log filtering
  }
}

function handleFilterLevels(levels: Set<string>) {
  if (tabsStore.activeTab) {
    // TODO: Update tab filters and trigger log filtering
    console.log('Filter levels:', levels)
  }
}

function handleClear() {
  // Clear logs handled by TableControls -> tabsStore.clearLogs()
}

const hasActiveTabs = computed(() => tabsStore.hasTabs)
</script>

<template>
  <div class="flex h-screen flex-col bg-zinc-950 text-zinc-100">
    <!-- Header -->
    <header class="border-b-2 border-zinc-800 bg-gradient-to-b from-zinc-900 to-zinc-950 px-4 py-3 shadow-xl">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <!-- Logo with glow effect -->
          <div class="relative">
            <div class="absolute inset-0 animate-pulse rounded-lg bg-emerald-500/20 blur-lg" />
            <div class="relative flex h-10 w-10 items-center justify-center rounded-lg border-2 border-emerald-600 bg-gradient-to-br from-emerald-600 to-cyan-600 shadow-lg">
              <span class="font-mono text-lg font-bold text-white">N</span>
            </div>
          </div>

          <div>
            <h1 class="bg-gradient-to-r from-emerald-400 to-cyan-400 bg-clip-text font-mono text-xl font-bold tracking-tight text-transparent">
              nLogMonitor
            </h1>
            <p class="font-mono text-xs text-zinc-500">Professional Log Analysis Tool</p>
          </div>
        </div>

        <div class="font-mono text-xs text-zinc-600">v1.0.0</div>
      </div>
    </header>

    <!-- Tab Bar -->
    <TabBar @add-file="handleAddFile" @add-folder="handleAddFolder" />

    <!-- Main Content -->
    <div v-if="hasActiveTabs" class="flex flex-1 flex-col overflow-hidden">
      <!-- Toolbar with filters and search -->
      <Toolbar @search="handleSearch" @filter-levels="handleFilterLevels" />

      <!-- Table Controls -->
      <TableControls @clear="handleClear" />

      <!-- Log Table -->
      <div class="flex-1 overflow-hidden p-2">
        <LogTable />
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="flex flex-1 flex-col items-center justify-center gap-6 p-8">
      <div class="relative">
        <!-- Animated background -->
        <div class="absolute inset-0 -z-10 animate-pulse rounded-full bg-gradient-to-r from-emerald-500/10 to-cyan-500/10 blur-3xl" />

        <!-- Icon -->
        <div class="flex h-24 w-24 items-center justify-center rounded-2xl border-2 border-zinc-700 bg-zinc-900">
          <svg class="h-12 w-12 text-zinc-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
      </div>

      <div class="text-center">
        <h2 class="font-mono text-2xl font-semibold text-zinc-300">Нет открытых файлов</h2>
        <p class="mt-2 font-mono text-sm text-zinc-600">Откройте лог-файл или директорию для начала анализа</p>
      </div>

      <div class="flex gap-3">
        <button
          class="group relative overflow-hidden rounded-lg border-2 border-emerald-700 bg-emerald-950/50 px-6 py-3 font-mono text-sm font-medium text-emerald-400 transition-all hover:border-emerald-500 hover:bg-emerald-900/50 hover:text-emerald-300 hover:shadow-lg hover:shadow-emerald-900/50"
          @click="handleAddFile"
        >
          <span class="relative z-10">Открыть файл</span>
          <!-- Hover effect -->
          <div class="absolute inset-0 -z-0 translate-y-full bg-gradient-to-t from-emerald-600/20 to-transparent transition-transform group-hover:translate-y-0" />
        </button>

        <button
          class="group relative overflow-hidden rounded-lg border-2 border-cyan-700 bg-cyan-950/50 px-6 py-3 font-mono text-sm font-medium text-cyan-400 transition-all hover:border-cyan-500 hover:bg-cyan-900/50 hover:text-cyan-300 hover:shadow-lg hover:shadow-cyan-900/50"
          @click="handleAddFolder"
        >
          <span class="relative z-10">Открыть папку</span>
          <!-- Hover effect -->
          <div class="absolute inset-0 -z-0 translate-y-full bg-gradient-to-t from-cyan-600/20 to-transparent transition-transform group-hover:translate-y-0" />
        </button>
      </div>
    </div>

    <!-- Toast Notifications -->
    <Toast />

    <!-- Terminal scanline overlay -->
    <div class="pointer-events-none fixed inset-0 z-50 bg-[repeating-linear-gradient(0deg,transparent,transparent_2px,rgba(0,0,0,0.03)_2px,rgba(0,0,0,0.03)_4px)] opacity-30" />
  </div>
</template>

<style scoped>
/* Neo-brutalist aesthetic */
@keyframes glow-pulse {
  0%, 100% {
    box-shadow: 0 0 20px rgba(16, 185, 129, 0.3);
  }
  50% {
    box-shadow: 0 0 40px rgba(16, 185, 129, 0.5);
  }
}

/* Smooth transitions */
* {
  transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
}
</style>
