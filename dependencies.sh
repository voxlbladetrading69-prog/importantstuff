#!/usr/bin/env bash
set -Eeuo pipefail

log() {
  printf '%s\n' "$*"
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || {
    log "❌ Missing required command: $1"
    exit 1
  }
}

PROJECT_DIR="$HOME/myproject/termux"
SOURCE_PKG="com.roblox.clienu"
AUTOEXEC_DIR="/storage/emulated/0/Android/data/$SOURCE_PKG/files/gloop/external/Autoexecute"
TERMUX_SCRIPT_PATH="$PROJECT_DIR/termux-side.py"
ROBLOX_SCRIPT_PATH="$AUTOEXEC_DIR/roblox-side.lua"

TERMUX_SCRIPT_URL="https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/main/termux-side.py"
ROBLOX_SCRIPT_URL="https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/main/roblox-side.lua"

log "📦 Updating Termux packages..."
pkg update -y
pkg upgrade -y

log "📦 Installing required packages..."
pkg install -y python rsync tmux termux-tools curl

require_cmd curl
require_cmd python

if command -v termux-setup-storage >/dev/null 2>&1; then
  log "🔧 Setting up storage access (may prompt once)..."
  termux-setup-storage || log "⚠️ Storage setup returned non-zero; continuing"
fi

log "📁 Creating project directories..."
mkdir -p "$PROJECT_DIR"
mkdir -p "$AUTOEXEC_DIR"

log "⬇️ Downloading controller script (Termux side)..."
curl -fsSL "$TERMUX_SCRIPT_URL" -o "$TERMUX_SCRIPT_PATH"

log "⬇️ Downloading Autoexecute script (Roblox side)..."
curl -fsSL "$ROBLOX_SCRIPT_URL" -o "$ROBLOX_SCRIPT_PATH"

log "✅ Setup complete"
echo
log "Next:"
log "Run controller with:"
log "python $TERMUX_SCRIPT_PATH"

