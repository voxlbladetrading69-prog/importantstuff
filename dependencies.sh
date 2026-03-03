#!/data/data/com.termux/files/usr/bin/bash
set -e  # exit on any error

echo "📦 Updating Termux packages..."
pkg update -y && pkg upgrade -y

echo "📦 Installing required packages..."
pkg install -y python rsync tmux

echo "🔧 Setting up storage access..."
termux-setup-storage

echo "📁 Creating project directories..."
mkdir -p ~/myproject/termux

# Ensure the target Autoexecute directory exists (parent may not exist)
AUTOEXEC_DIR="/storage/emulated/0/Android/data/com.roblox.clienu/files/gloop/external/Autoexecute"
mkdir -p "$AUTOEXEC_DIR"

echo "⬇️ Downloading controller script (Termux side)..."
curl -L "https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/refs/heads/main/termux-side.py" \
     -o ~/myproject/termux/termux-side.py

echo "⬇️ Downloading Autoexecute script (Roblox side)..."
curl -L "https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/refs/heads/main/roblox-side.lua" \
     -o "$AUTOEXEC_DIR/roblox-side.lua"

echo "✅ Setup complete!"
echo ""
echo "⚠️  Next steps:"
echo "   1. Make sure your device is rooted and Termux has root permissions."
echo "   2. Test that 'rsync' is available to root:   su -c \"which rsync\""
echo "      If it fails, you may need to symlink it:"
echo "         su -c \"ln -s /data/data/com.termux/files/usr/bin/rsync /system/bin/rsync\""
echo "   3. Run the controller:   python ~/myproject/termux/termux-side.py"
echo "   4. (Optional) Use tmux to keep it running:   tmux new-session -s roblox 'python ~/myproject/termux/termux-side.py'"
