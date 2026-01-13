# nLogMonitor Frontend

Vue 3 + TypeScript + Vite frontend для просмотра и анализа NLog-логов.

## Технологии

| Технология | Версия | Назначение |
|------------|--------|------------|
| Vue | 3.x | UI фреймворк |
| TypeScript | 5.x | Типизация |
| Vite | 5.x | Сборщик |
| Pinia | 2.x | State management |
| TanStack Table | 8.x | Таблица логов |
| Tailwind CSS | 3.x | Стилизация |
| shadcn-vue | latest | UI компоненты |
| Axios | 1.x | HTTP клиент |

## Быстрый старт

```bash
# Установка зависимостей
npm install

# Запуск dev-сервера (http://localhost:5173)
npm run dev

# Production сборка
npm run build

# Проверка кода
npm run lint
```

## Структура проекта

```
src/
├── api/                  # Axios API client
│   ├── client.ts
│   ├── logs.ts
│   ├── files.ts
│   ├── export.ts
│   └── health.ts
├── components/
│   ├── ui/               # shadcn-vue (Button, Input, Card, Table)
│   ├── FileSelector/     # Drag & drop загрузка файлов
│   ├── LogTable/         # Таблица логов с TanStack Table
│   └── ExportButton/     # Dropdown экспорт в JSON/CSV
├── lib/                  # Utility functions
├── stores/               # Pinia stores
│   ├── logStore.ts       # Состояние логов и сессии
│   ├── filterStore.ts    # Фильтры и поиск
│   └── recentStore.ts    # Недавние файлы
├── types/                # TypeScript типы
│   └── index.ts
├── App.vue
└── main.ts
```

## Реализованные компоненты

- **FileSelector** — drag & drop загрузка .log файлов
- **LogTable** — таблица с TanStack Table и виртуализацией
- **LogLevelBadge** — цветовая индикация уровней (Trace/Debug/Info/Warn/Error/Fatal)
- **ExportButton** — dropdown с экспортом в JSON/CSV, terminal/brutalist дизайн
- **shadcn-vue** — Button, Input, Card, Table

## Конфигурация

Переменные окружения (`.env`):

```bash
VITE_API_URL=http://localhost:5000  # URL backend API
VITE_APP_TITLE=nLogMonitor
```

## Связь с Backend

Frontend взаимодействует с .NET 10 Backend через REST API:

- `POST /api/upload` — загрузка файлов
- `GET /api/logs/{sessionId}` — получение логов с фильтрацией
- `GET /api/export/{sessionId}` — экспорт в JSON/CSV
- `GET /health` — проверка доступности API

## Планируется (Фаза 5)

- FilterPanel — фильтры по уровням логирования
- SearchBar — полнотекстовый поиск с debounce
- Pagination — навигация по страницам
- LogDetails — детальный просмотр одной записи
