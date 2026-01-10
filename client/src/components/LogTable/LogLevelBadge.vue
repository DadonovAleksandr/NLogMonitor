<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  level: string
}

const props = defineProps<Props>()

const defaultConfig = {
  bg: 'bg-zinc-700/50',
  text: 'text-zinc-400',
  glow: '',
  label: 'TRC'
}

const configs: Record<string, { bg: string; text: string; glow: string; label: string }> = {
  Trace: defaultConfig,
  Debug: {
    bg: 'bg-sky-950/60',
    text: 'text-sky-400',
    glow: 'shadow-[0_0_8px_rgba(56,189,248,0.15)]',
    label: 'DBG'
  },
  Info: {
    bg: 'bg-emerald-950/60',
    text: 'text-emerald-400',
    glow: 'shadow-[0_0_8px_rgba(52,211,153,0.15)]',
    label: 'INF'
  },
  Warn: {
    bg: 'bg-amber-950/60',
    text: 'text-amber-400',
    glow: 'shadow-[0_0_8px_rgba(251,191,36,0.2)]',
    label: 'WRN'
  },
  Error: {
    bg: 'bg-red-950/70',
    text: 'text-red-400',
    glow: 'shadow-[0_0_10px_rgba(248,113,113,0.25)]',
    label: 'ERR'
  },
  Fatal: {
    bg: 'bg-fuchsia-950/70',
    text: 'text-fuchsia-300 font-bold',
    glow: 'shadow-[0_0_12px_rgba(232,121,249,0.35)]',
    label: 'FTL'
  }
}

const levelConfig = computed(() => configs[props.level] ?? defaultConfig)
</script>

<template>
  <span
    :class="[
      'inline-flex items-center justify-center',
      'px-2 py-0.5 rounded',
      'font-mono text-[11px] tracking-wider uppercase',
      'border border-current/20',
      'transition-all duration-200',
      levelConfig.bg,
      levelConfig.text,
      levelConfig.glow
    ]"
  >
    {{ levelConfig.label }}
  </span>
</template>
