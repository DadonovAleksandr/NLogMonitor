import { ref } from 'vue'

export interface Toast {
  id: string
  message: string
  type: 'error' | 'success' | 'info'
  timestamp: number
}

const toasts = ref<Toast[]>([])
let idCounter = 0

export function useToast() {
  const showToast = (message: string, type: Toast['type'] = 'info') => {
    const id = `toast-${++idCounter}-${Date.now()}`
    const toast: Toast = {
      id,
      message,
      type,
      timestamp: Date.now()
    }

    toasts.value.push(toast)

    // Auto-dismiss через 5 секунд
    setTimeout(() => {
      hideToast(id)
    }, 5000)

    return id
  }

  const hideToast = (id: string) => {
    const index = toasts.value.findIndex(t => t.id === id)
    if (index !== -1) {
      toasts.value.splice(index, 1)
    }
  }

  return {
    toasts,
    showToast,
    hideToast
  }
}
