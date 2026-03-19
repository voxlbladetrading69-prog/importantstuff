import asyncio
import curses
import fcntl
import logging
import os
import signal
import subprocess
import time
from contextlib import suppress
from typing import Dict, List, Optional

# ===== STATE DIR =====
STATE_DIR = os.path.expanduser(os.environ.get("ROBLOX_STATE_DIR", "~/roblox_state"))
os.makedirs(STATE_DIR, exist_ok=True)

# ===== LOGGING =====
LOG_FILE = os.path.join(STATE_DIR, "controller.log")
logging.basicConfig(
    filename=LOG_FILE,
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)

# ===== CONFIG =====
PACKAGES: List[str] = [
    "com.roblox.clienu",
    "com.roblox.clienv",
    "com.roblox.clienw",
    "com.roblox.clienx",
    "com.roblox.clieny",
]

PLACE_ID = os.environ.get("ROBLOX_PLACE_ID", "1537690962")
BASE = "/storage/emulated/0/Android/data"
REJOINER_REL = "files/gloop/external/Workspace/REJOINER.txt"
RSYNC = "rsync"

CHECK_INTERVAL = int(os.environ.get("CHECK_INTERVAL", "15"))
TIMEOUT = int(os.environ.get("TIMEOUT", "300"))
LAUNCH_DELAY = int(os.environ.get("LAUNCH_DELAY", "12"))
RESTART_COOLDOWN = int(os.environ.get("RESTART_COOLDOWN", "300"))

# ===== FILE MIRROR CONFIG =====
SOURCE_PKG = PACKAGES[0]
AUTOEXEC_REL = "files/gloop/external/Autoexecute"
FILE_SYNC_INTERVAL = int(os.environ.get("FILE_SYNC_INTERVAL", "20"))

# ===== GLOBAL STATE =====
last_seen: Dict[str, Optional[int]] = {pkg: None for pkg in PACKAGES}
last_restart: Dict[str, int] = {pkg: 0 for pkg in PACKAGES}
shutdown_event = asyncio.Event()

LOCK_FILE = os.path.join(STATE_DIR, "controller.lock")
lock_fd: Optional[int] = None


