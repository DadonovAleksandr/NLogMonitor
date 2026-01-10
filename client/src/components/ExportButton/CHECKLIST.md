# ExportButton — Checklist для тестирования

## Pre-deployment проверки

### ✅ Файловая структура

- [x] `ExportButton.vue` — основной компонент (4.6 KB)
- [x] `index.ts` — re-export компонента
- [x] `README.md` — документация
- [x] `FEATURES.md` — детальное описание возможностей
- [x] `ExportButton.example.vue` — пример использования
- [x] `CHECKLIST.md` — этот файл

### ✅ TypeScript

- [x] Все типы импортированы из `@/types`
- [x] Нет `any` типов
- [x] `vue-tsc --noEmit` проходит без ошибок
- [x] Computed values типизированы
- [x] Props и emits типизированы (нет props/emits в компоненте)

### ✅ Импорты

- [x] `lucide-vue-next` — иконки
- [x] `@/components/ui/button` — Button компонент
- [x] `@/api` — exportApi
- [x] `@/stores/logStore` — logStore
- [x] `@/stores/filterStore` — filterStore
- [x] `@/types` — TypeScript типы

### ✅ Интеграция в App.vue

- [x] Компонент импортирован
- [x] Добавлен в template
- [x] Позиция в header (между Entries и Close)
- [x] Conditional rendering через `v-if="logStore.hasSession"`

---

## Функциональное тестирование

### 1. Dropdown поведение

- [ ] Клик на кнопку открывает dropdown
- [ ] Dropdown показывает 2 опции: JSON и CSV
- [ ] Каждая опция имеет иконку и label
- [ ] ChevronDown иконка поворачивается на 180° при открытии
- [ ] Клик вне компонента закрывает dropdown
- [ ] Повторный клик на кнопку закрывает dropdown

### 2. Экспорт JSON

- [ ] Клик на "JSON" закрывает dropdown
- [ ] Показывается loading state (spinner, "EXPORTING...")
- [ ] Файл скачивается с именем `logs-{sessionId}.json`
- [ ] Файл содержит валидный JSON
- [ ] JSON содержит все записи (или отфильтрованные)

### 3. Экспорт CSV

- [ ] Клик на "CSV" закрывает dropdown
- [ ] Показывается loading state
- [ ] Файл скачивается с именем `logs-{sessionId}.csv`
- [ ] CSV имеет header строку с колонками
- [ ] CSV содержит все записи (или отфильтрованные)
- [ ] CSV корректно открывается в Excel/Google Sheets

### 4. Фильтры

**Без фильтров:**
- [ ] Экспорт содержит все записи

**С search фильтром:**
- [ ] Установить filterStore.searchText = "error"
- [ ] Экспорт содержит только записи с "error"

**С level фильтром:**
- [ ] Установить filterStore.minLevel = LogLevel.Error
- [ ] Экспорт содержит только Error и Fatal записи

**С date фильтром:**
- [ ] Установить filterStore.fromDate и toDate
- [ ] Экспорт содержит только записи в диапазоне

**С logger фильтром:**
- [ ] Установить filterStore.logger = "MyApp.Service"
- [ ] Экспорт содержит только записи от этого logger

**С комбинацией фильтров:**
- [ ] Установить search + minLevel + dateRange
- [ ] Экспорт применяет ВСЕ фильтры (AND логика)

### 5. Loading state

- [ ] Во время экспорта кнопка disabled
- [ ] Иконка Download → Spinner (вращается)
- [ ] Текст "EXPORT" → "EXPORTING..."
- [ ] После 1 секунды loading state сбрасывается
- [ ] Кнопка снова активна после экспорта

### 6. Disabled state

**Без сессии:**
- [ ] `logStore.sessionId === null`
- [ ] Кнопка disabled
- [ ] Border gray-600
- [ ] Text gray-600
- [ ] Hover не работает
- [ ] Клик не открывает dropdown

**С сессией:**
- [ ] `logStore.sessionId !== null`
- [ ] Кнопка активна
- [ ] Border emerald-500
- [ ] Text emerald-500
- [ ] Hover работает (bg-emerald-500, text-black)

### 7. Визуальный дизайн

**Typography:**
- [ ] Font: monoширинный
- [ ] Text transform: uppercase
- [ ] Letter spacing: tracking-wide
- [ ] Font weight: bold (700)

**Colors:**
- [ ] Border: emerald-500 (active) / gray-600 (disabled)
- [ ] Text: emerald-500 (active) / gray-600 (disabled)
- [ ] Background: black
- [ ] Hover: bg-emerald-500, text-black

**Borders:**
- [ ] Border width: 2px
- [ ] Border radius: 0 (острые углы)

**Dropdown:**
- [ ] Border: 2px solid emerald-500
- [ ] Background: black
- [ ] Box-shadow: 4px 4px rgba(emerald-500, 0.5)

**Icons:**
- [ ] Download icon (16px)
- [ ] ChevronDown icon (16px)
- [ ] FileJson icon (18px)
- [ ] FileText icon (18px)

### 8. Transitions

**Dropdown open:**
- [ ] Duration: 200ms
- [ ] Easing: ease-out
- [ ] Transform: translateY(-8px) → translateY(0)
- [ ] Opacity: 0 → 1

**Dropdown close:**
- [ ] Duration: 150ms
- [ ] Easing: ease-in
- [ ] Transform: translateY(0) → translateY(-8px)
- [ ] Opacity: 1 → 0

