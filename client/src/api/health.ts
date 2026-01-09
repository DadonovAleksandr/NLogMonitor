import apiClient from './client'
import type { HealthResponse } from '@/types'

export const healthApi = {
  /**
   * Проверка состояния API
   */
  async check(): Promise<HealthResponse> {
    const response = await apiClient.get<HealthResponse>('/health')
    return response.data
  },

  /**
   * Проверка доступности API (возвращает boolean)
   */
  async isAvailable(): Promise<boolean> {
    try {
      const response = await this.check()
      return response.status === 'healthy'
    } catch {
      return false
    }
  }
}
