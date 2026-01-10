#!/bin/bash
# nLogMonitor - Development Mode
# Run: chmod +x start-dev.sh && ./start-dev.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLIENT_DIR="$SCRIPT_DIR/client"
API_PROJECT="$SCRIPT_DIR/src/nLogMonitor.Api"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo "========================================"
echo "  nLogMonitor - Development Mode"
echo "========================================"
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo -e "${RED}[ERROR]${NC} Node.js not found. Please install Node.js 20+"
    exit 1
fi
echo -e "${GREEN}[OK]${NC} Node.js found: $(node --version)"

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}[ERROR]${NC} .NET SDK not found. Please install .NET 10 SDK"
    exit 1
fi
echo -e "${GREEN}[OK]${NC} .NET SDK found: $(dotnet --version)"
echo ""

# Install frontend dependencies if needed
if [ ! -d "$CLIENT_DIR/node_modules" ]; then
    echo -e "${YELLOW}[INFO]${NC} Installing frontend dependencies..."
    cd "$CLIENT_DIR"
    npm install
    cd "$SCRIPT_DIR"
    echo ""
fi

# Create logs directory
mkdir -p "$SCRIPT_DIR/.dev-logs"

# Start Backend
echo -e "${CYAN}[INFO]${NC} Starting Backend (http://localhost:5000)..."
cd "$SCRIPT_DIR"
nohup dotnet watch run --project "$API_PROJECT" > "$SCRIPT_DIR/.dev-logs/backend.log" 2>&1 &
BACKEND_PID=$!
echo "       Backend PID: $BACKEND_PID"

# Wait a bit for backend to start
sleep 3

# Start Frontend
echo -e "${CYAN}[INFO]${NC} Starting Frontend (http://localhost:5173)..."
cd "$CLIENT_DIR"
nohup npm run dev > "$SCRIPT_DIR/.dev-logs/frontend.log" 2>&1 &
FRONTEND_PID=$!
echo "       Frontend PID: $FRONTEND_PID"

cd "$SCRIPT_DIR"

echo ""
echo "========================================"
echo -e "  ${GREEN}Both servers started!${NC}"
echo "========================================"
echo ""
echo "  Backend:  http://localhost:5000"
echo "  Frontend: http://localhost:5173"
echo "  Swagger:  http://localhost:5000/swagger"
echo ""
echo "  Logs:"
echo "    Backend:  .dev-logs/backend.log"
echo "    Frontend: .dev-logs/frontend.log"
echo ""
echo "  To stop: ./stop.sh"
echo "========================================"
