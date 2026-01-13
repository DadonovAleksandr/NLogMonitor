<script setup lang="ts">
import { TabsTrigger, type TabsTriggerProps } from 'reka-ui'
import { computed, type HTMLAttributes } from 'vue'
import { cn } from '@/lib/utils'

const props = defineProps<TabsTriggerProps & { class?: HTMLAttributes['class'] }>()

const delegatedProps = computed(() => {
  const { class: _, ...delegated } = props

  return delegated
})
</script>

<template>
  <TabsTrigger
    v-bind="delegatedProps"
    :class="cn(
      'group relative inline-flex items-center justify-center gap-2 whitespace-nowrap px-4 py-2 font-mono text-sm font-medium transition-all',
      'text-zinc-500 hover:text-zinc-300',
      'data-[state=active]:text-emerald-400 data-[state=active]:shadow-sm',
      'disabled:pointer-events-none disabled:opacity-50',
      'after:absolute after:bottom-0 after:left-0 after:right-0 after:h-0.5 after:bg-emerald-500 after:transition-transform after:duration-200',
      'after:scale-x-0 data-[state=active]:after:scale-x-100',
      props.class
    )"
  >
    <slot />
  </TabsTrigger>
</template>
