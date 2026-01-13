import apiClient from './client'
import type { AppInfo } from '@/types'

export const infoApi = {
  /**
   * Получение информации о приложении
   */
  async getInfo(): Promise<AppInfo> {
    const response = await apiClient.get<AppInfo>('/api/info')
    return response.data
  }
}
