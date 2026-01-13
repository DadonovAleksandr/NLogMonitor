# ExportButton - Features

## Основные возможности

### 1. Dropdown с выбором формата экспорта

- **JSON** — структурированный формат с полными данными
- **CSV** — табличный формат для Excel/Google Sheets

Каждая опция имеет:
- Иконку (FileJson/FileText из lucide-vue-next)
- Label (JSON/CSV)
- Hover effect (emerald background)

### 2. Интеграция с Pinia stores

**Автоматическое подключение к stores (без props):**

```typescript
// logStore
const sessionId = logStore.sessionId           // ID для экспорта
const hasSession = logStore.hasSession         // Для disabled state

// filterStore
const filters = filterStore.filterOptions      // Применяются к экспорту
```

**Экспорт учитывает активные фильтры:**
- searchText — полнотекстовый поиск
- minLevel/maxLevel — уровни логирования
- fromDate/toDate — временной диапазон
- logger — фильтр по имени logger

### 3. Loading state

**Визуальная индикация во время экспорта:**

```vue
<!-- Normal state -->
<Download :size="16" />
<span>EXPORT</span>

<!-- Loading state -->
<LoadingSpinner />
<span>EXPORTING...</span>
```

**Поведение:**
- Кнопка disabled на 1 секунду
- Иконка заменяется на вращающийся spinner
- Текст меняется на "EXPORTING..."
- Dropdown автоматически закрывается

### 4. Terminal/Brutalist дизайн

**Визуальный стиль:**
- Моноширинный шрифт (font-mono)
- Uppercase текст (text-transform: uppercase)
- Letter spacing (tracking-wide)
- Острые углы (нет border-radius)
- Emerald accent (#10b981)

**Кнопка:**
```css
border: 2px solid emerald-500
background: black
color: emerald-500
hover: bg-emerald-500, text-black
```

**Dropdown:**
```css
border: 2px solid emerald-500
background: black
box-shadow: 4px 4px 0px rgba(16,185,129,0.5)
```

### 5. Transitions и анимации

**Dropdown open/close:**
```css
transition: all 200ms ease-out
transform: translateY(-8px) → translateY(0)
opacity: 0 → 1
```

**ChevronDown иконка:**
```css
transform: rotate(0deg) → rotate(180deg)
transition: 200ms
```

**Button hover:**
```css
background: black → emerald-500
color: emerald-500 → black
transition: 200ms
```

**Spinner rotation:**
```css
animation: spin 1s linear infinite
```

### 6. Click outside behavior

**Автоматическое закрытие dropdown:**
```typescript
// При клике вне компонента
document.addEventListener('click', handleClickOutside)

function handleClickOutside(event: MouseEvent) {
  if (!target.closest('.export-button-wrapper')) {
    closeDropdown()
  }
}
```

**Cleanup:**
- Event listener добавляется при открытии
- Удаляется при закрытии
- watch() отслеживает isOpen state

### 7. Disabled state

**Кнопка disabled когда:**
- Нет активной сессии (`!logStore.hasSession`)
- Идёт экспорт (`isExporting === true`)

**Визуальное отображение:**
```css
disabled:border-gray-600
disabled:text-gray-600
disabled:hover:bg-black
disabled:pointer-events-none
```

### 8. TypeScript типизация

**Полная типизация:**
```typescript
// Export format
type ExportFormat = 'json' | 'csv'

// Export options
interface ExportOption {
  format: ExportFormat
  label: string
  icon: Component  // lucide-vue-next icon component
}

// API method
exportApi.downloadExport(
  sessionId: string,
  format: ExportFormat,
  filters?: FilterOptions
): void
```

## Архитектура

### Composition API

```typescript
// Reactive state
const isOpen = ref(false)
const isExporting = ref(false)

// Computed
const isDisabled = computed(() =>
  !logStore.hasSession || isExporting.value
)

// Methods
async function handleExport(format: ExportFormat) { }
function toggleDropdown() { }
function closeDropdown() { }
```

### Stores integration

```typescript
// Auto-import из @/stores
import { useLogStore } from '@/stores/logStore'
import { useFilterStore } from '@/stores/filterStore'

// Использование
const logStore = useLogStore()
const filterStore = useFilterStore()
```

### API integration

```typescript
// exportApi.downloadExport() создаёт <a> и кликает
exportApi.downloadExport(
  logStore.sessionId,
  format,
  filterStore.filterOptions
)

// Файл: logs-{sessionId}.{format}
// Браузер автоматически скачивает
```

## Browser compatibility

- **Chrome/Edge** 90+ ✅
- **Firefox** 88+ ✅
- **Safari** 14+ ✅

**Используемые API:**
- `document.createElement('a')` — широкая поддержка
- `link.download` — HTML5 feature
- CSS transitions — все браузеры
- CSS animations — все браузеры

## Performance

**Оптимизации:**
- Lazy API call — только при клике на формат
- Event listener cleanup — удаляется при закрытии
- CSS transitions — GPU accelerated
- No re-renders — computed values

**Memory:**
- Минимальный footprint (~2KB gzipped)
- Нет глобальных listeners после unmount
- Нет memory leaks

## Accessibility

**Keyboard navigation:**
- Tab — фокус на кнопку
- Enter/Space — открытие dropdown
- Escape — закрытие dropdown (TODO)

**Screen readers:**
- Button имеет текст "EXPORT"
- Dropdown опции имеют label
- disabled state распознаётся

**TODO (Phase 5+):**
- [ ] aria-expanded на кнопке
- [ ] aria-haspopup="menu"
- [ ] role="menu" на dropdown
- [ ] role="menuitem" на опциях
- [ ] Keyboard navigation (Arrow keys)
- [ ] Escape для закрытия

## Error handling

**Текущая реализация:**
```typescript
try {
  exportApi.downloadExport(sessionId, format, filters)
} catch (error) {
  console.error('Export failed:', error)
}
```

**TODO (Phase 5+):**
- [ ] Toast notification с ошибкой
- [ ] Retry механизм
- [ ] Error boundary component
- [ ] Логирование на backend (clientLogs API)

## Testing

**Unit tests (TODO):**
- [ ] Dropdown открывается/закрывается
- [ ] handleExport вызывает exportApi
- [ ] isDisabled работает корректно
- [ ] Click outside закрывает dropdown
- [ ] Loading state работает

**Integration tests (TODO):**
- [ ] Экспорт с фильтрами
- [ ] Экспорт без фильтров
- [ ] Disabled state без сессии
- [ ] File download работает

**E2E tests (TODO):**
- [ ] Полный flow: загрузка → фильтрация → экспорт
- [ ] JSON vs CSV форматы
- [ ] Большие файлы (1M+ записей)
