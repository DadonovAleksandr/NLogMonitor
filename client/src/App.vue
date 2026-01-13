<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { HeaderTabBar } from '@/components/HeaderTabBar'
import { Toolbar } from '@/components/Toolbar'
import { LogTable } from '@/components/LogTable'
import { StatusBar } from '@/components/StatusBar'
import { Toast } from '@/components/Toast'
import { useTabsStore, useLogStore, useFilterStore, useSettingsStore } from '@/stores'
import { usePhotinoBridge, useSettings, useFileWatcher } from '@/composables'
import { logger } from '@/services/logger'

const tabsStore = useTabsStore()
const logStore = useLogStore()
const filterStore = useFilterStore()
const settingsStore = useSettingsStore()
const photinoBridge = usePhotinoBridge()

const { startWatching, stopWatching } = useFileWatcher()

const isDesktop = ref(false)
const isRestoring = ref(false)

useSettings()

watch(
  () => tabsStore.activeTab,
  (activeTab) => {
    if (activeTab) {
      logStore.logs = activeTab.logs
      logStore.totalCount = activeTab.totalCount
      logStore.page = activeTab.page
      logStore.pageSize = activeTab.pageSize
      logStore.totalPages = activeTab.totalPages
      logStore.levelCounts = activeTab.levelCounts
      logStore.sessionId = activeTab.sessionId
      logStore.isLoading = activeTab.isLoading
      logStore.error = activeTab.error
    } else {
      logStore.logs = []
      logStore.totalCount = 0
      logStore.page = 1
      logStore.pageSize = 100
      logStore.totalPages = 0
      logStore.levelCounts = { Trace: 0, Debug: 0, Info: 0, Warn: 0, Error: 0, Fatal: 0 }
      logStore.sessionId = null
      logStore.isLoading = false
      logStore.error = null
    }
  },
  { immediate: true, deep: true }
)

watch(
  [() => logStore.page, () => logStore.pageSize],
  async ([newPage, newPageSize]) => {
    const activeTab = tabsStore.activeTab
    if (!activeTab || !activeTab.sessionId) return

    activeTab.page = newPage
    activeTab.pageSize = newPageSize

    try {
      await logStore.fetchLogs(activeTab.filters)
      syncTabWithLogStore(activeTab.id)
    } catch (error) {
      logger.error('Failed to fetch logs after page change', { error })
    }
  }
)

watch(
  () => tabsStore.activeTab?.sessionId,
  async (newSessionId, oldSessionId) => {
    if (oldSessionId && oldSessionId !== newSessionId) {
      await stopWatching()
    }

    if (newSessionId) {
      try {
        await startWatching(newSessionId, (newLogs) => {
          const activeTab = tabsStore.activeTab
          if (!activeTab || activeTab.sessionId !== logStore.sessionId) {
            logger.warn('Received logs for inactive tab, skipping UI update')
            return
          }

          logStore.appendLogs(newLogs, activeTab.filters, activeTab.activeLevels)
          activeTab.levelCounts = { ...logStore.levelCounts }
        })
        logger.info(`Started watching session ${newSessionId}`)
      } catch (err) {
        logger.error('Failed to start watching session', {
          sessionId: newSessionId,
          error: err instanceof Error ? err.message : String(err)
        })
      }
    }
  }
)

onMounted(async () => {
  isDesktop.value = photinoBridge.isDesktop.value
  await restoreTabs()
})

async function restoreTabs() {
  isRestoring.value = true

  try {
    await settingsStore.loadSettings()

    const savedTabs = tabsStore.importFromSettings(settingsStore.settings)

    if (savedTabs.length === 0) {
      logger.info('No saved tabs to restore')
      return
    }

    logger.info(`Restoring ${savedTabs.length} tabs`)

    for (const tabSetting of savedTabs) {
      try {
        if (tabSetting.type === 'file') {
          await openFileByPath(tabSetting.path, tabSetting.displayName)
        } else if (tabSetting.type === 'directory') {
          await openDirectoryByPath(tabSetting.path, tabSetting.displayName)
        }
      } catch (error) {
        logger.error(`Failed to restore tab: ${tabSetting.path}`, { error })
      }
    }

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

async function openFileByPath(path: string, displayName: string) {
  if (isDesktop.value) {
    await logStore.openFile(path)
    if (logStore.sessionId) {
      const tab = tabsStore.addTab('file', displayName, path, logStore.sessionId)
      syncTabWithLogStore(tab.id)
    }
  } else {
    logger.warn(`Cannot restore file in Web mode: ${path}`)
  }
}

async function openDirectoryByPath(path: string, displayName: string) {
  if (isDesktop.value) {
    await logStore.openDirectory(path)
    if (logStore.sessionId) {
      const tab = tabsStore.addTab('directory', displayName, path, logStore.sessionId)
      syncTabWithLogStore(tab.id)
    }
  } else {
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
          syncTabWithLogStore(tab.id)
          logger.info(`File opened: ${logStore.fileName}`)
        }
      } catch (err) {
        logger.error('Failed to open file', { error: err })
      }
    }
  } else {
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
          syncTabWithLogStore(tab.id)
          logger.info(`Directory opened: ${folderName}`)
        }
      } catch (err) {
        logger.error('Failed to open folder', { error: err })
      }
    }
  }
}

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

