<script setup lang="ts">
import { useToast } from '@/composables/useToast'
import { AlertTriangle, CheckCircle2, Info, XCircle, X } from 'lucide-vue-next'

const { toasts, hideToast, pauseToast, resumeToast } = useToast()

const getIcon = (type: 'error' | 'success' | 'info' | 'warning') => {
  switch (type) {
    case 'error':
      return XCircle
    case 'success':
      return CheckCircle2
    case 'warning':
      return AlertTriangle
    case 'info':
      return Info
  }
}

const getToastStyles = (type: 'error' | 'success' | 'info' | 'warning') => {
  const styles = {
    success: {
      iconBg: 'bg-emerald-500/20',
      iconColor: 'text-emerald-400',
      iconGlow: 'shadow-emerald-500/50',
      progressBg: 'bg-gradient-to-r from-emerald-500 to-cyan-500',
      border: 'border-emerald-500/30'
    },
    error: {
      iconBg: 'bg-rose-500/20',
      iconColor: 'text-rose-400',
      iconGlow: 'shadow-rose-500/50',
      progressBg: 'bg-gradient-to-r from-rose-500 to-red-500',
      border: 'border-rose-500/30'
    },
    warning: {
      iconBg: 'bg-amber-500/20',
      iconColor: 'text-amber-400',
      iconGlow: 'shadow-amber-500/50',
      progressBg: 'bg-gradient-to-r from-amber-500 to-orange-500',
      border: 'border-amber-500/30'
    },
    info: {
      iconBg: 'bg-cyan-500/20',
      iconColor: 'text-cyan-400',
      iconGlow: 'shadow-cyan-500/50',
      progressBg: 'bg-gradient-to-r from-cyan-500 to-blue-500',
      border: 'border-cyan-500/30'
    }
  }
  return styles[type]
}

const getProgressWidth = (toast: typeof toasts.value[0]) => {
  if (toast.isPaused) {
    return `${(toast.remainingTime / toast.duration) * 100}%`
  }
  return '100%'
}

const getAnimationDuration = (toast: typeof toasts.value[0]) => {
  return `${toast.duration}ms`
}
</script>

<template>
  <div class="fixed top-6 right-6 z-[9999] flex flex-col gap-3 pointer-events-none">
    <TransitionGroup name="toast">
      <div
        v-for="(toast, index) in toasts"
        :key="toast.id"
        class="toast-item pointer-events-auto relative overflow-hidden"
        :style="{ '--stagger-delay': `${index * 50}ms` }"
        @mouseenter="pauseToast(toast.id)"
        @mouseleave="resumeToast(toast.id)"
      >
        <!-- Glass container -->
        <div
          class="group relative flex items-start gap-3 min-w-[320px] max-w-[420px] rounded-xl border backdrop-blur-xl transition-all duration-300 ease-out"
          :class="[
            getToastStyles(toast.type).border,
            'bg-zinc-900/80 hover:bg-zinc-900/90',
            'shadow-2xl hover:shadow-3xl',
            'hover:scale-[1.02] hover:translate-y-[-2px]'
          ]"
        >
          <!-- Content wrapper with padding -->
          <div class="flex items-start gap-3 p-4 flex-1">
            <!-- Icon circle with glow -->
            <div
              class="relative flex-shrink-0 flex items-center justify-center w-10 h-10 rounded-full transition-all duration-300"
              :class="[
                getToastStyles(toast.type).iconBg,
                'group-hover:scale-110'
              ]"
            >
              <!-- Glow effect -->
              <div
                class="absolute inset-0 rounded-full blur-md transition-opacity duration-300 opacity-0 group-hover:opacity-100"
                :class="getToastStyles(toast.type).iconGlow"
              />

              <!-- Icon -->
              <component
                :is="getIcon(toast.type)"
                class="w-5 h-5 relative z-10 transition-transform duration-300 group-hover:scale-110"
                :class="getToastStyles(toast.type).iconColor"
              />
            </div>

            <!-- Message -->
            <p class="flex-1 text-sm font-mono leading-relaxed text-zinc-200 pt-1.5 break-words">
              {{ toast.message }}
            </p>

            <!-- Close button -->
            <button
              @click.stop="hideToast(toast.id)"
              class="flex-shrink-0 p-1 rounded-lg hover:bg-white/10 transition-all duration-200 group/btn"
              aria-label="Close notification"
            >
              <X class="w-4 h-4 text-zinc-400 group-hover/btn:text-zinc-200 transition-colors" />
            </button>
          </div>

          <!-- Progress bar -->
          <div
            v-if="!toast.isPaused"
            class="absolute bottom-0 left-0 right-0 h-1 bg-zinc-800/50 overflow-hidden rounded-b-xl"
          >
            <div
              class="h-full origin-left toast-progress rounded-b-xl"
              :class="getToastStyles(toast.type).progressBg"
              :style="{
                animationDuration: getAnimationDuration(toast),
                width: getProgressWidth(toast)
              }"
            />
          </div>

          <!-- Static progress bar when paused -->
          <div
            v-else
            class="absolute bottom-0 left-0 right-0 h-1 bg-zinc-800/50 overflow-hidden rounded-b-xl"
          >
            <div
              class="h-full rounded-b-xl transition-all duration-200"
              :class="getToastStyles(toast.type).progressBg"
              :style="{ width: getProgressWidth(toast) }"
            />
          </div>

          <!-- Top highlight -->
          <div
            class="absolute top-0 left-0 right-0 h-px bg-gradient-to-r from-transparent via-white/20 to-transparent"
          />
        </div>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
/* Toast entrance/exit animations */
.toast-enter-active {
  transition: all 0.4s cubic-bezier(0.16, 1, 0.3, 1);
  transition-delay: var(--stagger-delay, 0ms);
}

.toast-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 1, 1);
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(100%) scale(0.9);
  filter: blur(4px);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(100%) scale(0.95);
  filter: blur(2px);
}

/* Stagger animation for list reordering */
.toast-move {
  transition: transform 0.4s cubic-bezier(0.16, 1, 0.3, 1);
}

/* Progress bar animation */
@keyframes progress {
  from {
    transform: scaleX(1);
  }
  to {
    transform: scaleX(0);
  }
}

.toast-progress {
  animation: progress linear forwards;
  transform-origin: left;
}

/* Enhanced shadow on hover */
.hover\:shadow-3xl:hover {
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5),
              0 0 40px -12px currentColor;
}

/* Glassmorphism enhancement */
@supports (backdrop-filter: blur(16px)) {
  .backdrop-blur-xl {
    backdrop-filter: blur(16px) saturate(180%);
  }
}
</style>
