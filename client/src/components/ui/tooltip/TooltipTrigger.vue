<script setup lang="ts">
import { TooltipTrigger, type TooltipTriggerProps } from 'reka-ui'
import { useForwardProps } from 'reka-ui'

const props = defineProps<TooltipTriggerProps>()

const forwarded = useForwardProps(props)
</script>

<template>
  <TooltipTrigger v-bind="forwarded">
    <slot />
  </TooltipTrigger>
</template>
