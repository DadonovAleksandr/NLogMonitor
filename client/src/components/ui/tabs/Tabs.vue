<script setup lang="ts">
import { TabsRoot, type TabsRootEmits, type TabsRootProps } from 'reka-ui'
import { computed, type HTMLAttributes } from 'vue'
import { cn } from '@/lib/utils'

const props = defineProps<TabsRootProps & { class?: HTMLAttributes['class'] }>()
const emits = defineEmits<TabsRootEmits>()

const delegatedProps = computed(() => {
  const { class: _, ...delegated } = props

  return delegated
})
</script>

<template>
  <TabsRoot v-bind="delegatedProps" :class="cn('', props.class)" @update:model-value="emits('update:modelValue', $event)">
    <slot />
  </TabsRoot>
</template>
