# ExportButton

Компонент для экспорта логов в форматах JSON и CSV с dropdown меню выбора формата.

## Особенности

- **Dropdown меню** с выбором формата экспорта (JSON/CSV)
- **Loading state** во время генерации файла
- **Terminal/brutalist дизайн** — моноширинный шрифт, острые углы, emerald акценты
- **Интеграция с stores** — использует `logStore.sessionId` и `filterStore.filterOptions`
- **Автоматическое скачивание** — файл скачивается напрямую через браузер (downloadExport)
- **Disabled state** — кнопка неактивна если нет сессии
- **Click outside** — dropdown закрывается при клике вне компонента

## Использование

```vue
<script setup lang="ts">
import { ExportButton } from '@/components/ExportButton'
</script>

<template>
  <ExportButton />
</template>
```

## Зависимости

- `@/components/ui/button` — shadcn-vue Button компонент
- `@/api/export` — exportApi.downloadExport()
- `@/stores/logStore` — sessionId для экспорта
- `@/stores/filterStore` — filterOptions (фильтры применяются к экспорту)
- `lucide-vue-next` — иконки Download, ChevronDown, FileJson, FileText

## API экспорта

Компонент использует `exportApi.downloadExport(sessionId, format, filters)`:
- **sessionId** — ID текущей сессии логов
- **format** — `'json'` или `'csv'`
- **filters** — FilterOptions из filterStore (searchText, minLevel, maxLevel, dateRange, logger)

Файл скачивается с именем `logs-{sessionId}.{format}`.

## Стилизация

Terminal/brutalist эстетика:
- Моноширинный шрифт (font-mono)
- Острые углы (без border-radius)
- Border 2px solid emerald-500
- Hover state: bg-emerald-500 + text-black
- Box-shadow: 4px 4px emerald-500 с прозрачностью
- Uppercase текст с letter-spacing

## Loading State

Во время экспорта:
- Иконка Download заменяется на спиннер (вращающийся border)
- Текст меняется на "EXPORTING..."
- Кнопка становится disabled
- Loading state сбрасывается через 1 секунду после старта загрузки

## Transitions

- Dropdown появляется/исчезает с анимацией fade + slide (200ms)
- ChevronDown вращается на 180° при открытии
- Hover transitions для кнопок (200ms)