def acquire_singleton_lock() -> None:
    """Prevent multiple controller instances from running."""
    global lock_fd
    lock_fd = os.open(LOCK_FILE, os.O_CREAT | os.O_RDWR, 0o644)

    try:
        fcntl.flock(lock_fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except BlockingIOError:
        raise RuntimeError("Another controller instance is already running")

    os.ftruncate(lock_fd, 0)
    os.write(lock_fd, str(os.getpid()).encode("utf-8"))


def release_singleton_lock() -> None:
    """Release singleton lock file."""
    global lock_fd

    if lock_fd is None:
        return

    with suppress(OSError):
        fcntl.flock(lock_fd, fcntl.LOCK_UN)

    with suppress(OSError):
        os.close(lock_fd)

    lock_fd = None


def run_su_command(cmd: str) -> int:
    """Run root command and log non-zero exits."""
    rc = subprocess.call(["su", "-c", cmd])
    if rc != 0:
        logging.error("Root command failed (%s): %s", rc, cmd)
    return rc


# ===== ROBLOX CONTROL =====
def hard_kill(pkg: str) -> None:
    run_su_command(f"cmd activity force-stop {pkg} >/dev/null 2>&1")


def launch(pkg: str) -> bool:
    hard_kill(pkg)
    time.sleep(2)

    rc = run_su_command(
        f"am start -n {pkg}/com.roblox.client.startup.ActivitySplash; "
        f"sleep 5; "
        f"am start -a android.intent.action.VIEW "
        f"-d 'roblox://placeId={PLACE_ID}' "
        # f"-d 'https://www.roblox.com/share?code=e54e53d5363d9f4e83bccc971590fa12&type=Server' "
        f"-p {pkg} "
        ">/dev/null 2>&1"
    )

    if rc == 0:
        now = int(time.time())
        path = f"{BASE}/{pkg}/{REJOINER_REL}"

        try:
            os.makedirs(os.path.dirname(path), exist_ok=True)

            with open(path, "w", encoding="utf-8") as f:
                f.write(str(now))

            last_seen[pkg] = now
            logging.info("Initial heartbeat written for %s", pkg)

        except OSError as exc:
            logging.warning("Failed to write initial heartbeat for %s: %s", pkg, exc)

        return True

    return False


async def restart(pkg: str) -> None:
    logging.warning("Restarting %s", pkg)

    await asyncio.sleep(LAUNCH_DELAY)

    if launch(pkg):
        last_restart[pkg] = int(time.time())
    else:
        logging.error("Failed to relaunch %s", pkg)


SUBDIRS = [
    "files/gloop/external/Workspace/atlas",
    "files/gloop/external/Autoexecute",
]


def aggressive_initial_sync():
    for subdir in SUBDIRS:
        source = f"{BASE}/{SOURCE_PKG}/{subdir}"

        if not os.path.isdir(source):
            logging.warning("Source missing, skipping: %s", source)
            continue

        for pkg in PACKAGES:
            if pkg == SOURCE_PKG:
                continue

            target = f"{BASE}/{pkg}/{subdir}"

            try:
                subprocess.run(
                    [RSYNC, "-a", "--delete", f"{source}/", f"{target}/"],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                    check=False,
                )

                logging.info("Synced %s -> %s", source, target)

            except Exception as e:
                logging.error("Initial sync failed for %s -> %s: %s", source, target, e)


def incremental_sync():
    for subdir in SUBDIRS:
        source = f"{BASE}/{SOURCE_PKG}/{subdir}"

        if not os.path.isdir(source):
            continue

        for pkg in PACKAGES:
            if pkg == SOURCE_PKG:
                continue

            target = f"{BASE}/{pkg}/{subdir}"
            os.makedirs(target, exist_ok=True)

            try:
                subprocess.run(
                    [RSYNC, "-a", "--delete", f"{source}/", f"{target}/"],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                    check=False,
                )

            except Exception as e:
                logging.error("rsync failed for %s -> %s: %s", source, target, e)


# ===== REJOINER =====
def init_rejoiners() -> None:
    for pkg in PACKAGES:
        path = f"{BASE}/{pkg}/{REJOINER_REL}"

        os.makedirs(os.path.dirname(path), exist_ok=True)

        if not os.path.exists(path):
            with open(path, "w", encoding="utf-8"):
                pass


async def file_sync_loop():
    while not shutdown_event.is_set():
        incremental_sync()
        await asyncio.sleep(FILE_SYNC_INTERVAL)


async def poll_rejoiner_loop() -> None:
    while not shutdown_event.is_set():

        for pkg in PACKAGES:

            path = f"{BASE}/{pkg}/{REJOINER_REL}"

            raw = ""

            try:
                with open(path, encoding="utf-8") as f:
                    raw = f.read().strip()

                if not raw:
                    continue

                ts = int(raw)

                if last_seen[pkg] is None or ts > int(last_seen[pkg]):
                    last_seen[pkg] = ts

            except FileNotFoundError:
                logging.warning("Rejoiner file missing for %s", pkg)

            except ValueError:
                logging.warning("Invalid heartbeat content for %s: %r", pkg, raw)

            except OSError as exc:
                logging.warning("Cannot read heartbeat for %s: %s", pkg, exc)

        await asyncio.sleep(CHECK_INTERVAL)


# ===== WATCHDOG =====
async def watchdog_loop() -> None:

    while not shutdown_event.is_set():

        now = int(time.time())

        for pkg, ts in last_seen.items():

            if ts is None:
                continue

            age = now - ts
            cooldown = now - last_restart[pkg]

            if age > TIMEOUT and cooldown > RESTART_COOLDOWN:
                logging.warning("%s stale (%ss), restarting", pkg, age)
                await restart(pkg)

        await asyncio.sleep(CHECK_INTERVAL)


# ===== CURSES UI =====
async def ui_loop(stdscr) -> None:

    with suppress(curses.error):
        curses.curs_set(0)

    stdscr.nodelay(True)

    while not shutdown_event.is_set():

        stdscr.erase()
        _, width = stdscr.getmaxyx()
        now = int(time.time())

        with suppress(curses.error):
            stdscr.addstr(0, 0, "Roblox Controller Dashboard v2 - Commercial")
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

            with suppress(curses.error):
                stdscr.addstr(row, 0, line[: max(width - 1, 1)])

            row += 1

        with suppress(curses.error):
            stdscr.addstr(row + 1, 0, "Press q to quit")
            stdscr.refresh()

        key = stdscr.getch()

        if key in (ord("q"), ord("Q")):
            shutdown_event.set()
            break

        await asyncio.sleep(1)


def request_shutdown(*_args) -> None:
    logging.info("Shutdown requested")
    shutdown_event.set()


# ===== MAIN =====
async def async_main(stdscr) -> None:

    init_rejoiners()

    run_su_command(
        f"curl -L -o {BASE}/{SOURCE_PKG}/files/gloop/external/Workspace/atlas/ATLAS.txt "
        "https://raw.githubusercontent.com/voxlbladetrading69-prog/importantstuff/refs/heads/main/ATLAS.txt "
        ">/dev/null 2>&1"
    )

    aggressive_initial_sync()

    ui_task = asyncio.create_task(ui_loop(stdscr))
    sync_task = asyncio.create_task(file_sync_loop())

    other_tasks: List[asyncio.Task] = []

    try:

        await asyncio.sleep(2)

        for pkg in PACKAGES:
            logging.info("Launching %s", pkg)
            await asyncio.sleep(LAUNCH_DELAY)
            launch(pkg)

        other_tasks = [
            asyncio.create_task(poll_rejoiner_loop()),
            asyncio.create_task(watchdog_loop()),
        ]

        await shutdown_event.wait()

    finally:

        shutdown_event.set()

        sync_task.cancel()
        ui_task.cancel()

        for task in other_tasks:
            task.cancel()

        with suppress(asyncio.CancelledError):
            await sync_task

        with suppress(asyncio.CancelledError):
            await ui_task

        for task in other_tasks:
            with suppress(asyncio.CancelledError):
                await task


def main() -> None:

    try:
        acquire_singleton_lock()

    except RuntimeError as exc:
        logging.error(str(exc))
        print(str(exc))
        return

    signal.signal(signal.SIGINT, request_shutdown)
    signal.signal(signal.SIGTERM, request_shutdown)

    with suppress(AttributeError):
        signal.signal(signal.SIGHUP, request_shutdown)

    with suppress(AttributeError):
        signal.signal(signal.SIGQUIT, request_shutdown)

    try:
        curses.wrapper(lambda stdscr: asyncio.run(async_main(stdscr)))

    finally:
        release_singleton_lock()


if __name__ == "__main__":
    main()
