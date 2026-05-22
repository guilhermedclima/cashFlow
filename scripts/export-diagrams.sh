#!/usr/bin/env bash
# Exports every Mermaid diagram (.mmd) under docs/diagrams/ to PNG and SVG.
#
# Uses mermaid-cli (mmdc). If not installed, it is fetched via npx.
# Useful for reviewers without a Mermaid-aware viewer.

set -euo pipefail

cd "$(dirname "$0")/.."

DIAGRAMS_DIR="docs/diagrams"
EXPORT_DIR="docs/diagrams/exports"
mkdir -p "$EXPORT_DIR"

if ! command -v npx &>/dev/null; then
    echo "ERROR: npx not found. Install Node.js >= 18."
    exit 1
fi

echo "Exporting diagrams to $EXPORT_DIR (formats: PNG + SVG)"
echo "  (first run fetches mermaid-cli - takes ~30s)"
echo ""

for mmd in "$DIAGRAMS_DIR"/*.mmd; do
    name=$(basename "$mmd" .mmd)

    echo "  - $name.png"
    npx -y @mermaid-js/mermaid-cli@latest \
        -i "$mmd" \
        -o "$EXPORT_DIR/$name.png" \
        -b transparent \
        -w 1600 \
        --quiet

    echo "  - $name.svg"
    npx -y @mermaid-js/mermaid-cli@latest \
        -i "$mmd" \
        -o "$EXPORT_DIR/$name.svg" \
        -b transparent \
        --quiet
done

echo ""
echo "Export complete. Files in $EXPORT_DIR/"
ls -la "$EXPORT_DIR"
