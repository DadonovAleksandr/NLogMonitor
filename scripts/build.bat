@echo off
chcp 65001 >nul
setlocal

echo ========================================
echo   nLogMonitor - Production Build
echo ========================================
echo.

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set CLIENT_DIR=%PROJECT_ROOT%\client
set API_DIR=%PROJECT_ROOT%\src\nLogMonitor.Api
set WWWROOT_DIR=%API_DIR%\wwwroot
set PUBLISH_DIR=%PROJECT_ROOT%\publish

:: Check prerequisites
where node >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Node.js not found. Please install Node.js 20+
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET SDK not found. Please install .NET 10 SDK
    pause
    exit /b 1
)

:: Step 1: Build Frontend
echo [1/4] Building frontend...
cd /d "%CLIENT_DIR%"

if not exist "node_modules" (
    echo       Installing dependencies...
    call npm install
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] npm install failed
        pause
        exit /b 1
    )
)

call npm run build
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Frontend build failed
    pause
    exit /b 1
)
echo       Frontend built successfully!
echo.

:: Step 2: Copy dist to wwwroot
echo [2/4] Copying frontend to wwwroot...
if exist "%WWWROOT_DIR%" (
    rmdir /s /q "%WWWROOT_DIR%"
)
mkdir "%WWWROOT_DIR%"
xcopy /e /i /q "%CLIENT_DIR%\dist\*" "%WWWROOT_DIR%\"
echo       Copied to %WWWROOT_DIR%
echo.

:: Step 3: Build Backend
echo [3/4] Building backend...
cd /d "%PROJECT_ROOT%"
dotnet build -c Release
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Backend build failed
    pause
    exit /b 1
)
echo       Backend built successfully!
echo.

:: Step 4: Publish
echo [4/4] Publishing...
if exist "%PUBLISH_DIR%" (
    rmdir /s /q "%PUBLISH_DIR%"
)
dotnet publish "%PROJECT_ROOT%\src\nLogMonitor.Api" -c Release -o "%PUBLISH_DIR%" --no-build
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Publish failed
    pause
    exit /b 1
)
echo       Published to %PUBLISH_DIR%
echo.

echo ========================================
echo   Build completed successfully!
echo ========================================
echo.
echo   Output: %PUBLISH_DIR%
echo.
echo   To run:
echo     cd publish
echo     nLogMonitor.Api.exe
echo.
echo   Then open: http://localhost:5000
echo ========================================

endlocal
