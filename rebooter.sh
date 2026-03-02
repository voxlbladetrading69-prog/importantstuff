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
