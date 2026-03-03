#!/data/data/com.termux/files/usr/bin/bash
set -Eeuo pipefail

BOOT_DIR="$HOME/.termux/boot"
BOOT_SCRIPT="$BOOT_DIR/start-termux-side.sh"
PROJECT_SCRIPT="$HOME/myproject/termux/termux-side.py"
SESSION_NAME="myproject"
LOG_FILE="$HOME/termux-side.log"
BASHRC_FILE="$HOME/.bashrc"

mkdir -p "$BOOT_DIR"

cat > "$BOOT_SCRIPT" <<BOOT
#!/data/data/com.termux/files/usr/bin/bash
set -Eeuo pipefail

sleep 10

if ! command -v tmux >/dev/null 2>&1; then
  echo "tmux is not installed" >> "$LOG_FILE"
  exit 1
fi

if [ ! -f "$PROJECT_SCRIPT" ]; then
  echo "Missing controller script: $PROJECT_SCRIPT" >> "$LOG_FILE"
  exit 1
fi

if ! tmux has-session -t "$SESSION_NAME" 2>/dev/null; then
  tmux new-session -d -s "$SESSION_NAME" \
    "python '$PROJECT_SCRIPT' >> '$LOG_FILE' 2>&1"
fi
BOOT

chmod +x "$BOOT_SCRIPT"

if ! grep -q "Auto-attach to $SESSION_NAME tmux session" "$BASHRC_FILE" 2>/dev/null; then
  cat >> "$BASHRC_FILE" <<'BASHRC'

# Auto-attach to myproject tmux session
if command -v tmux >/dev/null 2>&1; then
  if tmux has-session -t "myproject" 2>/dev/null && [ -z "$TMUX" ]; then
    tmux attach -t "myproject"
  fi
fi
BASHRC
fi

echo "✅ Boot script written to: $BOOT_SCRIPT"
echo "✅ Logs will be written to: $LOG_FILE"
