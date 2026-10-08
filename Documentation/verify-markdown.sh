#!/bin/bash

# Markdown Verification Script
# This script runs the same markdown linting and link verification that runs in CI

# No `set -e` here: both steps have to run so the summary reports everything that failed, not just the first.
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Site-absolute links (`/prologue/`, `/arc/backend/...`) point at the aggregated documentation site, which is built
# from every product repository at once - there is nothing in this repository for them to resolve to, and the
# Documentation repository's own build verifies them. Everything else, including every relative link within this
# repository and every external link, is checked.
#
# linkinator serves the scanned files from a local web server, so every internal link resolves to
# http://127.0.0.1:<port>/<path>. Skipping what falls outside /Documentation/ therefore skips exactly the
# site-absolute paths, by rule rather than by product name - this repository has its own Documentation/arc/ folder,
# so a name-based skip of 'arc/' would also skip real internal links. The pattern must never match the crawl root.
# Keep in sync with .github/workflows/markdown-verification.yml.
LINKS_TO_SKIP='^https?://(localhost|127\.0\.0\.1):[0-9]+/(?!Documentation/)'

echo "=========================================="
echo "Markdown Verification"
echo "=========================================="
echo ""

# Check if running from repository root or Documentation folder
if [ "$(basename "$PWD")" = "Documentation" ]; then
    cd ..
fi

echo "Working directory: $PWD"
echo ""

# Step 1: Markdown Linting
echo "=========================================="
echo "Step 1: Running markdownlint..."
echo "=========================================="
echo ""

if ! command -v npx &> /dev/null; then
    echo "Error: npx is not installed. Please install Node.js and npm."
    exit 1
fi

npx markdownlint-cli2 "Documentation/**/*.md"
LINT_EXIT_CODE=$?

echo ""
if [ $LINT_EXIT_CODE -eq 0 ]; then
    echo "✓ Markdown linting passed!"
else
    echo "✗ Markdown linting failed with exit code $LINT_EXIT_CODE"
fi
echo ""

# Step 2: Link Verification
echo "=========================================="
echo "Step 2: Running link verification..."
echo "=========================================="
echo ""
echo "This may take a few minutes to check all links..."
echo ""

npx --yes linkinator@8.1.0 "Documentation/**/*.md" --markdown --recurse --verbosity warning --status-code "403:ok" --status-code "429:warn" --skip "$LINKS_TO_SKIP"
LINK_EXIT_CODE=$?

echo ""
if [ $LINK_EXIT_CODE -eq 0 ]; then
    echo "✓ Link verification passed!"
else
    echo "✗ Link verification failed with exit code $LINK_EXIT_CODE"
fi
echo ""

# Final summary
echo "=========================================="
echo "Summary"
echo "=========================================="
if [ $LINT_EXIT_CODE -eq 0 ] && [ $LINK_EXIT_CODE -eq 0 ]; then
    echo "✓ All checks passed!"
    exit 0
else
    echo "✗ Some checks failed:"
    [ $LINT_EXIT_CODE -ne 0 ] && echo "  - Markdown linting"
    [ $LINK_EXIT_CODE -ne 0 ] && echo "  - Link verification"
    exit 1
fi
