import { ref } from 'vue'

export interface Toast {
  id: string
  message: string
  type: 'error' | 'success' | 'info' | 'warning'
  timestamp: number
  duration: number
  isPaused: boolean
  timeoutId?: number
  startTime: number
  remainingTime: number
}

const toasts = ref<Toast[]>([])
let idCounter = 0

export function useToast() {
  const showToast = (
    message: string,
    type: Toast['type'] = 'info',
    duration: number = 5000
  ) => {
    const id = `toast-${++idCounter}-${Date.now()}`
    const now = Date.now()
    const toast: Toast = {
      id,
      message,
      type,
      timestamp: now,
      duration,
      isPaused: false,
      startTime: now,
      remainingTime: duration
    }

    toasts.value.push(toast)

    // Auto-dismiss
    const timeoutId = window.setTimeout(() => {
      hideToast(id)
    }, duration)

    toast.timeoutId = timeoutId

    return id
  }

  const hideToast = (id: string) => {
    const index = toasts.value.findIndex(t => t.id === id)
    if (index === -1) return

    const toast = toasts.value[index]
    if (toast?.timeoutId) {
      clearTimeout(toast.timeoutId)
    }
    toasts.value.splice(index, 1)
  }

  const pauseToast = (id: string) => {
    const toast = toasts.value.find(t => t.id === id)
    if (!toast || toast.isPaused) return

    toast.isPaused = true
    if (toast.timeoutId) {
      clearTimeout(toast.timeoutId)
    }
    toast.remainingTime = toast.duration - (Date.now() - toast.startTime)
  }

  const resumeToast = (id: string) => {
    const toast = toasts.value.find(t => t.id === id)
    if (!toast || !toast.isPaused) return

    toast.isPaused = false
    toast.startTime = Date.now()

    const timeoutId = window.setTimeout(() => {
      hideToast(id)
    }, toast.remainingTime)

    toast.timeoutId = timeoutId
  }

  return {
    toasts,
    showToast,
    hideToast,
    pauseToast,
    resumeToast
  }
}
