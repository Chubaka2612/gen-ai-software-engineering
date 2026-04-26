#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/../src/AiCraft.Banking/AiCraft.Banking.csproj"

echo "============================================"
echo "  AiCraft.Banking API"
echo "============================================"
echo "  Project : $PROJECT"
echo "  API     : http://localhost:5092"
echo "  Swagger : http://localhost:5092/swagger"
echo "============================================"
echo ""

dotnet run --project "$PROJECT"
