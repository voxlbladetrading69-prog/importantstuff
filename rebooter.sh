#!/data/data/com.termux/files/usr/bin/bash
set -Eeuo pipefail

BOOT_DIR="$HOME/.termux/boot"
BOOT_SCRIPT="$BOOT_DIR/start-controller.sh"
PROJECT_SCRIPT="$HOME/myproject/termux/termux-side.py"
LOG_FILE="$HOME/termux-side.log"

mkdir -p "$BOOT_DIR"

cat > "$BOOT_SCRIPT" <<EOF
#!/data/data/com.termux/files/usr/bin/bash
set -Eeuo pipefail

sleep 10

if [ ! -f "$PROJECT_SCRIPT" ]; then
  echo "Missing controller script: $PROJECT_SCRIPT" >> "$LOG_FILE"
  exit 1
fi

while true; do
  echo "Starting controller at \$(date)" >> "$LOG_FILE"
  python "$PROJECT_SCRIPT" >> "$LOG_FILE" 2>&1
  echo "Controller crashed. Restarting in 5 seconds..." >> "$LOG_FILE"
  sleep 5
done
EOF

chmod +x "$BOOT_SCRIPT"

echo "✅ Modern boot script written to: $BOOT_SCRIPT"
echo "✅ Logs will be written to: $LOG_FILE"
