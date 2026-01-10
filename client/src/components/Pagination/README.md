# Pagination Component

Компонент пагинации для nLogMonitor с terminal/brutalist эстетикой.

## Особенности

- **Кнопки навигации**: Previous/Next с автоматической блокировкой на границах
- **Выбор размера страницы**: Dropdown с вариантами 50, 100, 200 записей
- **Информация о странице**: Текущая страница / всего страниц + диапазон записей
- **Компактная компоновка**: Горизонтальное расположение всех элементов
- **Terminal эстетика**: Моноширинный шрифт, острые углы, минимализм
- **Интеграция с Pinia**: Полная синхронизация с logStore

## Использование

```vue
<script setup>
import { Pagination } from '@/components/Pagination'
</script>

<template>
  <Pagination />
</template>
```

Компонент автоматически подключается к `logStore` и реагирует на изменения состояния.

## API logStore

Компонент использует следующие свойства и методы из `logStore`:

### Свойства (реактивные)
- `page` - текущая страница (число)
- `pageSize` - размер страницы (50 | 100 | 200)
- `totalPages` - общее количество страниц
- `totalCount` - общее количество записей
- `canPreviousPage` - можно ли перейти назад
- `canNextPage` - можно ли перейти вперёд

### Методы
- `setPage(newPage: number)` - установить текущую страницу
- `setPageSize(newSize: number)` - изменить размер страницы (сбрасывает на страницу 1)

## Структура

```
┌─────────────────────────────────────────────────────────────┐
│ Показать: [50 ▼]    Страница 1 из 5     [◄ Назад] [Вперёд ►] │
│                      1-50 из 250                              │
└─────────────────────────────────────────────────────────────┘
```

## Стили

- **Моноширинный шрифт**: JetBrains Mono, Fira Code, SF Mono
- **Цветовая схема**: zinc (dark) + emerald (accent)
- **Границы**: острые углы (border-radius: 4-8px)
- **Состояния**:
  - Disabled кнопки: opacity-30
  - Hover: bg-zinc-800
  - Focus: ring-1 ring-emerald-500
  - Active dropdown: bg-emerald-950/30 text-emerald-400

## Dropdown поведение

- Открывается по клику на кнопку
- Закрывается автоматически при потере фокуса (blur)
- Выбранный вариант подсвечен emerald
- Анимация slideDown при открытии

## Интеграция в Layout

Компонент размещается под таблицей логов:

```vue
<div class="flex h-[calc(100vh-8rem)] flex-col">
  <LogTable />
  <Pagination v-if="logStore.hasSession" />
</div>
```

## Preview

Для визуального тестирования используйте:
```
test-data/pagination-preview.html
```

## Скриншоты

Скриншоты компонента находятся в `.playwright-mcp/`:
- `pagination-preview-full.png` - все состояния
- `pagination-dropdown-detail.png` - детальный вид с dropdown
