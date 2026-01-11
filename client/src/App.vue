<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { TabBar } from '@/components/TabBar'
import { Toolbar } from '@/components/Toolbar'
import { TableControls } from '@/components/TableControls'
import { LogTable } from '@/components/LogTable'
import { Pagination } from '@/components/Pagination'
import { Toast } from '@/components/Toast'
import { useTabsStore, useLogStore, useFilterStore, useSettingsStore } from '@/stores'
import { usePhotinoBridge, useSettings, useFileWatcher } from '@/composables'
import { logger } from '@/services/logger'

const tabsStore = useTabsStore()
const logStore = useLogStore()
const filterStore = useFilterStore()
const settingsStore = useSettingsStore()
const photinoBridge = usePhotinoBridge()

// SignalR file watcher для real-time обновлений
const { startWatching, stopWatching } = useFileWatcher()

const isDesktop = ref(false)
const isRestoring = ref(false)

// Подключаем автосохранение настроек при изменении вкладок
useSettings()

// Синхронизация activeTab → logStore для работы LogTable и Pagination
watch(
  () => tabsStore.activeTab,
  (activeTab) => {
    if (activeTab) {
      // Копируем данные из активной вкладки в logStore
      logStore.logs = activeTab.logs
      logStore.totalCount = activeTab.totalCount
      logStore.page = activeTab.page
      logStore.pageSize = activeTab.pageSize
      logStore.totalPages = activeTab.totalPages
      logStore.levelCounts = activeTab.levelCounts
      logStore.sessionId = activeTab.sessionId
      logStore.isLoading = activeTab.isLoading
      logStore.error = activeTab.error

      // ВАЖНО: filterStore автоматически обновится через computed properties
      // Не требуется явная синхронизация, так как filterStore читает из activeTab
    } else {
      // Нет активной вкладки - очищаем logStore
      logStore.logs = []
      logStore.totalCount = 0
      logStore.page = 1
      logStore.pageSize = 50
      logStore.totalPages = 0
      logStore.levelCounts = { Trace: 0, Debug: 0, Info: 0, Warn: 0, Error: 0, Fatal: 0 }
      logStore.sessionId = null
      logStore.isLoading = false
      logStore.error = null

      // filterStore тоже автоматически "очистится" (вернёт дефолтные значения)
    }
  },
  { immediate: true, deep: true }
)

// Обратная синхронизация: logStore → activeTab при изменении страницы/размера
watch(
  [() => logStore.page, () => logStore.pageSize],
  async ([newPage, newPageSize]) => {
    const activeTab = tabsStore.activeTab
    if (!activeTab || !activeTab.sessionId) return

    // Обновляем поля в activeTab
    activeTab.page = newPage
    activeTab.pageSize = newPageSize

    // Загружаем новые данные с сервера
    try {
      await logStore.fetchLogs(activeTab.filters)
      // Синхронизируем загруженные данные обратно в activeTab
      syncTabWithLogStore(activeTab.id)
    } catch (error) {
      logger.error('Failed to fetch logs after page change', { error })
    }
  }
)