**ChevronDown:**
- [ ] Rotate: 0deg → 180deg при открытии
- [ ] Duration: 200ms

**Button hover:**
- [ ] Background: black → emerald-500
- [ ] Text: emerald-500 → black
- [ ] Duration: 200ms

**Spinner:**
- [ ] Rotation: 360deg в 1 секунду
- [ ] Animation: linear infinite

### 9. Keyboard navigation

- [ ] Tab переводит фокус на кнопку
- [ ] Enter открывает dropdown
- [ ] Space открывает dropdown
- [ ] **TODO:** Escape закрывает dropdown
- [ ] **TODO:** Arrow keys навигация по опциям

### 10. Browser compatibility

**Chrome/Edge:**
- [ ] Dropdown работает
- [ ] Transitions плавные
- [ ] Файл скачивается

**Firefox:**
- [ ] Dropdown работает
- [ ] Transitions плавные
- [ ] Файл скачивается

**Safari:**
- [ ] Dropdown работает
- [ ] Transitions плавные
- [ ] Файл скачивается

---

## Edge cases

### 1. Большие файлы
- [ ] Экспорт 1,000 записей
- [ ] Экспорт 10,000 записей
- [ ] Экспорт 100,000 записей
- [ ] Экспорт 1,000,000 записей
- [ ] Loading state достаточно длинный для больших файлов

### 2. Ошибки сети
- [ ] Backend недоступен → console.error
- [ ] Timeout запроса → console.error
- [ ] **TODO:** Toast notification с ошибкой

### 3. Пустые результаты
- [ ] Фильтры не нашли записей → экспорт пустого файла
- [ ] JSON: `{"logs":[]}`
- [ ] CSV: только header строка

### 4. Специальные символы
- [ ] Message с переносами строк `\n`
- [ ] Message с `|` символами
- [ ] Message с `,` в CSV (экранирование)
- [ ] Message с `"` в CSV (двойное экранирование)

### 5. Multiple clicks
- [ ] Быстрые клики на разные форматы
- [ ] Loading state предотвращает повторный экспорт
- [ ] Dropdown закрывается после первого клика

### 6. Session lifecycle
- [ ] Экспорт → clearSession() → sessionId null
- [ ] Кнопка становится disabled после clearSession
- [ ] Загрузка нового файла → новый sessionId
- [ ] Экспорт с новым sessionId работает

---

## Performance testing

### 1. Bundle size
- [ ] ExportButton.vue gzipped < 3 KB
- [ ] lucide-vue-next иконки tree-shaken (только 4 иконки)
- [ ] No duplicate dependencies

### 2. Runtime performance
- [ ] Dropdown открывается < 50ms
- [ ] Transitions плавные (60 FPS)
- [ ] Нет memory leaks после unmount
- [ ] Event listeners удаляются при закрытии

### 3. Network
- [ ] Только один HTTP запрос на экспорт
- [ ] Streaming response (не весь файл в память)
- [ ] Прогресс скачивания показывается браузером

---

## Code quality

### 1. TypeScript
- [x] Нет `any` типов
- [x] Все функции типизированы
- [x] Props/emits типизированы (нет props/emits)

### 2. Vue best practices
- [x] Composition API с `<script setup>`
- [x] Reactive refs для state
- [x] Computed values для derived state
- [x] watch() для side effects
- [x] Cleanup в watch callback

### 3. Accessibility
- [ ] **TODO:** aria-expanded
- [ ] **TODO:** aria-haspopup
- [ ] **TODO:** role="menu"
- [ ] **TODO:** role="menuitem"

### 4. Error handling
- [x] try-catch в handleExport
- [ ] **TODO:** Toast notification
- [ ] **TODO:** Retry механизм

---

## Documentation

- [x] `README.md` — overview и примеры
- [x] `FEATURES.md` — детальное описание
- [x] `ExportButton.example.vue` — code example
- [x] `CHECKLIST.md` — этот файл
- [x] JSDoc комментарии в коде
- [x] TypeScript типы задокументированы

---

## Deployment checklist

### Pre-deploy
- [ ] Все тесты в этом чеклисте пройдены
- [ ] TypeScript проверка успешна
- [ ] Build успешен (`npm run build`)
- [ ] Code review выполнен

### Deploy
- [ ] Frontend deploy (Vite build)
- [ ] Backend ready (экспорт API работает)
- [ ] CORS настроен для frontend origin

### Post-deploy
- [ ] Smoke test: загрузка файла → экспорт JSON → скачался
- [ ] Smoke test: загрузка файла → экспорт CSV → скачался
- [ ] Monitoring: нет JS errors в console
- [ ] Monitoring: API endpoint `/api/export/{sessionId}` работает

---

## Known issues

1. **Нет progress indicator** — большие файлы генерируются без feedback
2. **Нет отмены** — экспорт нельзя остановить после старта
3. **Нет error UI** — ошибки только в console
4. **Keyboard nav неполная** — нет Escape, Arrow keys

---

## Next steps (Phase 5+)

- [ ] Unit тесты (Vitest + Vue Testing Library)
- [ ] E2E тесты (Playwright)
- [ ] Accessibility improvements (WCAG 2.1 Level AA)
- [ ] Progress indicator для больших файлов
- [ ] Toast notifications для ошибок/успеха
- [ ] Настройка полей для экспорта
- [ ] Дополнительные форматы (XML, Excel)

---

**Чеклист обновлён:** 2026-01-10
**Версия компонента:** 1.0.0
