# 🚀 Руководство по деплою

## 📋 Содержание

- [Обзор](#-обзор)
- [Production сборка](#-production-сборка)
- [Desktop Build](#-desktop-build)
- [Docker](#-docker)
- [CI/CD](#-cicd)
- [Переменные окружения](#-переменные-окружения)
- [Мониторинг](#-мониторинг)

---

## 📖 Обзор

nLogMonitor поддерживает несколько вариантов деплоя:

| Вариант | Сложность | Рекомендуется для |
|---------|-----------|-------------------|
| Desktop (Photino) | Низкая | Локальное использование, портативность |
| Docker Compose | Низкая | Локальная разработка, небольшие команды |
| Kubernetes | Высокая | Enterprise, масштабирование |
| Azure App Service | Средняя | .NET-экосистема |
| VPS (nginx) | Средняя | Бюджетные решения |

---

## 🔨 Production сборка

### Backend

```bash
# Release сборка
dotnet publish src/nLogMonitor.Api -c Release -o ./publish

# Self-contained (без установленного .NET)
dotnet publish src/nLogMonitor.Api -c Release -o ./publish \
  --self-contained true \
  -r linux-x64
```

### Frontend

```bash
cd client

# Production сборка
npm run build

# Результат в ./dist
```

### Объединенная сборка

```bash
# Backend
dotnet publish src/nLogMonitor.Api -c Release -o ./publish

# Frontend → wwwroot
cd client && npm run build
cp -r dist/* ../publish/wwwroot/
```

---

## 💻 Desktop Build

Desktop приложение на базе Photino.NET — автономный executable с встроенным ASP.NET Core сервером и WebView.

### Особенности

- **Self-contained** — не требует установленного .NET Runtime
- **Embedded Server** — ASP.NET Core работает в фоновом потоке
- **Native Dialogs** — системные диалоги открытия файлов
- **Direct File Access** — без ограничения на размер файла
- **Cross-platform** — Windows, Linux, macOS

### Сборка

#### Windows (x64)

```bash
# Используйте готовый скрипт (рекомендуется)
build-desktop.bat

# Или вручную:
# 1. Сборка frontend
cd client
npm run build

# 2. Копирование в Desktop wwwroot
xcopy /E /I /Y dist ..\src\nLogMonitor.Desktop\wwwroot

# 3. Публикация Desktop приложения
cd ..
dotnet publish src/nLogMonitor.Desktop -c Release -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -o publish/desktop/win-x64

# Результат в publish/desktop/win-x64/
# Размер: ~50 MB
```

#### Linux (x64)

```bash
# Используйте готовый скрипт (рекомендуется)
./build-desktop.sh

# Или вручную:
# 1. Сборка frontend
cd client
npm run build

# 2. Копирование в Desktop wwwroot
cp -r dist/* ../src/nLogMonitor.Desktop/wwwroot/

# 3. Публикация Desktop приложения
cd ..
dotnet publish src/nLogMonitor.Desktop -c Release -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -o publish/desktop/linux-x64

# Результат в publish/desktop/linux-x64/
```

#### macOS (ARM64 / x64)

```bash
# macOS Apple Silicon (M1/M2/M3)
dotnet publish src/nLogMonitor.Desktop -c Release -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -o publish/desktop/osx-arm64

# macOS Intel
dotnet publish src/nLogMonitor.Desktop -c Release -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -o publish/desktop/osx-x64
```

### Запуск

```bash
# Windows
cd publish/desktop/win-x64
nLogMonitor.Desktop.exe

# Linux
cd publish/desktop/linux-x64
chmod +x nLogMonitor.Desktop
./nLogMonitor.Desktop

# macOS
cd publish/desktop/osx-arm64
chmod +x nLogMonitor.Desktop
./nLogMonitor.Desktop
```

### Отличия от Web версии

| Параметр | Web | Desktop |
|----------|:---:|:-------:|
| Требует .NET Runtime | ✅ Да | ❌ Self-contained |
| Требует браузер | ✅ Да | ❌ Embedded WebView |
| Размер файла | ~10 MB | ~50 MB |
| Ограничение размера лог-файла | 100 MB | Без ограничений |
| Открытие директорий | ❌ | ✅ |
| История файлов | Per-session | Persistent (AppData) |
| CORS конфигурация | Требуется | Не требуется |

### Портативная версия

Desktop приложение является портативным — все файлы в одной папке:

```
publish/desktop/win-x64/
├── nLogMonitor.Desktop.exe         # Основной executable
├── nLogMonitor.Api.dll             # ASP.NET Core API
├── nLogMonitor.*.dll               # Библиотеки приложения
├── wwwroot/                        # Vue 3 frontend
└── [runtime dependencies]          # .NET Runtime
```

Можно скопировать эту папку на любую машину с поддерживаемой ОС и запустить без установки.

---

## 🐳 Docker

### Dockerfile (Multi-stage)

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY ["src/", "src/"]
COPY ["nLogMonitor.sln", "."]
RUN dotnet restore
RUN dotnet publish src/nLogMonitor.Api -c Release -o /app/publish

FROM node:20-alpine AS frontend-build
WORKDIR /app
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend-build /app/publish .
COPY --from=frontend-build /app/dist ./wwwroot
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "nLogMonitor.Api.dll"]
```

> **Примечание:** Docker используется только для Web режима. Desktop приложение не требует контейнеризации, так как является self-contained executable.

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - SessionStorage__DefaultTtlMinutes=60
    volumes:
      - logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  logs:
```

### Команды Docker

```bash
# Сборка образа
docker build -t nlogmonitor:latest .

# Запуск контейнера
docker run -d \
  --name nlogmonitor \
  -p 5000:5000 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  nlogmonitor:latest

# С docker-compose
docker-compose up -d

# Логи
docker-compose logs -f app

# Остановка
docker-compose down
```

---

## 🔄 CI/CD

### GitHub Actions

```yaml
# .github/workflows/ci.yml
name: CI/CD

on:
  push:
    branches: [master, develop]
  pull_request:
    branches: [master]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: client/package-lock.json

      - name: Restore & Test Backend
        run: |
          dotnet restore
          dotnet test --no-restore --verbosity normal

      - name: Test Frontend
        working-directory: client
        run: |
          npm ci
          npm run lint
          npm run type-check
          npm run test -- --run

  build:
    needs: test
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/master'

    steps:
      - uses: actions/checkout@v4

      - name: Login to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push Docker image
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
```

### GitLab CI

```yaml
# .gitlab-ci.yml
stages:
  - test
  - build
  - deploy

variables:
  DOCKER_IMAGE: $CI_REGISTRY_IMAGE

test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - dotnet restore
    - dotnet test

build:
  stage: build
  image: docker:latest
  services:
    - docker:dind
  script:
    - docker login -u $CI_REGISTRY_USER -p $CI_REGISTRY_PASSWORD $CI_REGISTRY
    - docker build -t $DOCKER_IMAGE:$CI_COMMIT_SHA .
    - docker push $DOCKER_IMAGE:$CI_COMMIT_SHA
  only:
    - master

deploy:
  stage: deploy
  script:
    - echo "Deploy to production"
  only:
    - master
  when: manual
```

---

## ⚙️ Переменные окружения

| Переменная | По умолчанию | Описание |
|------------|--------------|----------|
| `App__Mode` | `Web` | Режим работы (Web/Desktop) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Окружение |
| `ASPNETCORE_URLS` | `http://+:5000` | URL для прослушивания |
| `SessionSettings__FallbackTtlMinutes` | `5` | Fallback TTL сессий (страховка) |
| `FileSettings__MaxFileSizeMB` | `100` | Макс. размер файла |
| `Logging__LogLevel__Default` | `Information` | Уровень логов |

### Пример .env файла

```env
# .env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
App__Mode=Desktop
SessionSettings__FallbackTtlMinutes=10
FileSettings__MaxFileSizeMB=200
```

---

## 📊 Мониторинг

### Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck<StorageHealthCheck>("storage");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Prometheus Metrics (опционально)

```bash
# Добавить пакет
dotnet add package prometheus-net.AspNetCore

# Program.cs
app.UseMetricServer();
app.UseHttpMetrics();
```

### Логирование в Production

```xml
<!-- NLog.config -->
<targets>
  <target name="file" xsi:type="File"
          fileName="/app/logs/nlogmonitor-${shortdate}.log"
          archiveEvery="Day"
          maxArchiveFiles="30" />
</targets>
```

---

## 🔗 Связанные документы

- [Configuration](CONFIGURATION.md)
- [Architecture](ARCHITECTURE.md)
