import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { RecentLog } from '@/types'
import { filesApi } from '@/api'

export const useRecentStore = defineStore('recent', () => {
  // State
  const recentLogs = ref<RecentLog[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Actions
  async function fetchRecent() {
    isLoading.value = true
    error.value = null

    try {
      recentLogs.value = await filesApi.getRecent()
    } catch (err: unknown) {
      error.value = (err as { message?: string }).message || 'Failed to fetch recent logs'
    } finally {
      isLoading.value = false
    }
  }

  async function clearRecent() {
    isLoading.value = true
    error.value = null

    try {
      await filesApi.clearRecent()
      recentLogs.value = []
    } catch (err: unknown) {
      error.value = (err as { message?: string }).message || 'Failed to clear recent logs'
    } finally {
      isLoading.value = false
    }
  }

  return {
    // State
    recentLogs,
    isLoading,
    error,
    // Actions
    fetchRecent,
    clearRecent
  }
})
