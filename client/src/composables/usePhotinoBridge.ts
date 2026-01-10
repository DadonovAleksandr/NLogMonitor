import { ref, readonly, onMounted } from 'vue'

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

/**
 * Composable for Photino Desktop bridge
 */
export function usePhotinoBridge() {
  const isDesktop = ref(false)
  const serverPort = ref<number | null>(null)
  const isReady = ref(false)

  // Initialize on mount
  onMounted(async () => {
    isDesktop.value = checkIsDesktop()

    if (isDesktop.value) {
      initializeBridge()
      try {
        serverPort.value = await sendCommand<number>('getServerPort')
      } catch {
        // Ignore errors getting server port
      }
      isReady.value = true
    } else {
      isReady.value = true
    }
  })

  /**
   * Show native file open dialog
   * @returns Selected file path or null if cancelled
   */
  async function showOpenFileDialog(): Promise<string | null> {
    if (!isDesktop.value) {
      throw new Error('Native dialogs only available in Desktop mode')
    }
    return sendCommand<string | null>('showOpenFile')
  }

  /**
   * Show native folder open dialog
   * @returns Selected folder path or null if cancelled
   */
  async function showOpenFolderDialog(): Promise<string | null> {
    if (!isDesktop.value) {
      throw new Error('Native dialogs only available in Desktop mode')
    }
    return sendCommand<string | null>('showOpenFolder')
  }

  return {
    isDesktop: readonly(isDesktop),
    serverPort: readonly(serverPort),
    isReady: readonly(isReady),
    showOpenFileDialog,
    showOpenFolderDialog
  }
}

export default usePhotinoBridge
