# Toast Component

Terminal/brutalist styled toast notification system для nLogMonitor.

## Features

- **Auto-dismiss**: Автоматическое скрытие через 5 секунд
- **Manual close**: Кнопка закрытия с X иконкой
- **Transitions**: Плавное появление/исчезновение (slide from right)
- **Type indicators**: Цветная левая граница и иконки для каждого типа
- **Stacking**: Поддержка нескольких уведомлений одновременно

## Usage

### 1. Basic Usage

```vue
<script setup lang="ts">
import { useToast } from '@/composables/useToast'

const { showToast } = useToast()

// Show different types
showToast('Operation completed', 'success')
showToast('Processing...', 'info')
showToast('Something went wrong', 'error')
</script>
```

### 2. Integration in App.vue

```vue
<script setup lang="ts">
import { Toast } from '@/components/Toast'
</script>

<template>
  <div>
    <!-- Your app content -->
    <Toast />
  </div>
</template>
```

### 3. In Stores (Error Handling)

```typescript
import { useToast } from '@/composables/useToast'

export const useLogStore = defineStore('logs', () => {
  const { showToast } = useToast()

  async function uploadFile(file: File) {
    try {
      // ... upload logic
      showToast(`File "${file.name}" loaded successfully`, 'success')
    } catch (error) {
      showToast(error.message, 'error')
    }
  }

  return { uploadFile }
})
```

## Types

```typescript
type ToastType = 'error' | 'success' | 'info'

interface Toast {
  id: string
  message: string
  type: ToastType
  timestamp: number
}
```

## API

### `showToast(message: string, type: ToastType = 'info'): string`

Отображает toast уведомление и возвращает его ID.

**Parameters:**
- `message` - текст уведомления
- `type` - тип уведомления (по умолчанию `'info'`)

**Returns:** ID созданного toast

### `hideToast(id: string): void`

Скрывает конкретное toast уведомление по ID.

### `toasts: Ref<Toast[]>`

Reactive массив всех активных toast уведомлений.

## Styling

- **Error**: Красная левая граница, AlertCircle иконка
- **Success**: Зеленая левая граница, CheckCircle иконка
- **Info**: Синяя левая граница, Info иконка
- **Position**: Фиксированная позиция в правом верхнем углу
- **Shadow**: Brutalist box-shadow (4px offset)
- **Dark mode**: Полная поддержка dark theme

## Demo

См. `ToastDemo.vue` для интерактивных примеров.
