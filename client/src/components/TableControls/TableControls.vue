<script setup lang="ts">
import { computed } from 'vue'
import { ArrowDown, Trash2, Pause, Play } from 'lucide-vue-next'
import { useTabsStore } from '@/stores'

const tabsStore = useTabsStore()

const emit = defineEmits<{
  (e: 'clear'): void
}>()

const activeTab = computed(() => tabsStore.activeTab)

const isAutoscroll = computed(() => activeTab.value?.autoscroll || false)
const isPaused = computed(() => activeTab.value?.isPaused || false)

function toggleAutoscroll() {
  tabsStore.toggleAutoscroll()
}

function togglePause() {
  tabsStore.togglePause()
}

function handleClear() {
  emit('clear')
  tabsStore.clearLogs()
}
</script>

<template>
  <div class="table-controls">
    <div class="controls-group">
      <!-- Autoscroll Toggle -->
      <button
        class="control-btn"
        :class="{ 'control-btn-active': isAutoscroll }"
        :data-type="isAutoscroll ? 'autoscroll-active' : 'autoscroll'"
        @click="toggleAutoscroll"
      >
        <ArrowDown
          class="control-icon"
          :class="{ 'animate-pulse': isAutoscroll }"
        />
        <span class="control-label">Автопрокрутка</span>
      </button>

      <!-- Pause/Resume Toggle -->
      <button
        class="control-btn"
        :class="{ 'control-btn-active': isPaused }"
        :data-type="isPaused ? 'pause-active' : 'pause'"
        @click="togglePause"
      >
        <Pause v-if="!isPaused" class="control-icon" />
        <Play v-else class="control-icon animate-pulse" />
        <span class="control-label">{{ isPaused ? 'Продолжить' : 'Пауза' }}</span>
      </button>

      <!-- Clear Button -->
      <button
        class="control-btn"
        data-type="clear"
        @click="handleClear"
      >
        <Trash2 class="control-icon" />
        <span class="control-label">Очистить</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
/* Import IBM Plex Mono for technical data */
@import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&display=swap');

.table-controls {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 16px;
  background: linear-gradient(to bottom, #fafafa 0%, #f5f5f5 100%);
  border-bottom: 1px solid #e5e5e5;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
}

.controls-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.control-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 5px 12px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

.control-btn:hover {
  background: #f5f5f5;
  border-color: #a3a3a3;
}

/* Active state for autoscroll */
.control-btn-active[data-type="autoscroll-active"] {
  background: #dbeafe;
  border-color: #60a5fa;
  color: #1e40af;
}

/* Active state for pause */
.control-btn-active[data-type="pause-active"] {
  background: #fef3c7;
  border-color: #fbbf24;
  color: #92400e;
}

/* Clear button hover */
.control-btn[data-type="clear"]:hover {
  background: #fee2e2;
  border-color: #fca5a5;
  color: #991b1b;
}

.control-icon {
  width: 14px;
  height: 14px;
  opacity: 0.8;
}

.control-btn-active .control-icon {
  opacity: 1;
}

.control-label {
  font-weight: 500;
}

/* Smooth animations */
@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-4px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.control-btn {
  animation: slideIn 0.2s ease-out backwards;
}

.control-btn:nth-child(1) { animation-delay: 0ms; }
.control-btn:nth-child(2) { animation-delay: 30ms; }
.control-btn:nth-child(3) { animation-delay: 60ms; }
</style>
