@echo off
chcp 65001 >nul
setlocal

echo ========================================
echo   nLogMonitor - Development Mode
echo ========================================
echo.

:: Check if Node.js is installed
where node >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Node.js not found. Please install Node.js 20+
    pause
    exit /b 1
)

:: Check if .NET SDK is installed
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET SDK not found. Please install .NET 10 SDK
    pause
    exit /b 1
)

:: Install frontend dependencies if needed
if not exist "client\node_modules" (
    echo [INFO] Installing frontend dependencies...
    cd client
    call npm install
    cd ..
    echo.
)

echo [INFO] Starting Backend (http://localhost:5000)...
start "nLogMonitor API" cmd /k "cd /d %~dp0 && dotnet watch run --project src/nLogMonitor.Api"

:: Wait a bit for backend to start
timeout /t 3 /nobreak >nul

echo [INFO] Starting Frontend (http://localhost:5173)...
start "nLogMonitor Client" cmd /k "cd /d %~dp0client && npm run dev"

echo.
echo ========================================
echo   Both servers started!
echo ========================================
echo.
echo   Backend:  http://localhost:5000
echo   Frontend: http://localhost:5173
echo   Swagger:  http://localhost:5000/swagger
echo.
echo   Close the terminal windows to stop.
echo ========================================
