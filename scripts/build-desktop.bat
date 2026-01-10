@echo off
setlocal enabledelayedexpansion

echo ========================================
echo nLogMonitor Desktop Build Script
echo ========================================
echo.

set PROJECT_ROOT=%~dp0
cd /d "%PROJECT_ROOT%"

:: Step 1: Build Frontend
echo [1/4] Building frontend...
cd client
call npm run build
if !errorlevel! neq 0 (
    echo ERROR: Frontend build failed!
    exit /b 1
)
cd ..
echo Frontend built successfully.
echo.

:: Step 2: Copy frontend to Desktop wwwroot
echo [2/4] Copying frontend to Desktop wwwroot...
if exist "src\nLogMonitor.Desktop\wwwroot" (
    rmdir /s /q "src\nLogMonitor.Desktop\wwwroot"
)
mkdir "src\nLogMonitor.Desktop\wwwroot"
xcopy /s /e /y "client\dist\*" "src\nLogMonitor.Desktop\wwwroot\"
if !errorlevel! neq 0 (
    echo ERROR: Failed to copy frontend files!
    exit /b 1
)
echo Frontend copied successfully.
echo.

:: Step 3: Build Desktop project
echo [3/4] Building Desktop project...
dotnet build "src\nLogMonitor.Desktop\nLogMonitor.Desktop.csproj" -c Release
if !errorlevel! neq 0 (
    echo ERROR: Desktop build failed!
    exit /b 1
)
echo Desktop built successfully.
echo.

:: Step 4: Publish Desktop (self-contained, single file)
echo [4/4] Publishing Desktop for Windows x64...
dotnet publish "src\nLogMonitor.Desktop\nLogMonitor.Desktop.csproj" -c Release -r win-x64 --self-contained -o "publish\desktop\win-x64"
if !errorlevel! neq 0 (
    echo ERROR: Desktop publish failed!
    exit /b 1
)
echo Desktop published successfully.
echo.

echo ========================================
echo Build completed!
echo Output: publish\desktop\win-x64
echo ========================================
echo.
echo Run: publish\desktop\win-x64\nLogMonitor.Desktop.exe
