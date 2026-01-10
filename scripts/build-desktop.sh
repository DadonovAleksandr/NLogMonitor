#!/bin/bash
set -e

echo "========================================"
echo "nLogMonitor Desktop Build Script"
echo "========================================"
echo ""

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

# Detect OS (only Linux is supported)
OS="unknown"
RID="unknown"
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="linux"
    RID="linux-x64"
else
    echo "ERROR: Unsupported OS. Only Linux is supported for Desktop build."
    echo "For Windows, use build-desktop.bat"
    exit 1
fi

# Step 1: Build Frontend
echo "[1/4] Building frontend..."
cd "$PROJECT_ROOT/client"
npm run build
cd "$PROJECT_ROOT"
echo "Frontend built successfully."
echo ""

# Step 2: Copy frontend to Desktop wwwroot
echo "[2/4] Copying frontend to Desktop wwwroot..."
rm -rf "$PROJECT_ROOT/src/nLogMonitor.Desktop/wwwroot"
mkdir -p "$PROJECT_ROOT/src/nLogMonitor.Desktop/wwwroot"
cp -r "$PROJECT_ROOT/client/dist/"* "$PROJECT_ROOT/src/nLogMonitor.Desktop/wwwroot/"
echo "Frontend copied successfully."
echo ""

# Step 3: Build Desktop project
echo "[3/4] Building Desktop project..."
dotnet build "$PROJECT_ROOT/src/nLogMonitor.Desktop/nLogMonitor.Desktop.csproj" -c Release
echo "Desktop built successfully."
echo ""

# Step 4: Publish Desktop (self-contained)
echo "[4/4] Publishing Desktop for $OS ($RID)..."
dotnet publish "$PROJECT_ROOT/src/nLogMonitor.Desktop/nLogMonitor.Desktop.csproj" -c Release -r "$RID" --self-contained -o "$PROJECT_ROOT/publish/desktop/$RID"
echo "Desktop published successfully."
echo ""

echo "========================================"
echo "Build completed!"
echo "Output: $PROJECT_ROOT/publish/desktop/$RID"
echo "========================================"
echo ""
echo "Run: $PROJECT_ROOT/publish/desktop/$RID/nLogMonitor.Desktop"
