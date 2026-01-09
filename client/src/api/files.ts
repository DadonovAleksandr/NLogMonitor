import apiClient from './client'
import type { OpenFileResult, RecentLog } from '@/types'

export const filesApi = {
  /**
   * Загрузка файла (Web режим)
   */
  async uploadFile(file: File): Promise<OpenFileResult> {
    const formData = new FormData()
    formData.append('file', file)

    const response = await apiClient.post<OpenFileResult>('/api/upload', formData)
    return response.data
  },

  /**
   * Открытие файла по пути (Desktop режим)
   */
  async openFile(filePath: string): Promise<OpenFileResult> {
    const response = await apiClient.post<OpenFileResult>('/api/files/open', {
      filePath
    })
    return response.data
  },

  /**
   * Открытие директории (Desktop режим)
   */
  async openDirectory(directoryPath: string): Promise<OpenFileResult> {
    const response = await apiClient.post<OpenFileResult>('/api/files/open-directory', {
      directoryPath
    })
    return response.data
  },

  /**
   * Остановка отслеживания файла
   */
  async stopWatching(sessionId: string): Promise<void> {
    await apiClient.post(`/api/files/${sessionId}/stop-watching`)
  },

  /**
   * Получение списка недавних файлов
   */
  async getRecent(): Promise<RecentLog[]> {
    const response = await apiClient.get<RecentLog[]>('/api/recent')
    return response.data
  },

  /**
   * Очистка списка недавних файлов
   */
  async clearRecent(): Promise<void> {
    await apiClient.delete('/api/recent')
  }
}
