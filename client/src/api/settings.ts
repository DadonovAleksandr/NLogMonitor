import apiClient from './client'
import type { UserSettings } from '@/types'

/**
 * API клиент для работы с пользовательскими настройками приложения
 *
 * @example
 * // Получение настроек
 * const settings = await settingsApi.getSettings()
 * console.log(settings.openedTabs) // [{type: 'file', path: '...', displayName: '...'}, ...]
 * console.log(settings.lastActiveTabIndex) // 0
 *
 * @example
 * // Сохранение настроек
 * await settingsApi.saveSettings({
 *   openedTabs: [
 *     { type: 'file', path: '/path/to/file.log', displayName: 'file.log' },
 *     { type: 'directory', path: '/path/to/logs', displayName: 'logs' }
 *   ],
 *   lastActiveTabIndex: 0
 * })
 */
export const settingsApi = {
  /**
   * Получить текущие настройки пользователя
   * @returns Пользовательские настройки (открытые вкладки, активная вкладка)
   */
  async getSettings(): Promise<UserSettings> {
    const { data } = await apiClient.get<UserSettings>('/api/settings')
    return data
  },

  /**
   * Сохранить настройки пользователя
   * @param settings - Настройки для сохранения
   */
  async saveSettings(settings: UserSettings): Promise<void> {
    await apiClient.put('/api/settings', settings)
  }
}
