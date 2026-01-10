/**
 * Централизованное управление базовым URL для API.
 * В Desktop режиме URL устанавливается динамически после получения порта через bridge.
 */

// Дефолтный URL для Web режима
const DEFAULT_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

// Текущий базовый URL (может быть изменён в runtime)
let currentBaseUrl = DEFAULT_BASE_URL

// Флаг инициализации (для Desktop режима)
let isInitialized = false

// Promise для ожидания инициализации
let initPromise: Promise<void> | null = null
let initResolve: (() => void) | null = null

/**
 * Получить текущий базовый URL.
 * ВАЖНО: В Desktop режиме вызывайте waitForInit() перед использованием.
 */
export function getBaseUrl(): string {
  return currentBaseUrl
}

/**
 * Установить базовый URL (используется в Desktop режиме после получения порта).
 * @param url - Новый базовый URL (например, 'http://localhost:12345')
 */
export function setBaseUrl(url: string): void {
  currentBaseUrl = url
  isInitialized = true

  // Resolve pending promise если есть
  if (initResolve) {
    initResolve()
    initResolve = null
  }

  console.info(`[API Config] Base URL set to: ${url}`)
}

/**
 * Установить базовый URL по порту (удобный хелпер для Desktop).
 * @param port - Порт embedded сервера
 */
export function setBaseUrlFromPort(port: number): void {
  setBaseUrl(`http://localhost:${port}`)
}

/**
 * Пометить как инициализированный (для Web режима, где URL не меняется).
 */
export function markAsInitialized(): void {
  isInitialized = true
  if (initResolve) {
    initResolve()
    initResolve = null
  }
}

/**
 * Проверить, инициализирован ли API config.
 */
export function isApiInitialized(): boolean {
  return isInitialized
}

/**
 * Ожидать инициализации API config.
 * В Web режиме резолвится сразу.
 * В Desktop режиме ждёт вызова setBaseUrl().
 */
export function waitForInit(): Promise<void> {
  if (isInitialized) {
    return Promise.resolve()
  }

  if (!initPromise) {
    initPromise = new Promise<void>((resolve) => {
      initResolve = resolve
    })
  }

  return initPromise
}

/**
 * Сбросить состояние (для тестов).
 */
export function resetConfig(): void {
  currentBaseUrl = DEFAULT_BASE_URL
  isInitialized = false
  initPromise = null
  initResolve = null
}
