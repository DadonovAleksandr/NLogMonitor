#!/bin/bash
# nLogMonitor - Stop Development Servers
# Run: chmod +x stop.sh && ./stop.sh

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "========================================"
echo "  nLogMonitor - Stopping Servers"
echo "========================================"
echo ""

STOPPED_SOMETHING=false

# Stop Backend (dotnet watch for nLogMonitor.Api)
echo -e "${YELLOW}[INFO]${NC} Stopping backend processes..."

# Find and kill dotnet processes related to nLogMonitor
DOTNET_PIDS=$(pgrep -f "dotnet.*nLogMonitor" 2>/dev/null || true)
if [ -n "$DOTNET_PIDS" ]; then
    echo "       Found dotnet processes: $DOTNET_PIDS"
    for pid in $DOTNET_PIDS; do
        kill "$pid" 2>/dev/null && echo -e "       ${GREEN}Killed${NC} dotnet process $pid" || true
    done
    STOPPED_SOMETHING=true
else
    echo "       No dotnet processes found"
fi

# Also try to kill by watch command
WATCH_PIDS=$(pgrep -f "dotnet watch" 2>/dev/null || true)
if [ -n "$WATCH_PIDS" ]; then
    for pid in $WATCH_PIDS; do
        kill "$pid" 2>/dev/null && echo -e "       ${GREEN}Killed${NC} dotnet watch process $pid" || true
    done
    STOPPED_SOMETHING=true
fi

echo ""

# Stop Frontend (vite/npm dev server)
echo -e "${YELLOW}[INFO]${NC} Stopping frontend processes..."

# Find and kill vite processes
VITE_PIDS=$(pgrep -f "vite" 2>/dev/null || true)
if [ -n "$VITE_PIDS" ]; then
    echo "       Found vite processes: $VITE_PIDS"
    for pid in $VITE_PIDS; do
        kill "$pid" 2>/dev/null && echo -e "       ${GREEN}Killed${NC} vite process $pid" || true
    done
    STOPPED_SOMETHING=true
else
    echo "       No vite processes found"
fi

# Find and kill node processes running npm dev in client directory
NODE_PIDS=$(pgrep -f "npm.*dev" 2>/dev/null || true)
if [ -n "$NODE_PIDS" ]; then
    echo "       Found npm dev processes: $NODE_PIDS"
    for pid in $NODE_PIDS; do
        kill "$pid" 2>/dev/null && echo -e "       ${GREEN}Killed${NC} npm process $pid" || true
    done
    STOPPED_SOMETHING=true
fi

echo ""

if [ "$STOPPED_SOMETHING" = true ]; then
    echo "========================================"
    echo -e "  ${GREEN}Servers stopped!${NC}"
    echo "========================================"
else
    echo "========================================"
    echo -e "  ${YELLOW}No running servers found${NC}"
    echo "========================================"
fi