// Отслеживание изменений sessionId активной вкладки для управления SignalR подпиской
watch(
  () => tabsStore.activeTab?.sessionId,
  async (newSessionId, oldSessionId) => {
    // Если была старая сессия, останавливаем отслеживание
    if (oldSessionId && oldSessionId !== newSessionId) {
      await stopWatching()
    }

    // Если появилась новая сессия, начинаем отслеживание
    if (newSessionId) {
      try {
        await startWatching(newSessionId, (newLogs) => {
          const activeTab = tabsStore.activeTab
          // Проверяем, что событие для текущей активной вкладки
          if (!activeTab || activeTab.sessionId !== logStore.sessionId) {
            logger.warn('Received logs for inactive tab, skipping UI update')
            return
          }

          // Передаём фильтры и activeLevels активной вкладки в appendLogs
          logStore.appendLogs(newLogs, activeTab.filters, activeTab.activeLevels)

          // Обновляем levelCounts в активной вкладке
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

/**
 * Обработчик изменения поискового запроса
 * Обновляет searchText в активной вкладке через filterStore и перезагружает логи
 */
async function handleSearch(searchText: string) {
  const activeTab = tabsStore.activeTab
  if (!activeTab || !activeTab.sessionId) return

  // Обновляем searchText в активной вкладке через filterStore
  filterStore.setSearchText(searchText)

  // Сбрасываем на первую страницу при изменении фильтра
  activeTab.page = 1

  // Загружаем логи с новыми фильтрами
  try {
    await logStore.fetchLogs(activeTab.filters)
    syncTabWithLogStore(activeTab.id)
  } catch (error) {
    logger.error('Failed to fetch logs after search change', { error })
  }
}

/**
 * Обработчик изменения фильтров по уровням логирования
 * Фильтры уже обновлены в filterStore через FilterPanel (вызывает filterStore.toggleLevel)
 * Здесь мы только перезагружаем логи с новыми фильтрами
 */
async function handleFilterLevels() {
  const activeTab = tabsStore.activeTab
  if (!activeTab || !activeTab.sessionId) return

  // Фильтры уже обновлены в filterStore через FilterPanel
  // (FilterPanel вызывает filterStore.toggleLevel напрямую)
  // Здесь мы только перезагружаем логи

  // Сбрасываем на первую страницу при изменении фильтра
  activeTab.page = 1

  // Загружаем логи с новыми фильтрами
  try {
    await logStore.fetchLogs(activeTab.filters)
    syncTabWithLogStore(activeTab.id)
  } catch (error) {
    logger.error('Failed to fetch logs after filter change', { error })
  }
}

function handleClear() {
  // Clear logs handled by TableControls -> tabsStore.clearLogs()
}

const hasActiveTabs = computed(() => tabsStore.hasTabs)
</script>

<template>
  <div class="app-root">
    <!-- Header -->
    <header class="app-header">
      <div class="header-content">
        <div class="header-left">
          <!-- Logo -->
          <div class="app-logo">
            <span class="logo-text">N</span>
          </div>

          <div class="app-title-group">
            <h1 class="app-title">nLogMonitor</h1>
            <p class="app-subtitle">Professional Log Analysis Tool</p>
          </div>
        </div>

        <div class="app-version">v1.0.0</div>
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

      <!-- Log Table with Pagination -->
      <div class="flex flex-1 flex-col overflow-hidden p-2">
        <div class="flex flex-1 flex-col overflow-hidden rounded-lg">
          <LogTable />
          <Pagination />
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <div class="empty-state-icon-wrapper">
        <!-- Icon -->
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
/* Import IBM Plex Mono for technical data */
@import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&display=swap');

/* App Root */
.app-root {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #ECF2F9;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  color: #171717;
}

/* Header */
.app-header {
  padding: 12px 20px;
  background: linear-gradient(to bottom, #ffffff 0%, #fafafa 100%);
  border-bottom: 1px solid #e5e5e5;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.header-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  max-width: 100%;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.app-logo {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

.logo-text {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 18px;
  font-weight: 700;
  color: #ffffff;
}

.app-title-group {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.app-title {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 18px;
  font-weight: 600;
  color: #171717;
  letter-spacing: -0.5px;
}

.app-subtitle {
  font-size: 11px;
  font-weight: 500;
  color: #737373;
  letter-spacing: 0.2px;
}

.app-version {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px;
  font-weight: 500;
  color: #a3a3a3;
  padding: 4px 8px;
  background: #f5f5f5;
  border: 1px solid #e5e5e5;
  border-radius: 4px;
}

/* Empty State */
.empty-state {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 24px;
  padding: 32px;
}

.empty-state-icon-wrapper {
  position: relative;
}

.empty-state-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 96px;
  height: 96px;
  background: #ffffff;
  border: 2px solid #e5e5e5;
  border-radius: 16px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
}

.icon-svg {
  width: 48px;
  height: 48px;
  color: #a3a3a3;
}

.empty-state-text {
  text-align: center;
}

.empty-state-title {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 20px;
  font-weight: 600;
  color: #171717;
  margin-bottom: 8px;
}

.empty-state-description {
  font-size: 13px;
  font-weight: 500;
  color: #737373;
}

.empty-state-actions {
  display: flex;
  gap: 12px;
}

.action-btn {
  padding: 10px 20px;
  font-size: 13px;
  font-weight: 500;
  border: 1px solid;
  border-radius: 8px;
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
