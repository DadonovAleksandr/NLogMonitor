<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useFilterStore } from '@/stores/filterStore'
import { useTabsStore } from '@/stores/tabsStore'
import { useDebounceFn } from '@vueuse/core'
import { Search, X } from 'lucide-vue-next'
import { Input } from '@/components/ui/input'

const filterStore = useFilterStore()
const tabsStore = useTabsStore()

// Проверка наличия активной вкладки
const hasActiveTab = computed(() => tabsStore.activeTab !== null)

// Локальное значение для немедленного отображения
const localSearchText = ref(filterStore.searchText)

// Debounced функция для обновления store
const debouncedSetSearch = useDebounceFn((value: string) => {
  filterStore.setSearchText(value)
}, 300)

// Следим за изменениями локального значения
watch(localSearchText, (newValue) => {
  debouncedSetSearch(newValue)
})

// Очистка поля поиска
const clearSearch = () => {
  localSearchText.value = ''
  filterStore.setSearchText('')
}
</script>

<template>
  <div class="relative flex items-center group">
    <!-- Иконка поиска -->
    <div
      class="absolute left-3 z-10 flex items-center justify-center pointer-events-none transition-all duration-200"
      :class="localSearchText ? 'text-emerald-400' : 'text-zinc-500 group-focus-within:text-zinc-400'"
    >
      <Search :size="16" :stroke-width="2.5" />
    </div>

    <!-- Input поле -->
    <Input
      v-model="localSearchText"
      type="text"
      :disabled="!hasActiveTab"
      :placeholder="hasActiveTab ? 'Поиск по сообщениям...' : 'Откройте файл для поиска...'"
      :class="[
        'pl-10 pr-10',
        'h-10',
        'font-mono text-[13px] tracking-wide',
        'bg-zinc-950/60 backdrop-blur-sm',
        'border-zinc-800/60',
        'text-zinc-200 placeholder:text-zinc-600',
        'focus-visible:ring-emerald-500/40 focus-visible:border-emerald-700/60',
        'transition-all duration-150',
        localSearchText && 'border-emerald-900/40 shadow-[0_0_12px_rgba(52,211,153,0.1)]',
        !hasActiveTab && 'opacity-50 cursor-not-allowed'
      ]"
    />

    <!-- Кнопка очистки -->
    <Transition
      enter-active-class="transition-all duration-150"
      enter-from-class="opacity-0 scale-75"
      enter-to-class="opacity-100 scale-100"
      leave-active-class="transition-all duration-100"
      leave-from-class="opacity-100 scale-100"
      leave-to-class="opacity-0 scale-75"
    >
      <button
        v-if="localSearchText"
        @click="clearSearch"
        class="absolute right-2 z-10 flex items-center justify-center w-6 h-6 rounded bg-zinc-900/60 hover:bg-red-950/60 border border-zinc-700/50 hover:border-red-800/60 text-zinc-400 hover:text-red-400 transition-all duration-150 active:scale-90"
        aria-label="Очистить поиск"
      >
        <X :size="14" :stroke-width="2.5" />
      </button>
    </Transition>

    <!-- Индикатор debounce (опционально - можно убрать) -->
    <div
      v-if="localSearchText"
      class="absolute -bottom-1 left-10 right-10 h-0.5 bg-gradient-to-r from-transparent via-emerald-500/40 to-transparent animate-pulse"
    />
  </div>
</template>
