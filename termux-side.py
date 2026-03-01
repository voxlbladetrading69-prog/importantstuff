import asyncio
import curses
import subprocess
import time
import os
import uuid
import logging

# ===== STATE DIR =====
STATE_DIR = os.path.expanduser("~/roblox_state")
os.makedirs(STATE_DIR, exist_ok=True)

# ===== LOGGING =====
LOG_FILE = os.path.join(STATE_DIR, "controller.log")
logging.basicConfig(
    filename=LOG_FILE,
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)

# ===== CONFIG =====
PACKAGES = [
    "com.roblox.clienu",
    "com.roblox.clienv",
    "com.roblox.clienw",
    "com.roblox.clienx",
    "com.roblox.clieny",
]

PLACE_ID = "1537690962"

BASE = "/storage/emulated/0/Android/data"
REJOINER_REL = "files/gloop/external/Workspace/REJOINER.txt"

CHECK_INTERVAL = 15
TIMEOUT = 300
LAUNCH_DELAY = 12
RESTART_COOLDOWN = 600  # 10 minutes

# ===== GLOBAL STATE =====
last_seen = {pkg: None for pkg in PACKAGES}
last_restart = {pkg: 0 for pkg in PACKAGES}

# ===== ROBLOX CONTROL =====
def hard_kill(pkg):
    subprocess.call(
        ["su", "-c", f"cmd activity force-stop {pkg} >/dev/null 2>&1"]
    )

def launch(pkg):
    hard_kill(pkg)
    time.sleep(2)
    subprocess.call(
        [
            "su", "-c",
            (
                "am start "
                "-a android.intent.action.VIEW "
                f"-d roblox://placeId={PLACE_ID} {pkg} "
                ">/dev/null 2>&1"
            )
        ]
    )

async def restart(pkg):
    logging.warning(f"Restarting {pkg}")
    await asyncio.sleep(LAUNCH_DELAY)
    launch(pkg)
    last_restart[pkg] = int(time.time())

# ===== REJOINER =====
def init_rejoiners():
    for pkg in PACKAGES:
        path = f"{BASE}/{pkg}/{REJOINER_REL}"
        os.makedirs(os.path.dirname(path), exist_ok=True)
        if not os.path.exists(path):
            with open(path, "w"):
                pass

async def poll_rejoiner_loop():
    while True:
        for pkg in PACKAGES:
            path = f"{BASE}/{pkg}/{REJOINER_REL}"
            try:
                with open(path) as f:
                    raw = f.read().strip()
                if not raw:
                    continue

                ts = int(raw)
                if last_seen[pkg] is None or ts > last_seen[pkg]:
                    last_seen[pkg] = ts
            except Exception:
                pass

        await asyncio.sleep(CHECK_INTERVAL)

# ===== WATCHDOG =====
async def watchdog_loop():
    while True:
        now = int(time.time())

        for pkg, ts in last_seen.items():
            if ts is None:
                continue

            age = now - ts
            cooldown = now - last_restart[pkg]

            if age > TIMEOUT and cooldown > RESTART_COOLDOWN:
                logging.warning(
                    f"{pkg} stale ({age}s), restarting"
                )
                await restart(pkg)

        await asyncio.sleep(CHECK_INTERVAL)

# ===== CURSES UI =====
async def ui_loop(stdscr):
    curses.curs_set(0)
    stdscr.nodelay(True)

    while True:
        stdscr.erase()
        h, w = stdscr.getmaxyx()
        now = int(time.time())

        stdscr.addstr(0, 0, "Roblox Controller Dashboard")
        stdscr.addstr(1, 0, f"Updated: {time.strftime('%H:%M:%S')}")

        stdscr.addstr(3, 0, f"{'Package':28}  {'Status':8}  {'Age':6}")

        row = 4
        for pkg in PACKAGES:
            ts = last_seen[pkg]

            if ts is None:
                status = "DEAD"
                age_str = "-"
            else:
                age = now - ts
                status = "OK" if age < TIMEOUT else "STALE"
                age_str = f"{age}s"

            line = f"{pkg:28}  {status:8}  {age_str:6}"
            stdscr.addstr(row, 0, line[: w - 1])
            row += 1

        stdscr.refresh()
        await asyncio.sleep(1)

# ===== MAIN =====
async def async_main(stdscr):
    init_rejoiners()

    for pkg in PACKAGES:
        logging.info(f"Launching {pkg}")
        await asyncio.sleep(LAUNCH_DELAY)
        launch(pkg)

    await asyncio.gather(
        poll_rejoiner_loop(),
        watchdog_loop(),
        ui_loop(stdscr),
    )

def main():
    curses.wrapper(lambda stdscr: asyncio.run(async_main(stdscr)))

if __name__ == "__main__":
    main()
