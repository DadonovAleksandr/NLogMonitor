import axios from 'axios'
import type { AxiosInstance, AxiosError } from 'axios'
import type { ApiError } from '@/types'

// Базовый URL API (можно переопределить через env)
export const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

// Создание axios instance с базовыми настройками
export const apiClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  timeout: 30000
  // Не устанавливаем Content-Type здесь - axios сам определит правильный тип
  // для JSON отправит application/json, для FormData - multipart/form-data
})

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
