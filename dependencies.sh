pkg update && pkg upgrade -y \
&& pkg install -y python ncurses \
&& pkg reinstall -y python \
&& termux-setup-storage
