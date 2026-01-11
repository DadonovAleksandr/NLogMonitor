<script setup lang="ts">
import { ref, computed } from 'vue'
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'
import { useLogStore } from '@/stores'

const logStore = useLogStore()

// Page size options
const pageSizeOptions = [100, 200, 500]

// Dropdown state
const isDropdownOpen = ref(false)

// Toggle dropdown
function toggleDropdown() {
  isDropdownOpen.value = !isDropdownOpen.value
}

// Close dropdown when clicking outside
function closeDropdown() {
  isDropdownOpen.value = false
}

// Select page size
function selectPageSize(size: number) {
  logStore.setPageSize(size)
  closeDropdown()
}

// Navigate to previous page
function previousPage() {
  if (logStore.canPreviousPage) {
    logStore.setPage(logStore.page - 1)
  }
}

// Navigate to next page
function nextPage() {
  if (logStore.canNextPage) {
    logStore.setPage(logStore.page + 1)
  }
}

// Page info text
const pageInfo = computed(() => {
  if (logStore.totalPages === 0) {
    return 'Страница 0 из 0'
  }
  return `Страница ${logStore.page} из ${logStore.totalPages}`
})
</script>

<template>
  <div class="pagination-container">
    <!-- Left side: Page size selector -->
    <div class="pagination-left">
      <span class="pagination-label">Показать:</span>
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

        <!-- Dropdown menu -->
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
    </div>

    <!-- Center: Page info -->
    <div class="pagination-center">
      <span class="page-info">{{ pageInfo }}</span>
    </div>

    <!-- Right side: Navigation buttons -->
    <div class="pagination-right">
      <button
        type="button"
        class="nav-btn"
        :disabled="!logStore.canPreviousPage"
        @click="previousPage"
      >
        <ChevronLeft class="nav-icon" />
        <span>Назад</span>
      </button>
      <button
        type="button"
        class="nav-btn"
        :disabled="!logStore.canNextPage"
        @click="nextPage"
      >
        <span>Вперёд</span>
        <ChevronRight class="nav-icon" />
      </button>
    </div>
  </div>
</template>

<style scoped>
/* Import IBM Plex Mono for technical data */
@import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&display=swap');

.pagination-container {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 8px 16px;
  background: linear-gradient(to bottom, #fafafa 0%, #f5f5f5 100%);
  border-top: 1px solid #e5e5e5;
  box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.04);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  font-size: 12px;
}

.pagination-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.pagination-label {
  font-size: 11px;
  font-weight: 500;
  color: #737373;
}

.page-size-selector {
  position: relative;
}

.page-size-btn {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  min-width: 70px;
  padding: 5px 10px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 6px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.page-size-btn:hover {
  background: #fafafa;
  border-color: #a3a3a3;
  box-shadow: 0 2px 3px rgba(0, 0, 0, 0.06);
}

.page-size-btn:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.dropdown-icon {
  width: 12px;
  height: 12px;
  transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}

.dropdown-icon-open {
  transform: rotate(180deg);
}

.page-size-dropdown {
  position: absolute;
  bottom: 100%;
  left: 0;
  z-index: 50;
  margin-bottom: 4px;
  min-width: 70px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 6px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  animation: slideUp 0.15s ease-out;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(4px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.page-size-option {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  padding: 8px 12px;
  background: transparent;
  border: none;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

.page-size-option:hover {
  background: #f5f5f5;
}

.page-size-option-active {
  background: #dbeafe;
  color: #1e40af;
  font-weight: 600;
}

.pagination-center {
  display: flex;
  align-items: center;
}

.page-info {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 11px;
  font-weight: 500;
  color: #525252;
  tabular-nums: tabular-nums;
}

.pagination-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.nav-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 5px 12px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 500;
  color: #525252;
  cursor: pointer;
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.nav-btn:hover:not(:disabled) {
  background: #fafafa;
  border-color: #a3a3a3;
  box-shadow: 0 2px 3px rgba(0, 0, 0, 0.06);
  transform: translateY(-0.5px);
}

.nav-btn:active:not(:disabled) {
  transform: translateY(0);
}

.nav-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.nav-icon {
  width: 14px;
  height: 14px;
}
</style>
