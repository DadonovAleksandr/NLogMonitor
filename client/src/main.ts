import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import App from './App.vue'
import { logger } from '@/services/logger'
import { initializeApiConfig } from '@/composables/usePhotinoBridge'

// Асинхронная инициализация приложения
async function bootstrap() {
  // ВАЖНО: Инициализируем API config ПЕРЕД созданием Vue приложения.
  // В Desktop режиме это получает порт embedded сервера и устанавливает базовый URL.
  // В Web режиме просто помечает API как готовый (использует дефолтный URL).
  await initializeApiConfig()

  const app = createApp(App)
  const pinia = createPinia()

  // Инициализация глобальных error handlers
  logger.initErrorHandlers()

  // Vue error handler для перехвата ошибок в компонентах
  app.config.errorHandler = logger.createVueErrorHandler()

  // Установка глобального контекста (опционально)
  // logger.setGlobalContext({
  //   version: import.meta.env.VITE_APP_VERSION || '1.0.0'
  // })

  // Отправка логов при закрытии страницы
  if (typeof window !== 'undefined') {
    window.addEventListener('beforeunload', () => {
      logger.forceFlush()
    })

    // Также при visibility change (для мобильных устройств)
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') {
        logger.forceFlush()
      }
    })
  }

  app.use(pinia)
  app.mount('#app')

  // Логируем успешную инициализацию приложения
  logger.info('Application initialized')
}

// Запуск приложения
bootstrap().catch((error) => {
  console.error('Failed to initialize application:', error)
})
