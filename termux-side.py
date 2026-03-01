import asyncio
import subprocess
import time
import os
import sys
import uuid

# ===== HARD STDIO SILENCE SETUP =====
DEVNULL = open(os.devnull, "w")

def silence_stdio():
    os.dup2(DEVNULL.fileno(), sys.stdout.fileno())
    os.dup2(DEVNULL.fileno(), sys.stderr.fileno())

# silence EVERYTHING by default
silence_stdio()

# ===== STATE =====
STATE_DIR = os.path.expanduser("~/roblox_state")
os.makedirs(STATE_DIR, exist_ok=True)

HWID_FILE = os.path.join(STATE_DIR, "hwid")
if not os.path.exists(HWID_FILE):
    with open(HWID_FILE, "w") as f:
        f.write(str(uuid.uuid4()))

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

SUBDIRS = [
    "files/gloop/external/Workspace/atlas",
    "files/gloop/external/Autoexecute",
]

TIMEOUT = 300
CHECK_INTERVAL = 15
SYNC_INTERVAL = 20
LAUNCH_DELAY = 12

# ===== GLOBAL STATE =====
startup_done = False
last_seen = {pkg: None for pkg in PACKAGES}

# ===== UTILS =====
def clear_screen():
    sys.stdout.write("\033[2J\033[H")
    sys.stdout.flush()

# ===== ROBLOX CONTROL =====
def hard_kill(pkg):
    subprocess.call(
        ["su", "-c", f"cmd activity force-stop {pkg}"],
        stdout=DEVNULL,
        stderr=DEVNULL,
    )

def launch(pkg):
    hard_kill(pkg)
    time.sleep(2)
    subprocess.call(
        [
            "am", "start",
            "-a", "android.intent.action.VIEW",
            "-d", f"roblox://placeId={PLACE_ID}",
            pkg
        ],
        stdout=DEVNULL,
        stderr=DEVNULL,
    )

async def restart(pkg):
    await asyncio.sleep(LAUNCH_DELAY)
    hard_kill(pkg)
    await asyncio.sleep(2)
    launch(pkg)

# ===== FILE SYNC =====
def sync_files():
    src_base = f"{BASE}/{PACKAGES[0]}"
    for sub in SUBDIRS:
        src = f"{src_base}/{sub}"
        if not os.path.isdir(src):
            continue
        for pkg in PACKAGES[1:]:
            dst = f"{BASE}/{pkg}/{sub}"
            os.makedirs(dst, exist_ok=True)
            subprocess.call(
                ["rsync", "-a", "--delete", f"{src}/", f"{dst}/"],
                stdout=DEVNULL,
                stderr=DEVNULL,
            )

async def file_sync_loop():
    while True:
        sync_files()
        await asyncio.sleep(SYNC_INTERVAL)

# ===== REJOINER =====
def init_rejoiners():
    for pkg in PACKAGES:
        path = f"{BASE}/{pkg}/{REJOINER_REL}"
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w"):
            pass  # blank init

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
            if ts is not None and now - ts > TIMEOUT:
                await restart(pkg)
        await asyncio.sleep(CHECK_INTERVAL)

# ===== DASHBOARD =====
def col(text, width):
    text = "" if text is None else str(text)
    return (text[: width - 1] + "…") if len(text) > width else text.ljust(width)

async def dashboard_loop():
    global startup_done

    # restore stdio ONLY for dashboard
    sys.stdout = sys.__stdout__
    sys.stderr = sys.__stderr__

    while not startup_done:
        await asyncio.sleep(1)

    while True:
        clear_screen()
        now = int(time.time())

        print("Roblox Controller Dashboard")
        print(f"Updated: {time.strftime('%H:%M:%S')}\n")

        header = (
            col("Package", 28) + "  " +
            col("Status", 8) + "  " +
            col("Last Seen", 10) + "  " +
            col("Age", 6)
        )

        print(header)
        print("-" * len(header))

        for pkg in PACKAGES:
            ts = last_seen[pkg]
            if ts is None:
                print(col(pkg, 28) + "  DEAD     never       —")
                continue

            age = now - ts
            status = "OK" if age < TIMEOUT else "STALE"
            last_seen_str = time.strftime("%H:%M:%S", time.localtime(ts))
            m, s = divmod(age, 60)
            age_str = f"{m}m{s}s" if m else f"{s}s"

            print(
                col(pkg, 28) + "  " +
                col(status, 8) + "  " +
                col(last_seen_str, 10) + "  " +
                col(age_str, 6)
            )

        await asyncio.sleep(5)

# ===== MAIN =====
async def main():
    global startup_done

    init_rejoiners()

    asyncio.create_task(file_sync_loop())

    for pkg in PACKAGES:
        await asyncio.sleep(LAUNCH_DELAY)
        launch(pkg)

    startup_done = True

    await asyncio.gather(
        poll_rejoiner_loop(),
        watchdog_loop(),
        dashboard_loop(),
    )

asyncio.run(main())
