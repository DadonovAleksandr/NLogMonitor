# ExportButton - Интеграция и использование

## Обзор

ExportButton — компонент для экспорта логов с dropdown выбором формата (JSON/CSV). Реализован в соответствии с terminal/brutalist эстетикой проекта.

## Созданные файлы

```
client/src/components/ExportButton/
├── ExportButton.vue         # Основной компонент
├── index.ts                 # Re-export
├── README.md                # Документация компонента
└── ExportButton.example.vue # Пример использования
```

## Интеграция

### 1. В App.vue

```vue
<script setup lang="ts">
import { ExportButton } from '@/components/ExportButton'
</script>

<template>
  <!-- В header, рядом с информацией о сессии -->
  <div v-if="logStore.hasSession" class="flex items-center gap-4">
    <div class="flex items-center gap-2">
      <span class="font-mono text-xs text-zinc-500">Entries:</span>
      <span class="font-mono text-sm text-emerald-400">
        {{ logStore.totalCount.toLocaleString() }}
      </span>
    </div>
    <ExportButton />
    <button @click="logStore.clearSession()">Close</button>
  </div>
</template>
```

### 2. Автоматическая интеграция с stores

Компонент автоматически использует:
- **logStore.sessionId** — ID текущей сессии для экспорта
- **logStore.hasSession** — для disabled state кнопки
- **filterStore.filterOptions** — фильтры применяются к экспорту

Никаких props не требуется.

## Функциональность

### Экспорт данных

1. **Dropdown меню** с опциями:
   - JSON (иконка FileJson)
   - CSV (иконка FileText)

2. **Применение фильтров к экспорту:**
   - searchText — полнотекстовый поиск
   - minLevel/maxLevel — диапазон уровней логирования
   - fromDate/toDate — временной диапазон
   - logger — фильтр по имени logger

3. **Скачивание файла:**
   - Используется `exportApi.downloadExport()`
   - Создаётся временная `<a>` ссылка
   - Файл: `logs-{sessionId}.{format}`
   - Браузер автоматически скачивает файл

### Loading state

```vue
<!-- Во время экспорта -->
<Button disabled>
  <LoadingSpinner />
  <span>EXPORTING...</span>
</Button>
```

- Кнопка disabled на 1 секунду после старта экспорта
- Иконка Download → вращающийся spinner
- Текст "EXPORT" → "EXPORTING..."

### Click outside behavior

Dropdown закрывается при клике вне компонента:
```typescript
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  if (!target.closest('.export-button-wrapper')) {
    closeDropdown()
  }
}
```

## Дизайн

### Terminal/Brutalist эстетика

```css
/* Кнопка */
border: 2px solid #10b981 (emerald-500)
background: #000000 (black)
color: #10b981 (emerald-500)
font: mono, uppercase, letter-spacing

/* Hover state */
background: #10b981 → text: #000000

/* Dropdown */
border: 2px solid #10b981
box-shadow: 4px 4px 0px rgba(16,185,129,0.5)

/* No border-radius (острые углы) */
```

### Transitions

- **Dropdown:** fade + slide (200ms ease-out)
- **ChevronDown:** rotate 180° при открытии
- **Button hover:** background/text color (200ms)
- **Spinner:** rotate animation (1s linear infinite)

## API Reference

### exportApi.downloadExport()

```typescript
downloadExport(
  sessionId: string,
  format: ExportFormat,
  filters?: FilterOptions
): void
```

**Параметры:**
- `sessionId` — ID сессии логов
- `format` — `'json'` | `'csv'`
- `filters` — FilterOptions (опционально)

**Filters:**
```typescript
interface FilterOptions {
  searchText?: string
  minLevel?: LogLevel
  maxLevel?: LogLevel
  fromDate?: string  // ISO date
  toDate?: string    // ISO date
  logger?: string
}
```

## Тестирование

### Проверка TypeScript типов

```bash
cd client
npx vue-tsc --noEmit
# ✅ Успешно
```

### Проверка импортов в App.vue

```bash
cd client
npm run build
# ✅ Компонент успешно импортирован
```

### Ручное тестирование

1. Запустить backend: `dotnet run --project src/nLogMonitor.Api`
2. Запустить frontend: `cd client && npm run dev`
3. Загрузить .log файл
4. Кликнуть "EXPORT" → должен открыться dropdown
5. Выбрать "JSON" или "CSV" → файл должен скачаться
6. Проверить loading state (кнопка disabled, spinner, текст "EXPORTING...")
7. Применить фильтры → экспорт должен учитывать фильтры

## Зависимости

### Внешние пакеты
- `lucide-vue-next` — иконки (Download, ChevronDown, FileJson, FileText)
- `vue` 3.x — реактивность и composables
- `pinia` — stores

### Внутренние зависимости
- `@/components/ui/button` — shadcn-vue Button
- `@/api/export` — exportApi
- `@/stores/logStore` — session state
- `@/stores/filterStore` — filter state
- `@/types` — TypeScript типы

## Известные ограничения

1. **Нет индикатора прогресса** — большие экспорты (>1M записей) могут занять время
2. **Нет отмены экспорта** — после клика на формат, экспорт нельзя остановить
3. **Нет кэширования** — каждый экспорт генерируется заново на backend
4. **Backend streaming** — файл генерируется на лету, но frontend получает полный response

## Возможные улучшения (Phase 5+)

- [ ] Progress bar для больших экспортов
- [ ] Кнопка отмены экспорта
- [ ] Выбор колонок для экспорта (настройка полей)
- [ ] Экспорт в другие форматы (XML, Excel)
- [ ] Сохранение настроек экспорта в localStorage
- [ ] Toast notification при успешном экспорте
- [ ] Error handling с retry механизмом

## Changelog

### 2026-01-10 - Initial Implementation
- ✅ Dropdown с выбором формата (JSON/CSV)
- ✅ Интеграция с logStore и filterStore
- ✅ Loading state с spinner
- ✅ Terminal/brutalist дизайн
- ✅ Click outside для закрытия dropdown
- ✅ Transitions и hover effects
- ✅ Disabled state для случая без сессии
- ✅ Иконки lucide-vue-next
- ✅ TypeScript типизация
- ✅ Документация и примеры
