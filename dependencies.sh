#!/data/data/com.termux/files/usr/bin/bash
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
TERMUX_SCRIPT_URL="https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/refs/heads/main/termux-side.py"
ROBLOX_SCRIPT_URL="https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/refs/heads/main/roblox-side.lua"

log "📦 Updating Termux packages..."
pkg update -y
pkg upgrade -y

log "📦 Installing required packages..."
pkg install -y python rsync tmux termux-tools curl

require_cmd curl
require_cmd python

if command -v termux-setup-storage >/dev/null 2>&1; then
  log "🔧 Setting up storage access (may prompt once)..."
  termux-setup-storage || log "⚠️ termux-setup-storage returned non-zero; continuing"
else
  log "⚠️ termux-setup-storage is unavailable; grant storage access manually if needed"
fi

log "📁 Creating project directories..."
mkdir -p "$PROJECT_DIR"
mkdir -p "$AUTOEXEC_DIR"

log "⬇️ Downloading controller script (Termux side)..."
curl --fail --show-error --location "$TERMUX_SCRIPT_URL" -o "$TERMUX_SCRIPT_PATH"

log "⬇️ Downloading Autoexecute script (Roblox side)..."
curl --fail --show-error --location "$ROBLOX_SCRIPT_URL" -o "$ROBLOX_SCRIPT_PATH"

log "✅ Setup complete"
printf '\n'
log "⚠️  Next steps:"
log "   1. Ensure device is rooted and Termux has root permissions."
log "   2. Verify rsync is available to root: su -c \"which rsync\""
log "      If needed: su -c \"ln -s /data/data/com.termux/files/usr/bin/rsync /system/bin/rsync\""
log "   3. Run controller: python $TERMUX_SCRIPT_PATH"
log "   4. Optional tmux: tmux new-session -s roblox 'python $TERMUX_SCRIPT_PATH'"
