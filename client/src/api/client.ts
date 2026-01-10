import axios from 'axios'
import type { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios'
import type { ApiError } from '@/types'
import { getBaseUrl } from './config'

// Создание axios instance с базовыми настройками
// ВАЖНО: baseURL устанавливается динамически через interceptor для поддержки Desktop режима
export const apiClient: AxiosInstance = axios.create({
  timeout: 30000
  // Не устанавливаем Content-Type здесь - axios сам определит правильный тип
  // для JSON отправит application/json, для FormData - multipart/form-data
})

// Интерцептор для динамического baseURL (поддержка Desktop режима с динамическим портом)
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Устанавливаем baseURL динамически при каждом запросе
    config.baseURL = getBaseUrl()
    return config
  },
  (error) => Promise.reject(error)
)

// Интерцептор для обработки ошибок
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiError>) => {
    // Преобразуем ошибку в понятный формат
    const apiError: ApiError = {
      message: 'An unexpected error occurred',
      details: undefined,
      traceId: undefined
    }

    if (error.response?.data) {
      apiError.message = error.response.data.message || apiError.message
      apiError.details = error.response.data.details
      apiError.traceId = error.response.data.traceId
    } else if (error.message) {
      apiError.message = error.message
    }

    // Добавляем HTTP статус в сообщение при необходимости
    if (error.response?.status === 404) {
      apiError.message = 'Resource not found'
    } else if (error.response?.status === 413) {
      apiError.message = 'File too large'
    } else if (error.response?.status === 501) {
      apiError.message = 'Feature not implemented'
    }

    return Promise.reject(apiError)
  }
)

export default apiClient
