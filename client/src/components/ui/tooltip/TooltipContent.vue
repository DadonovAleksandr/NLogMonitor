<script setup lang="ts">
import { computed } from 'vue'
import { TooltipContent, TooltipPortal, type TooltipContentProps } from 'reka-ui'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<TooltipContentProps & { class?: string }>(), {
  sideOffset: 6,
  side: 'bottom'
})

const delegatedProps = computed(() => {
  const { class: _, ...rest } = props
  return rest
})
</script>

<template>
  <TooltipPortal>
    <TooltipContent
      v-bind="delegatedProps"
      :class="cn(
        'z-50 overflow-hidden rounded-md bg-white px-3 py-1.5 text-xs',
        'text-neutral-700 shadow-md border border-neutral-200',
        'animate-in fade-in-0 zoom-in-95',
        'data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=closed]:zoom-out-95',
        'data-[side=bottom]:slide-in-from-top-2',
        'data-[side=left]:slide-in-from-right-2',
        'data-[side=right]:slide-in-from-left-2',
        'data-[side=top]:slide-in-from-bottom-2',
        'font-mono',
        props.class
      )"
    >
      <slot />
    </TooltipContent>
  </TooltipPortal>
</template>
