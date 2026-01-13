import { ref, readonly } from 'vue'
import { setBaseUrlFromPort, markAsInitialized, waitForInit } from '@/api/config'

// Type declarations for Photino bridge
declare global {
  interface External {
    sendMessage(message: string): void
    receiveMessage(callback: (message: string) => void): void
  }
}

interface BridgeRequest {
  requestId: string
  command: string
  data?: unknown
}

interface BridgeResponse {
  requestId: string | null
  success: boolean
  data?: unknown
  error?: string
}

// Pending promises for bridge requests
const pendingRequests = new Map<string, {
  resolve: (value: unknown) => void
  reject: (reason: Error) => void
}>()

// Generate unique request ID
function generateRequestId(): string {
  return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
}

// Check if running in Photino Desktop
function checkIsDesktop(): boolean {
  return typeof window !== 'undefined' &&
         typeof window.external !== 'undefined' &&
         typeof window.external.sendMessage === 'function'
}

// Initialize bridge response handler
let bridgeInitialized = false
function initializeBridge() {
  if (bridgeInitialized || !checkIsDesktop()) return

  window.external.receiveMessage((message: string) => {
    try {
      const response: BridgeResponse = JSON.parse(message)

      if (response.requestId && pendingRequests.has(response.requestId)) {
        const { resolve, reject } = pendingRequests.get(response.requestId)!
        pendingRequests.delete(response.requestId)

        if (response.success) {
          resolve(response.data)
        } else {
          reject(new Error(response.error || 'Unknown bridge error'))
        }
      }
    } catch (error) {
      console.error('Failed to parse bridge response:', error)
    }
  })

  bridgeInitialized = true
}

// Send command to .NET backend
async function sendCommand<T = unknown>(command: string, data?: unknown): Promise<T> {
  if (!checkIsDesktop()) {
    throw new Error('Not running in Photino Desktop mode')
  }

  initializeBridge()

  const requestId = generateRequestId()
  const request: BridgeRequest = {
    requestId,
    command,
    data
  }

  return new Promise<T>((resolve, reject) => {
    pendingRequests.set(requestId, {
      resolve: resolve as (value: unknown) => void,
      reject
    })

    // Set timeout for request
    setTimeout(() => {
      if (pendingRequests.has(requestId)) {
        pendingRequests.delete(requestId)
        reject(new Error('Bridge request timeout'))
      }
    }, 30000)

    window.external.sendMessage(JSON.stringify(request))
  })
}

// Глобальное состояние для синглтона
const globalIsDesktop = ref(false)
const globalServerPort = ref<number | null>(null)
const globalIsReady = ref(false)
let apiInitPromise: Promise<void> | null = null

/**
 * Инициализация API config.
 * Вызывается один раз при старте приложения.
 * В Desktop режиме получает порт через bridge и устанавливает базовый URL.
 * В Web режиме просто помечает API как готовый.
 */
export async function initializeApiConfig(): Promise<void> {
  // Если уже инициализирован, возвращаем существующий promise
  if (apiInitPromise) {
    return apiInitPromise
  }

  apiInitPromise = (async () => {
    globalIsDesktop.value = checkIsDesktop()

    if (globalIsDesktop.value) {
      initializeBridge()
      try {
        const port = await sendCommand<number>('getServerPort')
        globalServerPort.value = port
        setBaseUrlFromPort(port)
        console.info(`[Photino Bridge] Desktop mode initialized, server port: ${port}`)
      } catch (error) {
        console.error('[Photino Bridge] Failed to get server port:', error)
        // Fallback: помечаем как инициализированный с дефолтным URL
        markAsInitialized()
      }
    } else {
      // Web режим - используем дефолтный URL
      markAsInitialized()
      console.info('[Photino Bridge] Web mode initialized')
    }

    globalIsReady.value = true
  })()

  return apiInitPromise
}

/**
 * Ожидание готовности API.
 * Используйте перед первым API вызовом.
 */
export function waitForApiReady(): Promise<void> {
  return waitForInit()
}

/**
 * Проверка, является ли приложение Desktop режимом.
 * Синхронная проверка без ожидания инициализации.
 */
export function isDesktopMode(): boolean {
  return checkIsDesktop()
}

/**
 * Composable for Photino Desktop bridge.
 * Предоставляет реактивное состояние и методы для работы с нативными диалогами.
 */
export function usePhotinoBridge() {
  /**
   * Show native file open dialog
   * @returns Selected file path or null if cancelled
   */
  async function showOpenFileDialog(): Promise<string | null> {
    if (!globalIsDesktop.value) {
      throw new Error('Native dialogs only available in Desktop mode')
    }
    return sendCommand<string | null>('showOpenFile')
  }

  /**
   * Show native folder open dialog
   * @returns Selected folder path or null if cancelled
   */
  async function showOpenFolderDialog(): Promise<string | null> {
    if (!globalIsDesktop.value) {
      throw new Error('Native dialogs only available in Desktop mode')
    }
    return sendCommand<string | null>('showOpenFolder')
  }

  return {
    isDesktop: readonly(globalIsDesktop),
    serverPort: readonly(globalServerPort),
    isReady: readonly(globalIsReady),
    showOpenFileDialog,
    showOpenFolderDialog
  }
}

export default usePhotinoBridge
