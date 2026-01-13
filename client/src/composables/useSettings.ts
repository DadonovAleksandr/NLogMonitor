import { watch } from 'vue'
import { useTabsStore } from '@/stores/tabsStore'
import { useSettingsStore } from '@/stores/settingsStore'
import { logger } from '@/services/logger'

let saveTimeout: ReturnType<typeof setTimeout> | null = null

/**
 * Composable для автоматического сохранения настроек при изменении вкладок
 *
 * Использование:
 * ```typescript
 * import { useSettings } from '@/composables'
 *
 * // В setup():
 * useSettings()
 * ```
 *
 * Настройки сохраняются автоматически через 1 секунду после последнего изменения вкладок
 * (debounce). Это позволяет избежать частых запросов к серверу при быстрых операциях
 * с вкладками (например, закрытие нескольких вкладок подряд).
 */
export function useSettings() {
  const tabsStore = useTabsStore()
  const settingsStore = useSettingsStore()

  /**
   * Запланировать сохранение настроек (с debounce)
   *
   * Сохранение произойдёт через 1 секунду после последнего вызова.
   * Если функция вызывается повторно до истечения таймаута, таймаут сбрасывается.
   */
  function scheduleSave() {
    if (saveTimeout) {
      clearTimeout(saveTimeout)
    }

    saveTimeout = setTimeout(async () => {
      try {
        const settings = tabsStore.exportToSettings()

        logger.debug('Auto-saving settings', {
          tabsCount: settings.openedTabs.length,
          activeTabIndex: settings.lastActiveTabIndex
        })

        await settingsStore.saveSettings(settings)
      } catch (error) {
        logger.error('Failed to auto-save settings', { error })
        // Не пробрасываем ошибку - автосохранение не должно прерывать работу приложения
      }
    }, 1000)
  }

  /**
   * Отслеживание изменений вкладок для автоматического сохранения
   *
   * Отслеживаем:
   * 1. Количество вкладок (добавление/удаление)
   * 2. ID активной вкладки (переключение между вкладками)
   *
   * Сохранение происходит только после загрузки начальных настроек (settingsStore.isLoaded),
   * чтобы избежать перезаписи настроек при инициализации приложения.
   */
  watch(
    () => [tabsStore.tabs.length, tabsStore.activeTabId],
    () => {
      if (settingsStore.isLoaded) {
        scheduleSave()
      }
    },
    { deep: true }
  )

  return {
    scheduleSave
  }
}
