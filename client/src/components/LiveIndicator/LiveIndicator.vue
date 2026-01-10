<script setup lang="ts">
import { computed } from 'vue'
import { Activity, WifiOff, Wifi } from 'lucide-vue-next'
import type { ConnectionState } from '@/types'

interface Props {
  /** Состояние подключения SignalR */
  connectionState: ConnectionState
  /** Флаг активного отслеживания файла */
  isWatching: boolean
}

const props = defineProps<Props>()

interface StatusConfig {
  label: string
  icon: typeof Activity | typeof Wifi | typeof WifiOff
  bg: string
  text: string
  border: string
  pulse: boolean
  iconClass?: string
}

// Конфигурации статусов
const statusConfigs = {
  // Активное отслеживание файла (Live mode)
  live: {
    label: 'Live',
    icon: Activity,
    bg: 'bg-emerald-950/60',
    text: 'text-emerald-400',
    border: 'border-emerald-500/30',
    pulse: true,
    iconClass: 'text-emerald-500'
  } as StatusConfig,
  // Подключение устанавливается
  connecting: {
    label: 'Connecting',
    icon: Wifi,
    bg: 'bg-sky-950/60',
    text: 'text-sky-400',
    border: 'border-sky-500/30',
    pulse: false
  } as StatusConfig,
  // Переподключение
  reconnecting: {
    label: 'Reconnecting',
    icon: Wifi,
    bg: 'bg-amber-950/60',
    text: 'text-amber-400',
    border: 'border-amber-500/30',
    pulse: true,
    iconClass: 'text-amber-500'
  } as StatusConfig,
  // Отключен
  disconnected: {
    label: 'Disconnected',
    icon: WifiOff,
    bg: 'bg-zinc-900/60',
    text: 'text-zinc-500',
    border: 'border-zinc-700/30',
    pulse: false
  } as StatusConfig
} as const

// Определяем текущий статус на основе пропсов
const currentStatus = computed((): StatusConfig => {
  // Live mode - когда файл отслеживается и подключение активно
  if (props.isWatching && props.connectionState === 'Connected') {
    return statusConfigs.live
  }

  // Состояния подключения
  if (props.connectionState === 'Connecting') {
    return statusConfigs.connecting
  }

  if (props.connectionState === 'Reconnecting') {
    return statusConfigs.reconnecting
  }

  // По умолчанию - отключен
  return statusConfigs.disconnected
})

const IconComponent = computed(() => currentStatus.value.icon)
</script>

<template>
  <div
    :class="[
      'inline-flex items-center gap-2 px-3 py-1.5 rounded-lg',
      'border transition-all duration-300',
      'font-mono text-xs font-medium',
      currentStatus.bg,
      currentStatus.text,
      currentStatus.border
    ]"
  >
    <!-- Icon with optional pulse animation -->
    <div class="relative flex items-center justify-center">
      <component
        :is="IconComponent"
        :class="[
          'h-3.5 w-3.5 transition-transform duration-300',
          currentStatus.iconClass || ''
        ]"
      />

      <!-- Pulse ring for Live/Reconnecting states -->
      <div
        v-if="currentStatus.pulse"
        :class="[
          'absolute inset-0 rounded-full animate-ping',
          isWatching ? 'bg-emerald-500/40' : 'bg-amber-500/40'
        ]"
      />
    </div>

    <!-- Status label -->
    <span class="tracking-wide">{{ currentStatus.label }}</span>
  </div>
</template>

<style scoped>
/* Пульсация для Live индикатора */
@keyframes pulse {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.3;
  }
}

.animate-ping {
  animation: ping 1.5s cubic-bezier(0, 0, 0.2, 1) infinite;
}

@keyframes ping {
  0% {
    transform: scale(1);
    opacity: 0.8;
  }
  75%,
  100% {
    transform: scale(1.8);
    opacity: 0;
  }
}
</style>
