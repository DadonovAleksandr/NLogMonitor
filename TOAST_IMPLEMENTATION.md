# Toast System Implementation

Реализована полнофункциональная система уведомлений (Toast) для nLogMonitor с terminal/brutalist эстетикой.

## Реализованные файлы

### 1. Composable: `client/src/composables/useToast.ts`
**Функционал:**
- Reactive массив `toasts` для хранения активных уведомлений
- `showToast(message, type)` - создание и отображение toast
- `hideToast(id)` - ручное скрытие toast
- Auto-dismiss через 5 секунд
- Уникальный ID для каждого toast (`toast-{counter}-{timestamp}`)

**Интерфейс:**
```typescript
interface Toast {
  id: string
  message: string
  type: 'error' | 'success' | 'info'
  timestamp: number
}
```

### 2. Компонент: `client/src/components/Toast/Toast.vue`
**Визуальные особенности:**
- Fixed position в правом верхнем углу (`top-4 right-4`)
- Цветная левая граница (8px) в зависимости от типа:
  - Error: красный (`border-l-red-500`)
  - Success: зеленый (`border-l-green-500`)
  - Info: синий (`border-l-blue-500`)
- Brutalist box-shadow: `4px 4px 0px` (черный/белый в зависимости от темы)
- Иконки из lucide-vue-next:
  - AlertCircle (error)
  - CheckCircle (success)
  - Info (info)
- Кнопка закрытия с X иконкой
- Монохромный шрифт (font-mono)

**Transitions:**
- Enter/Leave: `cubic-bezier(0.4, 0, 0.2, 1)` (0.3s)
- Slide from right с scale эффектом
- Smooth stacking при добавлении/удалении

**Responsive:**
- Min width: 320px
- Max width: 480px
- Автоматический перенос текста (break-words)

### 3. Demo: `client/src/components/Toast/ToastDemo.vue`
Демо-страница для тестирования всех вариантов toast:
- Error toast
- Success toast
- Info toast
- Multiple toasts
- Long message toast

### 4. Интеграция

#### App.vue
```vue
<script setup>
import { Toast } from '@/components/Toast'
</script>

<template>
  <div>
    <!-- App content -->
    <Toast />
  </div>
</template>
```

#### logStore.ts
Интегрирован `useToast()` в следующих методах:
- `uploadFile()` - success/error при загрузке файла
- `openFile()` - success/error при открытии файла
- `openDirectory()` - success/error при открытии директории
- `fetchLogs()` - error при загрузке логов

**Примеры сообщений:**
```typescript
// Success
showToast(`File "${file.name}" loaded successfully`, 'success')

// Error
showToast('Failed to upload file', 'error')
```

## Экспорты

### `client/src/components/Toast/index.ts`
```typescript
export { default as Toast } from './Toast.vue'
```

### `client/src/composables/index.ts`
```typescript
export * from './useToast'
```

## Использование

### В компонентах
```vue
<script setup>
import { useToast } from '@/composables/useToast'

const { showToast } = useToast()

const handleClick = () => {
  showToast('Operation completed', 'success')
}
</script>
```

### В stores
```typescript
import { useToast } from '@/composables/useToast'

export const useMyStore = defineStore('my', () => {
  const { showToast } = useToast()

  async function doSomething() {
    try {
      // ...
      showToast('Success!', 'success')
    } catch (error) {
      showToast(error.message, 'error')
    }
  }

  return { doSomething }
})
```

## Дизайн-система

### Цвета
- Error: `red-500` (#ef4444)
- Success: `green-500` (#22c55e)
- Info: `blue-500` (#3b82f6)
- Background: `white` (light) / `black` (dark)
- Text: `black` (light) / `white` (dark)

### Типография
- Font: `font-mono` (Menlo, Monaco, Courier New)
- Size: `text-sm` (14px)
- Line height: `leading-relaxed` (1.625)

### Spacing
- Padding: `px-4 py-3` (1rem × 0.75rem)
- Gap: `gap-3` (0.75rem)
- Stack gap: `gap-2` (0.5rem)

### Animation
- Duration: 0.3s
- Easing: cubic-bezier(0.4, 0, 0.2, 1)
- Auto-dismiss: 5000ms

## Тестирование

### Manual Testing
1. Запустить dev server: `npm run dev`
2. Временно изменить `main.ts` для рендера ToastDemo:
```typescript
import ToastDemo from './components/Toast/ToastDemo.vue'
createApp(ToastDemo).mount('#app')
```
3. Тестировать все типы уведомлений

### Integration Testing
Все toast вызовы в `logStore` автоматически тестируются при:
- Загрузке файлов (FileSelector)
- Открытии файлов (RecentFiles)
- Ошибках API (перехватчики Axios)

## Документация

См. `client/src/components/Toast/README.md` для детальной документации API и примеров.

## Следующие шаги (опционально)

- [ ] Добавить toast queue с лимитом одновременных уведомлений
- [ ] Добавить настройку duration для каждого toast
- [ ] Добавить звуковые уведомления
- [ ] Добавить toast actions (кнопки в уведомлении)
- [ ] Добавить toast с progress bar для длительных операций
