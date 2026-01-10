@echo off
setlocal

echo ========================================
echo nLogMonitor Desktop - Development Mode
echo ========================================
echo.

set PROJECT_ROOT=%~dp0
cd /d "%PROJECT_ROOT%"

:: Step 1: Start Vite dev server in new window
echo [1/2] Starting Vite dev server...
start "Vite Dev Server" cmd /k "cd /d "%PROJECT_ROOT%client" && npm run dev"

:: Wait for Vite to start
echo Waiting for Vite dev server to start...
timeout /t 3 /nobreak >nul

:: Step 2: Start Desktop application
echo [2/2] Starting Desktop application...
cd /d "%PROJECT_ROOT%src\nLogMonitor.Desktop"
set ASPNETCORE_ENVIRONMENT=Development
dotnet run

echo.
echo Desktop application closed.
pause