async function handleSearch(searchText: string) {
  const activeTab = tabsStore.activeTab
  if (!activeTab || !activeTab.sessionId) return

  filterStore.setSearchText(searchText)
  activeTab.page = 1

  try {
    await logStore.fetchLogs(activeTab.filters)
    syncTabWithLogStore(activeTab.id)
  } catch (error) {
    logger.error('Failed to fetch logs after search change', { error })
  }
}

async function handleFilterLevels() {
  const activeTab = tabsStore.activeTab
  if (!activeTab || !activeTab.sessionId) return

  activeTab.page = 1

  try {
    await logStore.fetchLogs(activeTab.filters)
    syncTabWithLogStore(activeTab.id)
  } catch (error) {
    logger.error('Failed to fetch logs after filter change', { error })
  }
}

function handleClear() {
  // Clear logs handled by Toolbar -> tabsStore.clearLogs()
}

const hasActiveTabs = computed(() => tabsStore.hasTabs)
</script>

<template>
  <div class="app-root">
    <!-- Header + Tabs (combined) -->
    <HeaderTabBar @add-file="handleAddFile" @add-folder="handleAddFolder" />

    <!-- Main Content -->
    <div v-if="hasActiveTabs" class="main-content">
      <!-- Toolbar with filters, search, and controls -->
      <Toolbar @search="handleSearch" @filter-levels="handleFilterLevels" @clear="handleClear" />

      <!-- Log Table -->
      <div class="table-wrapper">
        <LogTable />
      </div>

      <!-- Status Bar -->
      <StatusBar />
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <div class="empty-state-icon-wrapper">
        <div class="empty-state-icon">
          <svg class="icon-svg" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
      </div>

      <div class="empty-state-text">
        <h2 class="empty-state-title">Нет открытых файлов</h2>
        <p class="empty-state-description">Откройте лог-файл или директорию для начала анализа</p>
      </div>

      <div class="empty-state-actions">
        <button class="action-btn action-btn-primary" @click="handleAddFile">
          Открыть файл
        </button>

        <button class="action-btn action-btn-secondary" @click="handleAddFolder">
          Открыть папку
        </button>
      </div>
    </div>

    <!-- Toast Notifications -->
    <Toast />
  </div>
</template>

<style scoped>
.app-root {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #ECF2F9;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  color: #171717;
}

.main-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow: hidden;
}

.table-wrapper {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  overflow: hidden;
  padding: 4px;
}

/* Empty State */
.empty-state {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 20px;
  padding: 32px;
}

.empty-state-icon-wrapper {
  position: relative;
}

.empty-state-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 80px;
  height: 80px;
  background: #ffffff;
  border: 2px solid #e5e5e5;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
}

.icon-svg {
  width: 40px;
  height: 40px;
  color: #a3a3a3;
}

.empty-state-text {
  text-align: center;
}

.empty-state-title {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 18px;
  font-weight: 600;
  color: #171717;
  margin-bottom: 6px;
}

.empty-state-description {
  font-size: 12px;
  font-weight: 500;
  color: #737373;
}

.empty-state-actions {
  display: flex;
  gap: 10px;
}

.action-btn {
  padding: 8px 16px;
  font-size: 12px;
  font-weight: 500;
  border: 1px solid;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.action-btn-primary {
  background: #3b82f6;
  color: #ffffff;
  border-color: #3b82f6;
}

.action-btn-primary:hover {
  background: #2563eb;
  border-color: #2563eb;
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
  transform: translateY(-1px);
}

.action-btn-secondary {
  background: #ffffff;
  color: #525252;
  border-color: #d4d4d4;
}

.action-btn-secondary:hover {
  background: #fafafa;
  border-color: #a3a3a3;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transform: translateY(-1px);
}

.action-btn:active {
  transform: translateY(0);
}
</style>
