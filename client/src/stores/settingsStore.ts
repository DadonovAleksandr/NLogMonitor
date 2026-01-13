import { defineStore } from 'pinia'
import { ref } from 'vue'
import { settingsApi } from '@/api'
import type { UserSettings, TabSetting } from '@/types'
import { logger } from '@/services/logger'

/**
 * Store для управления пользовательскими настройками приложения
 *
 * Отвечает за загрузку и сохранение настроек (открытые вкладки, активная вкладка).
 * Настройки автоматически сохраняются при изменении вкладок через composable useSettings.
 */
export const useSettingsStore = defineStore('settings', () => {
  // State
  const settings = ref<UserSettings>({
    openedTabs: [],
    lastActiveTabIndex: 0
  })
  const isLoaded = ref(false)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  /**
   * Загрузить настройки с сервера
   *
   * При ошибке используются дефолтные настройки (пустой массив вкладок).
   */
  async function loadSettings(): Promise<void> {
    if (isLoading.value) {
      logger.warn('Settings are already being loaded')
      return
    }

    isLoading.value = true
    error.value = null

    try {
      logger.info('Loading user settings from server')
      const loadedSettings = await settingsApi.getSettings()

      settings.value = loadedSettings
      isLoaded.value = true

      logger.info('Settings loaded successfully', {
        tabsCount: loadedSettings.openedTabs.length,
        activeTabIndex: loadedSettings.lastActiveTabIndex
      })
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error'
      error.value = errorMessage

      logger.error('Failed to load settings, using defaults', { error: err })

      // Используем дефолтные настройки при ошибке
      settings.value = {
        openedTabs: [],
        lastActiveTabIndex: 0
      }
      isLoaded.value = true
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Сохранить настройки на сервер
   *
   * @param newSettings - Новые настройки для сохранения
   * @throws Ошибка при неудачном сохранении
   */
  async function saveSettings(newSettings: UserSettings): Promise<void> {
    try {
      logger.debug('Saving user settings', {
        tabsCount: newSettings.openedTabs.length,
        activeTabIndex: newSettings.lastActiveTabIndex
      })

      await settingsApi.saveSettings(newSettings)

      // Обновляем локальное состояние только после успешного сохранения
      settings.value = newSettings

      logger.info('Settings saved successfully')
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error'
      error.value = errorMessage

      logger.error('Failed to save settings', { error: err })
      throw err
    }
  }

  /**
   * Получить список открытых вкладок из настроек
   */
  function getOpenedTabs(): TabSetting[] {
    return settings.value.openedTabs
  }

  /**
   * Получить индекс последней активной вкладки
   */
  function getLastActiveTabIndex(): number {
    return settings.value.lastActiveTabIndex
  }

  return {
    // State
    settings,
    isLoaded,
    isLoading,
    error,
    // Actions
    loadSettings,
    saveSettings,
    getOpenedTabs,
    getLastActiveTabIndex
  }
})
