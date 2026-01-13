<script setup lang="ts">
import { TooltipRoot } from 'reka-ui'
import type { TooltipRootEmits, TooltipRootProps } from 'reka-ui'
import { useForwardPropsEmits } from 'reka-ui'

const props = withDefaults(defineProps<TooltipRootProps>(), {
  delayDuration: 300
})
const emits = defineEmits<TooltipRootEmits>()

const forwarded = useForwardPropsEmits(props, emits)
</script>

<template>
  <TooltipRoot v-bind="forwarded">
    <slot />
  </TooltipRoot>
</template>
