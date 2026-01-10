@echo off
echo ========================================
echo Stopping nLogMonitor Desktop Dev Mode
echo ========================================
echo.

:: Stop Desktop application
echo Stopping Desktop application...
taskkill /IM "nLogMonitor.Desktop.exe" /F 2>nul
if %errorlevel%==0 (
    echo Desktop application stopped.
) else (
    echo Desktop application was not running.
)

:: Stop Vite dev server (node process on port 5173)
echo Stopping Vite dev server...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5173.*LISTENING"') do (
    taskkill /PID %%a /F 2>nul
    echo Vite dev server stopped (PID: %%a).
)

:: Also kill any orphaned dotnet processes for this project
taskkill /IM "dotnet.exe" /FI "WINDOWTITLE eq *nLogMonitor.Desktop*" /F 2>nul

echo.
echo All processes stopped.
pause
