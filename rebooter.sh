~/.termux/boot/
mkdir -p ~/.termux/boot
cat > ~/.termux/boot/start-termux-side.sh << 'EOF'
#!/data/data/com.termux/files/usr/bin/bash

# Wait for system to fully boot
sleep 10

# Start tmux session if not already running
if ! tmux has-session -t myproject 2>/dev/null; then
    tmux new-session -d -s myproject \
        "python ~/myproject/termux/termux-side.py"
fi
EOF
chmod +x ~/.termux/boot/start-myproject.sh
nano ~/.bashrc
# Auto-attach to myproject tmux session
if command -v tmux >/dev/null 2>&1; then
    if tmux has-session -t myproject 2>/dev/null; then
        tmux attach -t myproject
    fi
fi














# install termux boot plugin if needed
pkg install -y termux-tools

# make boot directory
mkdir -p ~/.termux/boot

# create boot script
cat > ~/.termux/boot/start-termux-side.sh << 'EOF'
#!/data/data/com.termux/files/usr/bin/bash
sleep 10
python /data/data/com.termux/files/home/myproject/termux/termux-side.py \
  >> /data/data/com.termux/files/home/termux-side.log 2>&1
EOF

# make it executable
chmod +x ~/.termux/boot/start-termux-side.sh

~/.termux/boot/start-termux-side.sh
