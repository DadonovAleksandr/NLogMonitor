# 🚀 Руководство по деплою

## 📋 Содержание

- [Обзор](#-обзор)
- [Production сборка](#-production-сборка)
- [Docker](#-docker)
- [CI/CD](#-cicd)
- [Переменные окружения](#-переменные-окружения)
- [Мониторинг](#-мониторинг)

---

## 📖 Обзор

nLogMonitor поддерживает несколько вариантов деплоя:

| Вариант | Сложность | Рекомендуется для |
|---------|-----------|-------------------|
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
| `ASPNETCORE_ENVIRONMENT` | `Production` | Окружение |
| `ASPNETCORE_URLS` | `http://+:5000` | URL для прослушивания |
| `SessionStorage__DefaultTtlMinutes` | `60` | TTL сессий (мин) |
| `SessionStorage__MaxFileSizeMb` | `100` | Макс. размер файла |
| `Logging__LogLevel__Default` | `Information` | Уровень логов |

### Пример .env файла

```env
# .env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
SessionStorage__DefaultTtlMinutes=120
SessionStorage__MaxFileSizeMb=200
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
