#!/bin/bash
# nLogMonitor - Production Build
# Run: chmod +x build.sh && ./build.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLIENT_DIR="$SCRIPT_DIR/client"
API_DIR="$SCRIPT_DIR/src/nLogMonitor.Api"
WWWROOT_DIR="$API_DIR/wwwroot"
PUBLISH_DIR="$SCRIPT_DIR/publish"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "========================================"
echo "  nLogMonitor - Production Build"
echo "========================================"
echo ""

# Check prerequisites
if ! command -v node &> /dev/null; then
    echo -e "${RED}[ERROR]${NC} Node.js not found. Please install Node.js 20+"
    exit 1
fi
echo -e "${GREEN}[OK]${NC} Node.js found: $(node --version)"

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}[ERROR]${NC} .NET SDK not found. Please install .NET 10 SDK"
    exit 1
fi
echo -e "${GREEN}[OK]${NC} .NET SDK found: $(dotnet --version)"
echo ""

# Step 1: Build Frontend
echo -e "${YELLOW}[1/4]${NC} Building frontend..."
cd "$CLIENT_DIR"

if [ ! -d "node_modules" ]; then
    echo "       Installing dependencies..."
    npm install || {
        echo -e "${RED}[ERROR]${NC} npm install failed"
        exit 1
    }
fi

npm run build || {
    echo -e "${RED}[ERROR]${NC} Frontend build failed"
    exit 1
}
echo -e "       ${GREEN}Frontend built successfully!${NC}"
echo ""

# Step 2: Copy dist to wwwroot
echo -e "${YELLOW}[2/4]${NC} Copying frontend to wwwroot..."
if [ -d "$WWWROOT_DIR" ]; then
    rm -rf "$WWWROOT_DIR"
fi
mkdir -p "$WWWROOT_DIR"
cp -r "$CLIENT_DIR/dist/"* "$WWWROOT_DIR/"
echo "       Copied to $WWWROOT_DIR"
echo ""

# Step 3: Build Backend
echo -e "${YELLOW}[3/4]${NC} Building backend..."
cd "$SCRIPT_DIR"
dotnet build -c Release || {
    echo -e "${RED}[ERROR]${NC} Backend build failed"
    exit 1
}
echo -e "       ${GREEN}Backend built successfully!${NC}"
echo ""

# Step 4: Publish
echo -e "${YELLOW}[4/4]${NC} Publishing..."
if [ -d "$PUBLISH_DIR" ]; then
    rm -rf "$PUBLISH_DIR"
fi
dotnet publish src/nLogMonitor.Api -c Release -o "$PUBLISH_DIR" --no-build || {
    echo -e "${RED}[ERROR]${NC} Publish failed"
    exit 1
}
echo "       Published to $PUBLISH_DIR"
echo ""

echo "========================================"
echo -e "  ${GREEN}Build completed successfully!${NC}"
echo "========================================"
echo ""
echo "  Output: $PUBLISH_DIR"
echo ""
echo "  To run:"
echo "    cd publish"
echo "    ./nLogMonitor.Api"
echo ""
echo "  Then open: http://localhost:5000"
echo "========================================"
