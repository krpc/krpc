"""Launching a game, waiting for its server, and stopping it.

``ensure`` gives the tests a running game with the mods they require. A game this run
launched is restarted when a test class requires other mods, and stopped at exit. A game
started elsewhere is never stopped or reinstalled.
"""

import atexit
import logging
import os
import subprocess
import sys
import time

import krpc

# Progress messages (installing, launching, the mod set in use) go to this logger at INFO.
# pytest shows them with log_cli; outside pytest, INFO is dropped by default.
log = logging.getLogger("krpctest")


def connect(game, use_cached=True):
    """A connection to the running game's server, cached on the game unless use_cached is
    False."""
    if game.connection is not None and use_cached:
        return game.connection
    connection = krpc.connect(name=game.client_name, address=game.address)
    if use_cached:
        game.connection = connection
    return connection


def _try_connect(game):
    """A connection to the running game's server, or None when none is reachable."""
    try:
        return connect(game)
    except Exception:
        return None


def loaded_mods(game, conn, required=()):
    """The names of the game's mods the running game has loaded. A required mod whose
    presence cannot be told counts as loaded."""
    loaded = set()
    for name, mod in game.mods.items():
        present = mod.present(game, conn)
        if present or (present is None and name in required):
            loaded.add(name)
    return loaded


def _validate(game, required):
    unknown = set(required) - set(game.mods)
    if unknown:
        raise ValueError(
            "Unknown mod(s) in `mods`: %s (known: %s)"
            % (", ".join(sorted(unknown)), ", ".join(sorted(game.mods)))
        )


