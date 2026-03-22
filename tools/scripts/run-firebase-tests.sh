#!/bin/bash
# ============================================================
# RiceFactory — Firebase Emulator Test Runner
#
# Firebase emulator baslat, testleri calistir, emulator kapat.
# Kullanim: ./tools/scripts/run-firebase-tests.sh
# ============================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
FIREBASE_DIR="$PROJECT_ROOT/packages/firebase-backend"

echo "========================================"
echo "  RiceFactory Firebase Emulator Tests"
echo "========================================"
echo ""

# Firebase backend dizinine gec
cd "$FIREBASE_DIR"

# Node modules kontrolu
if [ ! -d "functions/node_modules" ]; then
  echo "[INFO] functions/node_modules bulunamadi, npm install calistiriliyor..."
  cd functions && npm install && cd ..
fi

# Testleri emulator icinde calistir
echo "[INFO] Firebase Emulator baslatiliyor ve testler calistiriliyor..."
echo ""

firebase emulators:exec --project ricefactory-game "cd functions && npm test"

echo ""
echo "========================================"
echo "  Testler tamamlandi!"
echo "========================================"
