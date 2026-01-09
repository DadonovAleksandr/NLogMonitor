# 📊 nLogMonitor

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18.x-61DAFB?logo=react)](https://react.dev/)
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
| ⏱️ **Сессии с TTL** | Автоматическая очистка через 1 час |

---

## 🚀 Быстрый старт

```bash
# Клонирование
git clone https://github.com/YOUR_USERNAME/nLogMonitor.git
cd nLogMonitor

# Запуск бэкенда
cd src/nLogMonitor.Api
dotnet run

# Запуск фронтенда (в новом терминале)
cd client
npm install && npm run dev
```

Откройте http://localhost:5173 в браузере.

---

## 📦 Установка

### Требования

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [npm](https://www.npmjs.com/) или [pnpm](https://pnpm.io/)

### Пошаговая установка

<details>
<summary><b>🖥️ Локальная разработка</b></summary>

```bash
# 1. Клонируйте репозиторий
git clone https://github.com/YOUR_USERNAME/nLogMonitor.git
cd nLogMonitor

# 2. Восстановите зависимости бэкенда
dotnet restore

# 3. Установите зависимости фронтенда
cd client
npm install

# 4. Запустите в режиме разработки
# Терминал 1:
dotnet run --project src/nLogMonitor.Api

# Терминал 2:
cd client && npm run dev
```

</details>

<details>
<summary><b>🐳 Docker</b></summary>

```bash
# Сборка и запуск
docker-compose up -d

# Или отдельно
docker build -t nlogmonitor .
docker run -p 5000:5000 -p 5173:5173 nlogmonitor
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
| [🚀 Деплой](docs/DEPLOYMENT.md) | Docker, CI/CD, production |
| [🔌 API](docs/API.md) | REST endpoints, примеры запросов |
| [⚙️ Конфигурация](docs/CONFIGURATION.md) | Настройки приложения |
| [📝 Changelog](docs/CHANGELOG.md) | История изменений |
| [🤝 Contributing](docs/CONTRIBUTING.md) | Как внести вклад |

---

## 🛠️ Технологии

### Backend

| Технология | Версия | Назначение |
|------------|--------|------------|
| ASP.NET Core | 9.0 | Web API |
| FluentValidation | 11.x | Валидация |
| NLog | 5.x | Логирование |
| Swagger | 6.x | API документация |

### Frontend

| Технология | Версия | Назначение |
|------------|--------|------------|
| React | 18.x | UI фреймворк |
| TypeScript | 5.x | Типизация |
| Vite | 5.x | Сборщик |
| Zustand | 4.x | State management |
| TanStack Table | 8.x | Таблица |
| Tailwind CSS | 3.x | Стилизация |
| shadcn/ui | latest | UI компоненты |

---

## 🗺️ Roadmap

- [x] Планирование архитектуры
- [ ] **Фаза 1**: Базовая инфраструктура
- [ ] **Фаза 2**: Парсинг и хранение
- [ ] **Фаза 3**: REST API
- [ ] **Фаза 4**: Базовый фронтенд
- [ ] **Фаза 5**: UI компоненты
- [ ] **Фаза 6**: Оптимизация производительности
- [ ] **Фаза 7**: Docker и CI/CD

### Планируемые фичи

- [ ] Поддержка нескольких форматов логов (Serilog, log4net)
- [ ] Real-time мониторинг через WebSocket
- [ ] Темная тема
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
