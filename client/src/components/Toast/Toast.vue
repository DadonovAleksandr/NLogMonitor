<script setup lang="ts">
import { useToast } from '@/composables/useToast'
import { AlertCircle, CheckCircle, Info, X } from 'lucide-vue-next'

const { toasts, hideToast } = useToast()

const getIcon = (type: 'error' | 'success' | 'info') => {
  switch (type) {
    case 'error':
      return AlertCircle
    case 'success':
      return CheckCircle
    case 'info':
      return Info
  }
}
</script>

<template>
  <div class="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-none">
    <TransitionGroup name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="pointer-events-auto flex items-start gap-3 border-2 border-black dark:border-white bg-white dark:bg-black px-4 py-3 shadow-[4px_4px_0px_0px_rgba(0,0,0,1)] dark:shadow-[4px_4px_0px_0px_rgba(255,255,255,1)] min-w-[320px] max-w-[480px]"
        :class="{
          'border-l-8 border-l-red-500': toast.type === 'error',
          'border-l-8 border-l-green-500': toast.type === 'success',
          'border-l-8 border-l-blue-500': toast.type === 'info'
        }"
      >
        <component
          :is="getIcon(toast.type)"
          class="w-5 h-5 flex-shrink-0 mt-0.5"
          :class="{
            'text-red-500': toast.type === 'error',
            'text-green-500': toast.type === 'success',
            'text-blue-500': toast.type === 'info'
          }"
        />
        <p class="flex-1 text-sm font-mono leading-relaxed text-black dark:text-white break-words">
          {{ toast.message }}
        </p>
        <button
          @click="hideToast(toast.id)"
          class="flex-shrink-0 p-0.5 hover:bg-black/10 dark:hover:bg-white/10 transition-colors"
          aria-label="Close toast"
        >
          <X class="w-4 h-4 text-black dark:text-white" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(100%) scale(0.95);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(100%) scale(0.95);
}

.toast-move {
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
</style>
