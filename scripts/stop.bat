@echo off
chcp 65001 >nul
setlocal

echo ========================================
echo   nLogMonitor - Stopping Servers
echo ========================================
echo.

set STOPPED_SOMETHING=0

:: Stop Backend (dotnet processes for nLogMonitor.Api)
echo [INFO] Stopping backend processes...

:: Find and kill dotnet processes with nLogMonitor
for /f "tokens=2" %%a in ('tasklist /fi "imagename eq dotnet.exe" /fo list 2^>nul ^| find "PID:"') do (
    wmic process where "ProcessId=%%a" get CommandLine 2>nul | find /i "nLogMonitor" >nul
    if not errorlevel 1 (
        echo        Killing dotnet process %%a
        taskkill /pid %%a /f >nul 2>&1
        set STOPPED_SOMETHING=1
    )
)

:: Kill any dotnet watch processes
taskkill /f /im dotnet.exe /fi "WINDOWTITLE eq nLogMonitor*" >nul 2>&1
if not errorlevel 1 (
    echo        Killed dotnet processes by window title
    set STOPPED_SOMETHING=1
)

echo.

:: Stop Frontend (node processes for vite/npm)
echo [INFO] Stopping frontend processes...

:: Find and kill node processes (vite dev server)
for /f "tokens=2" %%a in ('tasklist /fi "imagename eq node.exe" /fo list 2^>nul ^| find "PID:"') do (
    wmic process where "ProcessId=%%a" get CommandLine 2>nul | find /i "vite" >nul
    if not errorlevel 1 (
        echo        Killing node/vite process %%a
        taskkill /pid %%a /f >nul 2>&1
        set STOPPED_SOMETHING=1
    )
)

:: Kill by window title (if started with start command)
taskkill /f /fi "WINDOWTITLE eq nLogMonitor Client*" >nul 2>&1
if not errorlevel 1 (
    echo        Killed node processes by window title
    set STOPPED_SOMETHING=1
)

taskkill /f /fi "WINDOWTITLE eq nLogMonitor API*" >nul 2>&1
if not errorlevel 1 (
    echo        Killed API processes by window title
    set STOPPED_SOMETHING=1
)

echo.

if %STOPPED_SOMETHING%==1 (
    echo ========================================
    echo   Servers stopped!
    echo ========================================
) else (
    echo ========================================
    echo   No running servers found
    echo ========================================
)

endlocal
