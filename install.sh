#!/bin/bash
set -e

PACKAGE_ID="Cratis.Cli"
PROJECT_DIR="Source/Cli"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

cd "$SCRIPT_DIR"

echo "Packing $PACKAGE_ID..."
dotnet pack "$PROJECT_DIR" -o ./artifacts

NUPKG=$(ls -1t ./artifacts/${PACKAGE_ID}.*.nupkg 2>/dev/null | head -1)
if [ -z "$NUPKG" ]; then
    echo "Error: No .nupkg found in ./artifacts"
    exit 1
fi

VERSION=$(basename "$NUPKG" | sed "s/${PACKAGE_ID}\.\(.*\)\.nupkg/\1/")
echo "Built version: $VERSION"

echo "Uninstalling existing global tool..."
dotnet tool uninstall -g "$PACKAGE_ID" 2>/dev/null || true

echo "Installing from local package..."
dotnet tool install -g "$PACKAGE_ID" --version "$VERSION" --add-source ./artifacts

echo ""
echo "Done! Run 'cratis' to verify."

echo ""
echo "Installing shell completions..."
cratis completions install --force || true
echo "If completions were just installed, restart your shell or run: source ~/.zshrc (or ~/.bashrc)"
