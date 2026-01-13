<script setup lang="ts">
/**
 * Пример использования ExportButton компонента
 *
 * Компонент автоматически интегрируется с:
 * - logStore.sessionId — для экспорта текущей сессии
 * - filterStore.filterOptions — экспорт с учётом активных фильтров
 *
 * Кнопка disabled если нет активной сессии.
 */
import { ExportButton } from '@/components/ExportButton'
</script>

<template>
  <div class="p-8 bg-zinc-950">
    <!-- Просто добавьте компонент -->
    <ExportButton />

    <!-- Или в контейнере с другими элементами -->
    <div class="flex items-center gap-4">
      <span class="font-mono text-zinc-400">Actions:</span>
      <ExportButton />
      <button class="font-mono text-zinc-400 hover:text-zinc-200">
        Close Session
      </button>
    </div>
  </div>
</template>

<style scoped>
/**
 * ExportButton имеет встроенные стили terminal/brutalist дизайна:
 * - Моноширинный шрифт
 * - Emerald accent (#10b981)
 * - Острые углы (без border-radius на кнопке)
 * - Box-shadow с emerald цветом на dropdown
 * - Transitions для hover states
 *
 * Стили НЕ требуют дополнительной настройки.
 */
</style>