def launch(game, mods, args=None):
    """Install the mods, launch the game and wait for its server. The game is owned by this
    run, so it may be restarted or stopped."""
    log.info("installing kRPC (mods: %s)", ", ".join(sorted(mods)) or "none")
    game.install(sorted(mods))
    if args is None:
        args = game.test_arguments()
    log.info("launching %s", game.title())
    game.process = subprocess.Popen(
        game.launch_command(args),
        cwd=game.game_dir(),
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    if not game.stops_at_exit:
        atexit.register(stop, game)
        game.stops_at_exit = True
    return wait_for_server(game)


def terminate(game, process):
    """Stop a game process, killing it when it does not exit, and every copy of the game by
    name when the handle does not reach it."""
    if process.poll() is not None:
        return
    try:
        process.terminate()
        process.wait(timeout=30)
    except Exception:
        try:
            process.kill()
            process.wait(timeout=10)
        except Exception:
            pass
    if process.poll() is None:
        game.kill_all()


def stop(game):
    """Stop the game this run launched. A game started elsewhere is left running."""
    if game.process is None:
        return
    process = game.process
    game.process = None
    game.connection = None
    log.info("stopping %s", game.title())
    terminate(game, process)


def wait_for_server(game, timeout=300, interval=3):
    """Poll until the server answers, and return the connection."""
    log.info("waiting for kRPC server...")
    deadline = time.time() + timeout
    while True:
        connection = _try_connect(game)
        if connection is not None:
            # Wait for the game to settle after loading
            time.sleep(3)
            log.info("kRPC server ready")
            return connection
        if game.process is not None and game.process.poll() is not None:
            raise RuntimeError(
                "%s exited before the kRPC server became available" % game.title()
            )
        if time.time() > deadline:
            raise RuntimeError(
                "Timed out after %gs waiting for the kRPC server" % timeout
            )
        time.sleep(interval)


def _unsatisfiable_message(game, required, installed=None):
    detail = ""
    if installed is not None:
        detail = " (the game reports these mods available: %s)" % (
            ", ".join(sorted(installed)) or "none"
        )
    return (
        "Installed and launched %s with mods %s, but the game does not report them "
        "available%s. The mod may have failed to load, e.g. an incompatible version or a "
        "missing dependency. Not retrying." % (game.title(), sorted(required), detail)
    )


def _mismatch_message(game, required, installed):
    parts = []
    missing = sorted(required - installed)
    extra = sorted(installed - required)
    if missing:
        parts.append("missing required mod(s): " + ", ".join(missing))
    if extra:
        parts.append("unexpected mod(s) installed: " + ", ".join(extra))
    return (
        "The running %s has the wrong mods (%s). It was not started by the tests, so it "
        "will not be modified. Restart it with the correct mods, e.g. `%s`, or run the "
        "suite without --no-launch and let it manage the game."
        % (game.title(), "; ".join(parts), game.run_command(required))
    )


def ensure(game, mods=()):
    """Ensure a server is running with exactly the required mods.

    - Server up with the required mods: return.
    - Server up with other mods: restart the game when this run launched it, and raise
      otherwise.
    - No server: launch the game with the required mods, unless auto-launch is off.
    """
    required = set(mods)
    _validate(game, required)
    if frozenset(required) in game.unsatisfiable:
        raise RuntimeError(_unsatisfiable_message(game, required))
    conn = _try_connect(game)
    if conn is not None:
        installed = loaded_mods(game, conn, required)
        if installed == required:
            return
        if game.process is None:
            raise RuntimeError(_mismatch_message(game, required, installed))
        log.info(
            "required mods changed (%s -> %s), restarting %s",
            ", ".join(sorted(installed)) or "none",
            ", ".join(sorted(required)) or "none",
            game.title(),
        )
        stop(game)
    elif not game.auto_launch:
        raise RuntimeError(
            "No kRPC server is reachable and auto-launch is disabled (--no-launch or "
            "KRPC_TEST_AUTO_LAUNCH=0). Start "
            "%s first, e.g. `%s`, and wait for it to load."
            % (game.title(), game.run_command(required))
        )
    # Launch, and check the required mods loaded before any test runs against the game
    conn = launch(game, required)
    installed = loaded_mods(game, conn, required)
    if installed != required:
        game.unsatisfiable.add(frozenset(required))
        stop(game)
        raise RuntimeError(_unsatisfiable_message(game, required, installed))


def file_stat(path):
    """The stat of the file at path, or None if it does not exist."""
    try:
        return os.stat(path)
    except FileNotFoundError:
        return None


def _open(path):
    """The file at path opened for binary reading, or None if it does not exist."""
    try:
        return open(path, "rb")
    except FileNotFoundError:
        return None


def _identity(stat):
    return (stat.st_ino, stat.st_size, stat.st_mtime_ns)


def follow_log(path, keep, process, previous):
    """Print the lines appended to the log at path for which keep(line) is true, a small
    stand-in for `tail -F path | grep`. previous is the stat of the log before the game was
    launched, or None. Returns when the game exits."""

    def emit(data):
        # Windows builds end lines with CRLF
        line = data.rstrip(b"\r").decode("utf-8", errors="replace")
        if keep(line):
            sys.stdout.write(line + "\n")
            sys.stdout.flush()

    # Wait for the game to start the log of this run
    while True:
        stat = file_stat(path)
        if stat is not None and (
            previous is None or _identity(stat) != _identity(previous)
        ):
            break
        if process.poll() is not None:
            return
        time.sleep(0.5)

    log_file = _open(path)
    # An incomplete last line, held back until its newline is written
    pending = b""
    exited = False
    try:
        while True:
            data = log_file.read() if log_file is not None else b""
            if data:
                *lines, pending = (pending + data).split(b"\n")
                for line in lines:
                    emit(line)
                continue
            if exited:
                if pending:
                    emit(pending)
                return
            if process.poll() is not None:
                # Read once more, for lines written just before the game exited
                exited = True
                continue
            # Reopen the log if the game truncated or replaced it
            stat = file_stat(path)
            if stat is not None and (
                log_file is None
                or stat.st_ino != os.fstat(log_file.fileno()).st_ino
                or stat.st_size < log_file.tell()
            ):
                if log_file is not None:
                    log_file.close()
                log_file = _open(path)
                pending = b""
                continue
            time.sleep(0.1)
    finally:
        if log_file is not None:
            log_file.close()
