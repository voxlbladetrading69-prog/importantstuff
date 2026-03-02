pkg update && pkg upgrade -y \
&& pkg install -y python ncurses \
&& pkg install tmux
&& pkg reinstall -y python \
&& termux-setup-storage \
&& mkdir -p ~/myproject/termux \
&& mkdir -p /storage/emulated/0/Android/data/com.roblox.clienu/files/gloop/external/Autoexecute \
&& curl -L "https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/refs/heads/main/termux-side.py" \
   -o ~/myproject/termux/termux-side.py \
&& curl -L "https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/refs/heads/main/roblox-side.lua" \
   -o /storage/emulated/0/Android/data/com.roblox.clienu/files/gloop/external/Autoexecute/roblox-side.lua \
# && python ~/myproject/termux/termux-side.py
