# Settings Store и Auto-Save

## Обзор

Система настроек приложения с автоматическим сохранением открытых вкладок и активной вкладки.

### Компоненты

1. **settingsStore** - Pinia store для управления настройками
2. **useSettings** - Composable для автоматического сохранения
3. **tabsStore** - Расширен методами экспорта/импорта настроек

## Архитектура

```
┌─────────────────┐
│   App.vue       │
│  (useSettings)  │  ← Подключаем composable один раз
└────────┬────────┘
         │
         │ watch tabs changes
         ▼
┌─────────────────┐     debounce 1s      ┌──────────────────┐
│   useSettings   │ ───────────────────> │ settingsStore    │
│   (composable)  │                      │ .saveSettings()  │
└─────────────────┘                      └────────┬─────────┘
         ▲                                        │
         │                                        │ PUT /api/settings
         │                                        ▼
         │                              ┌──────────────────┐
         │                              │   Backend API    │
         │                              │  (JSON file)     │
         │                              └──────────────────┘
         │
┌─────────────────┐
│   tabsStore     │
│ .exportToSettings() │
└─────────────────┘
```

## Использование

### 1. Инициализация (App.vue или main.ts)

```typescript
import { useSettings } from '@/composables'
import { useSettingsStore } from '@/stores'

// В setup()
const settingsStore = useSettingsStore()

// Загрузка настроек при старте приложения
onMounted(async () => {
  await settingsStore.loadSettings()

  // Восстановление вкладок из настроек
  const tabsToRestore = tabsStore.importFromSettings(settingsStore.settings)
  for (const tab of tabsToRestore) {
    // Открыть файлы/директории и создать вкладки
    await openFileOrDirectory(tab.path, tab.type)
  }

  // Установить активную вкладку
  if (tabsStore.tabs.length > 0) {
    const activeIndex = Math.min(
      settingsStore.getLastActiveTabIndex(),
      tabsStore.tabs.length - 1
    )
    tabsStore.setActiveTab(tabsStore.tabs[activeIndex].id)
  }
})

// Подключить автоматическое сохранение
useSettings()
```

### 2. Автоматическое сохранение

После вызова `useSettings()` настройки автоматически сохраняются при:
- Добавлении новой вкладки
- Закрытии вкладки
- Переключении между вкладками

Сохранение происходит с debounce 1 секунда для избежания частых запросов.

### 3. Ручное сохранение (опционально)

```typescript
import { useSettingsStore, useTabsStore } from '@/stores'

const settingsStore = useSettingsStore()
const tabsStore = useTabsStore()

// Экспортировать текущие вкладки
const settings = tabsStore.exportToSettings()

// Сохранить
await settingsStore.saveSettings(settings)
```

### 4. Восстановление вкладок

```typescript
import { useSettingsStore, useTabsStore } from '@/stores'

const settingsStore = useSettingsStore()
const tabsStore = useTabsStore()

// Загрузить настройки
await settingsStore.loadSettings()

// Получить список вкладок для восстановления
const tabsToRestore = tabsStore.importFromSettings(settingsStore.settings)

// Восстановить вкладки
for (const tab of tabsToRestore) {
  if (tab.type === 'file') {
    await openFile(tab.path)
  } else {
    await openDirectory(tab.path)
  }
}

// Восстановить активную вкладку
const activeIndex = settingsStore.getLastActiveTabIndex()
if (tabsStore.tabs.length > 0 && activeIndex < tabsStore.tabs.length) {
  tabsStore.setActiveTab(tabsStore.tabs[activeIndex].id)
}
```

## API

### settingsStore

#### State
- `settings: UserSettings` - текущие настройки
- `isLoaded: boolean` - флаг загрузки настроек
- `isLoading: boolean` - флаг процесса загрузки
- `error: string | null` - ошибка загрузки/сохранения

#### Actions
- `loadSettings(): Promise<void>` - загрузить настройки с сервера
- `saveSettings(settings: UserSettings): Promise<void>` - сохранить настройки
- `getOpenedTabs(): TabSetting[]` - получить список вкладок
- `getLastActiveTabIndex(): number` - получить индекс активной вкладки

### tabsStore (расширения)

#### Actions
- `exportToSettings(): UserSettings` - экспортировать вкладки в формат настроек
- `importFromSettings(settings: UserSettings): TabSetting[]` - получить список вкладок для восстановления

### useSettings() composable

Автоматически отслеживает изменения вкладок и сохраняет настройки с debounce.

#### Возвращает
- `scheduleSave(): void` - принудительно запланировать сохранение

## Типы данных

### UserSettings
```typescript
interface UserSettings {
  openedTabs: TabSetting[]
  lastActiveTabIndex: number
}
```

### TabSetting
```typescript
interface TabSetting {
  type: 'file' | 'directory'
  path: string           // Полный путь к файлу/директории
  displayName: string    // Имя для отображения во вкладке
}
```

## Backend API

### GET /api/settings
Получить настройки пользователя

**Response:**
```json
{
  "openedTabs": [
    {
      "type": "file",
      "path": "C:/logs/app.log",
      "displayName": "app.log"
    }
  ],
  "lastActiveTabIndex": 0
}
```

### PUT /api/settings
Сохранить настройки

**Request Body:**
```json
{
  "openedTabs": [...],
  "lastActiveTabIndex": 0
}
```

**Response:** 204 No Content

## Примечания

1. **Debounce**: Сохранение происходит через 1 секунду после последнего изменения вкладок
2. **Error Handling**: Ошибки сохранения логируются, но не прерывают работу приложения
3. **isLoaded Check**: Автосохранение начинается только после загрузки начальных настроек
4. **Default Settings**: При ошибке загрузки используются пустые настройки (без вкладок)

## Пример полного цикла

```typescript
// 1. При старте приложения
await settingsStore.loadSettings()

// 2. Восстановление вкладок
const tabs = tabsStore.importFromSettings(settingsStore.settings)
for (const tab of tabs) {
  await openFileOrDirectory(tab.path, tab.type)
}

// 3. Установка активной вкладки
tabsStore.setActiveTab(tabsStore.tabs[settingsStore.getLastActiveTabIndex()].id)

// 4. Подключение автосохранения
useSettings()

// 5. Далее все изменения вкладок автоматически сохраняются
tabsStore.addTab('file', 'new.log', 'C:/logs/new.log') // Auto-save через 1 сек
tabsStore.setActiveTab(newTabId)                        // Auto-save через 1 сек
tabsStore.closeTab(oldTabId)                            // Auto-save через 1 сек
```
