<script setup lang="ts">
import { ref, computed } from 'vue'
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'
import { useLogStore, useTabsStore } from '@/stores'

const logStore = useLogStore()
const tabsStore = useTabsStore()

const pageSizeOptions = [100, 200, 500]
const isDropdownOpen = ref(false)

const activeTab = computed(() => tabsStore.activeTab)

function toggleDropdown() {
  isDropdownOpen.value = !isDropdownOpen.value
}

function closeDropdown() {
  isDropdownOpen.value = false
}

function selectPageSize(size: number) {
  logStore.setPageSize(size)
  closeDropdown()
}

function previousPage() {
  if (logStore.canPreviousPage) {
    logStore.setPage(logStore.page - 1)
  }
}

function nextPage() {
  if (logStore.canNextPage) {
    logStore.setPage(logStore.page + 1)
  }
}

const pageInfo = computed(() => {
  if (logStore.totalPages === 0) return '0/0'
  return `${logStore.page}/${logStore.totalPages}`
})

const totalCount = computed(() => logStore.totalCount)

const levelStats = computed(() => {
  const counts = activeTab.value?.levelCounts || {
    Trace: 0, Debug: 0, Info: 0, Warn: 0, Error: 0, Fatal: 0
  }
  return counts
})

const hasErrors = computed(() => (levelStats.value.Error || 0) > 0)
const hasFatals = computed(() => (levelStats.value.Fatal || 0) > 0)
const hasWarnings = computed(() => (levelStats.value.Warn || 0) > 0)
</script>

<template>
  <div class="status-bar">
    <!-- Left: Page size + Navigation -->
    <div class="status-left">
      <!-- Page size selector -->
      <div class="page-size-selector">
        <button
          type="button"
          class="page-size-btn"
          @click="toggleDropdown"
          @blur="closeDropdown"
        >
          <span>{{ logStore.pageSize }}</span>
          <svg
            class="dropdown-icon"
            :class="{ 'dropdown-icon-open': isDropdownOpen }"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </button>

        <div v-if="isDropdownOpen" class="page-size-dropdown">
          <button
            v-for="size in pageSizeOptions"
            :key="size"
            type="button"
            class="page-size-option"
            :class="{ 'page-size-option-active': size === logStore.pageSize }"
            @mousedown.prevent="selectPageSize(size)"
          >
            {{ size }}
          </button>
        </div>
      </div>

      <!-- Navigation -->
      <div class="nav-group">
        <button
          type="button"
          class="nav-btn"
          :disabled="!logStore.canPreviousPage"
          @click="previousPage"
        >
          <ChevronLeft class="nav-icon" />
        </button>

        <span class="page-info">{{ pageInfo }}</span>

        <button
          type="button"
          class="nav-btn"
          :disabled="!logStore.canNextPage"
          @click="nextPage"
        >
          <ChevronRight class="nav-icon" />
        </button>
      </div>
    </div>

    <!-- Center: spacer -->
    <div class="status-spacer" />

    <!-- Right: Stats -->
    <div class="status-right">
      <!-- Level stats (compact) -->
      <div class="level-stats">
        <span v-if="hasFatals" class="stat-badge stat-fatal">
          F:{{ levelStats.Fatal }}
        </span>
        <span v-if="hasErrors" class="stat-badge stat-error">
          E:{{ levelStats.Error }}
        </span>
        <span v-if="hasWarnings" class="stat-badge stat-warn">
          W:{{ levelStats.Warn }}
        </span>
      </div>

      <!-- Total -->
      <div class="total-info">
        <span class="total-label">Total:</span>
        <span class="total-value">{{ totalCount.toLocaleString() }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.status-bar {
  display: flex;
  align-items: center;
  height: 28px;
  padding: 0 8px;
  background: linear-gradient(to bottom, #fafafa 0%, #f5f5f5 100%);
  border-top: 1px solid #e5e5e5;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  font-size: 11px;
  gap: 8px;
}

.status-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* Page size selector */
.page-size-selector {
  position: relative;
}

.page-size-btn {
  display: flex;
  align-items: center;
  gap: 4px;
  height: 20px;
  padding: 0 6px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 3px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.12s ease;
}

.page-size-btn:hover {
  background: #fafafa;
  border-color: #a3a3a3;
}

.dropdown-icon {
  width: 10px;
  height: 10px;
  transition: transform 0.15s ease;
}

.dropdown-icon-open {
  transform: rotate(180deg);
}

.page-size-dropdown {
  position: absolute;
  bottom: 100%;
  left: 0;
  z-index: 50;
  margin-bottom: 2px;
  min-width: 50px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 4px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.page-size-option {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  padding: 4px 8px;
  background: transparent;
  border: none;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.12s ease;
}

.page-size-option:hover {
  background: #f5f5f5;
}

.page-size-option-active {
  background: #dbeafe;
  color: #1e40af;
  font-weight: 600;
}

/* Navigation */
.nav-group {
  display: flex;
  align-items: center;
  gap: 4px;
}

.nav-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 3px;
  color: #525252;
  cursor: pointer;
  transition: all 0.12s ease;
}

.nav-btn:hover:not(:disabled) {
  background: #fafafa;
  border-color: #a3a3a3;
}

.nav-btn:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.nav-icon {
  width: 12px;
  height: 12px;
}

.page-info {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px;
  font-weight: 500;
  color: #525252;
  min-width: 40px;
  text-align: center;
  font-variant-numeric: tabular-nums;
}

/* Spacer */
.status-spacer {
  flex: 1;
}

/* Right side */
.status-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* Level stats */
.level-stats {
  display: flex;
  align-items: center;
  gap: 4px;
}

.stat-badge {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 9px;
  font-weight: 600;
  padding: 1px 4px;
  border-radius: 2px;
  font-variant-numeric: tabular-nums;
}

.stat-fatal {
  background: #8B0000;
  color: #ffffff;
}

.stat-error {
  background: #FF0000;
  color: #ffffff;
}

.stat-warn {
  background: #FFFF00;
  color: #000000;
}

/* Total */
.total-info {
  display: flex;
  align-items: center;
  gap: 4px;
}

.total-label {
  font-size: 10px;
  font-weight: 500;
  color: #737373;
}

.total-value {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px;
  font-weight: 600;
  color: #171717;
  font-variant-numeric: tabular-nums;
}
</style>
