# 📊 nLogMonitor

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Vue](https://img.shields.io/badge/Vue-3.x-4FC08D?logo=vuedotjs)](https://vuejs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
<!-- Добавьте после настройки CI/CD:
[![Build Status](https://img.shields.io/github/actions/workflow/status/YOUR_USERNAME/nLogMonitor/ci.yml?branch=master)](https://github.com/YOUR_USERNAME/nLogMonitor/actions)
[![Coverage](https://img.shields.io/codecov/c/github/YOUR_USERNAME/nLogMonitor)](https://codecov.io/gh/YOUR_USERNAME/nLogMonitor)
-->

> Современный веб-инструмент для просмотра, анализа и экспорта логов NLog

![nLogMonitor Screenshot](docs/assets/screenshot-placeholder.png)
<!-- TODO: Заменить на реальный скриншот после реализации UI -->

---

## 📋 Содержание

- [Возможности](#-возможности)
- [Текущий статус](#-текущий-статус)
- [Быстрый старт](#-быстрый-старт)
- [Установка](#-установка)
- [Использование](#-использование)
- [Документация](#-документация)
- [Технологии](#-технологии)
- [Roadmap](#-roadmap)
- [Contributing](#-contributing)
- [Лицензия](#-лицензия)

---

## ✨ Возможности

| Функция | Описание |
|---------|----------|
| 📤 **Drag & Drop загрузка** | Перетащите лог-файл в браузер |
| 🔍 **Полнотекстовый поиск** | Мгновенный поиск по сообщениям |
| 🎯 **Фильтрация по уровням** | Trace, Debug, Info, Warn, Error, Fatal |
| 📊 **Виртуализированная таблица** | Плавная работа с миллионами записей |
| 💾 **Экспорт данных** | JSON и CSV форматы |
| ⚡ **Real-time обновления** | Автоматическое отображение новых записей через SignalR |
| ⏱️ **Управление сессиями** | Автоочистка при закрытии вкладки + fallback TTL 5 мин |

---

## 🎯 Текущий статус

**Фаза 6 ✅ ЗАВЕРШЕНО** — Real-time обновления через SignalR с FileWatcher полностью реализованы. Следующая фаза: **Фаза 7** (Скрипты запуска и конфигурация).

### Реализовано (Backend)

| Компонент | Описание |
|-----------|----------|
| **NLogParser** | Высокопроизводительный парсер на `Span<char>` + `IAsyncEnumerable` |
| **Многострочные записи** | Корректная обработка stack traces и сообщений с переносами |
| **InMemorySessionStorage** | Хранилище сессий с TTL и автоочисткой |
| **REST API Controllers** | FilesController, UploadController, LogsController, ExportController, RecentController |
| **Серверная фильтрация** | Фильтрация по уровням, временным диапазонам, logger, process/thread ID |
| **Полнотекстовый поиск** | Поиск по сообщению (case-insensitive) |
| **Пагинация** | Серверная пагинация с сортировкой |
| **Экспорт** | JSON и CSV форматы с потоковой генерацией (Utf8JsonWriter) |
| **DesktopOnlyAttribute** | Защита Desktop-only эндпоинтов в Web-режиме |
| **Path traversal защита** | Санитизация имён файлов при Upload |
| **FileWatcherService** | Мониторинг изменений файла с debounce 200ms |
| **SignalR Hub** | Real-time обновления через LogWatcherHub |
| **Session lifecycle** | Привязка к connectionId, автоудаление при disconnect |
| **Unit/Integration тесты** | 283 теста (NUnit) — парсер, хранилище, сервисы, контроллеры, SignalR |

```bash
# Проверка работоспособности
dotnet test                       # 283 теста
curl http://localhost:5000/health  # {"status":"healthy",...}
```

### Реализовано (Frontend)

| Компонент | Описание |
|-----------|----------|
| **Vue 3 + Vite + TypeScript** | Современный frontend stack |
| **shadcn-vue + Tailwind CSS** | UI компоненты с dark theme |
| **Pinia stores** | State management (logStore, filterStore, recentStore) |
| **Axios API client** | HTTP клиент с interceptors |
| **FileSelector** | Drag & drop загрузка файлов |
| **LogTable** | TanStack Table с цветовой индикацией уровней |
| **LogLevelBadge** | Компактные badges для Trace/Debug/Info/Warn/Error/Fatal |
| **FilterPanel** | Toggle-кнопки фильтрации по уровням с подсчётом записей |
| **SearchBar** | Полнотекстовый поиск с debounce 300ms |
| **Pagination** | Навигация по страницам с выбором размера (50/100/200) |
| **ExportButton** | Экспорт в JSON/CSV с dropdown выбора формата |
| **RecentFiles** | История недавно открытых файлов |
| **SignalR клиент** | @microsoft/signalr 10.0 с автореконнектом |
| **useFileWatcher** | Composable для real-time обновлений |
| **LiveIndicator** | Индикатор состояния соединения (Live/Connecting/Reconnecting) |
| **Loading/Error states** | Toast уведомления и empty states |
| **Responsive design** | Адаптивная вёрстка для всех разрешений |

```bash
# Запуск frontend
cd client
npm install
npm run dev    # http://localhost:5173
npm run build  # Production build
```

---

## 🚀 Быстрый старт

```bash
# Клонирование
git clone https://github.com/YOUR_USERNAME/nLogMonitor.git
cd nLogMonitor

# Сборка и тесты
dotnet build
dotnet test

# Запуск бэкенда
dotnet run --project src/nLogMonitor.Api
```

- API: http://localhost:5000
- Health check: http://localhost:5000/health
- Swagger UI: http://localhost:5000/swagger (Development)
- Frontend: http://localhost:5173 (npm run dev)

---

## 📦 Установка

### Требования

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) *(для Frontend, Фазы 4-5)*

### Пошаговая установка

<details>
<summary><b>🖥️ Локальная разработка</b></summary>

```bash
# 1. Клонируйте репозиторий
git clone https://github.com/YOUR_USERNAME/nLogMonitor.git
cd nLogMonitor

# 2. Восстановите зависимости и соберите
dotnet restore
dotnet build

# 3. Запустите тесты
dotnet test

# 4. Запустите API в режиме разработки
dotnet run --project src/nLogMonitor.Api
# или с hot reload:
dotnet watch run --project src/nLogMonitor.Api

# 5. Запустите Frontend (в отдельном терминале)
cd client
npm install
npm run dev
# Frontend: http://localhost:5173
```

</details>

<details>
<summary><b>⚡ Быстрый запуск (скрипты)</b></summary>

```bash
# Windows (CMD или PowerShell)
start-dev.bat      # Запуск backend + frontend с hot reload
```

</details>

<details>
<summary><b>📦 Production сборка</b></summary>

```bash
# Backend
dotnet publish src/nLogMonitor.Api -c Release -o publish

# Frontend
cd client
npm run build

# Скопировать dist в wwwroot для объединённой сборки
```

</details>

---

## 💻 Использование

### Загрузка лог-файла

1. Откройте приложение в браузере
2. Перетащите `.log` файл в область загрузки или нажмите "Выбрать файл"
3. Дождитесь парсинга (статус отображается в прогресс-баре)

### Поддерживаемый формат NLog

```
${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
```

**Пример:**
```
2024-01-15 10:30:45.1234|INFO|Application started|MyApp.Program|1234|1
2024-01-15 10:30:46.5678|ERROR|Connection failed|MyApp.Database|1234|2
System.Net.Sockets.SocketException: Connection refused
   at System.Net.Sockets.Socket.Connect()
```

### Фильтрация и поиск

![Filters Demo](docs/assets/filters-placeholder.gif)
<!-- TODO: Заменить на реальную GIF-анимацию -->

- **По уровню**: Кликните на чипы уровней (можно выбрать несколько)
- **Поиск**: Введите текст в строку поиска (debounce 300ms)
- **Сброс**: Кнопка "Сбросить фильтры"

### API Endpoints

| Endpoint | Метод | Описание |
|----------|-------|----------|
| `/api/files/open` | POST | Открытие файла по пути (Desktop) |
| `/api/files/open-directory` | POST | Открытие директории с автовыбором последнего .log |
| `/api/upload` | POST | Загрузка файла (Web, max 100MB) |
| `/api/logs/{sessionId}` | GET | Получение логов с фильтрацией и пагинацией |
| `/api/export/{sessionId}` | GET | Экспорт логов (?format=json\|csv) |
| `/api/recent` | GET | Список недавно открытых файлов |
| `/api/recent` | DELETE | Очистка истории недавних |
| `/health` | GET | Health check |

### Экспорт

```bash
# JSON
GET /api/export/{sessionId}?format=json

# CSV
GET /api/export/{sessionId}?format=csv
```

---

## 📚 Документация

| Документ | Описание |
|----------|----------|
| [🏗️ Архитектура](docs/ARCHITECTURE.md) | Clean Architecture, слои, диаграммы |
| [👩‍💻 Разработка](docs/DEVELOPMENT.md) | Настройка окружения, запуск тестов |
| [🚀 Запуск](docs/DEPLOYMENT.md) | Скрипты запуска, production |
| [🔌 API](docs/API.md) | REST endpoints, примеры запросов |
| [⚙️ Конфигурация](docs/CONFIGURATION.md) | Настройки приложения |
| [📝 Changelog](docs/CHANGELOG.md) | История изменений |
| [🤝 Contributing](docs/CONTRIBUTING.md) | Как внести вклад |

---

## 🛠️ Технологии

### Backend

| Технология | Версия | Назначение |
|------------|--------|------------|
| ASP.NET Core | 10.0 | Web API |
| FluentValidation | 11.3.1 | Валидация |
| NLog.Web.AspNetCore | 6.1.0 | Логирование |
| Swashbuckle.AspNetCore | 10.1.0 | API документация |

### Frontend

| Технология | Версия | Назначение |
|------------|--------|------------|
| Vue | 3.5.24 | UI фреймворк |
| TypeScript | 5.9.3 | Типизация |
| Vite | 7.2.4 | Сборщик |
| Pinia | 3.0.4 | State management |
| TanStack Table Vue | 8.21.3 | Таблица |
| Tailwind CSS | 3.4.19 | Стилизация |
| Reka UI | 2.7.0 | UI компоненты |
| Axios | 1.13.2 | HTTP клиент |
| @microsoft/signalr | 10.0.0 | Real-time соединение |
| @vueuse/core | 14.1.0 | Composables утилиты |
| lucide-vue-next | 0.562.0 | Иконки |

---

## 🗺️ Roadmap

- [x] Планирование архитектуры
- [x] **Фаза 1**: Базовая инфраструктура
- [x] **Фаза 2**: Парсинг и хранение
- [x] **Фаза 3**: REST API Endpoints
- [x] **Фаза 3.1**: Исправления безопасности и оптимизации (path traversal, DesktopOnly, потоковый экспорт)
- [x] **Фаза 4**: Базовый фронтенд (Vue 3)
- [x] **Фаза 5**: UI компоненты (FilterPanel, SearchBar, Pagination, ExportButton, RecentFiles)
- [x] **Фаза 6**: Real-time обновления (SignalR)
- [ ] **Фаза 7**: Скрипты запуска и конфигурация
- [ ] **Фаза 8**: Client-side Logging
- [ ] **Фаза 9**: Photino Desktop
- [ ] **Фаза 10**: Оптимизация и тестирование

### Планируемые фичи (после v1.0)

- [ ] Удалённый доступ по SSH (Фаза 11)
- [ ] Компактный режим Dashboard (Фаза 12)
- [ ] Поддержка нескольких форматов логов (Serilog, log4net)
- [ ] Графики и статистика

---

## 🤝 Contributing

Мы приветствуем вклад в проект! Пожалуйста, ознакомьтесь с [CONTRIBUTING.md](docs/CONTRIBUTING.md).

```bash
# Форк и клон
git clone https://github.com/YOUR_USERNAME/nLogMonitor.git

# Создайте ветку
git checkout -b feature/amazing-feature

# Коммит
git commit -m "feat: add amazing feature"

# Push и PR
git push origin feature/amazing-feature
```

---

## 📄 Лицензия

Распространяется под лицензией MIT. См. [LICENSE](LICENSE) для подробностей.

---

<div align="center">

**[⬆ Наверх](#-nlogmonitor)**

Made with ❤️ for the .NET community

</div>
