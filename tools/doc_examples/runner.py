"""Runs an example program, in a process group of its own, and reports how it ended."""

from __future__ import annotations

import os
import signal
import subprocess
import tempfile
import time
from collections.abc import Callable, Sequence
from dataclasses import dataclass

# The time an example has to stop once asked, in seconds
STOP_TIMEOUT = 5


@dataclass
class Result:
    # Whether the program exited by itself, rather than being stopped
    exited: bool
    returncode: int | None
    stdout: str
    stderr: str

    def describe(self) -> str:
        status = (
            "exited with code %s" % self.returncode if self.exited else "was stopped"
        )
        return "The example %s.\n--- stdout\n%s\n--- stderr\n%s" % (
            status,
            _tail(self.stdout),
            _tail(self.stderr),
        )


def _tail(text: str, lines: int = 40) -> str:
    return "\n".join(text.splitlines()[-lines:])


class Process:
    """A running example. Its output goes to files, so a chatty program cannot block on a
    full pipe."""

    def __init__(self, argv: Sequence[str], env: dict[str, str], cwd: str):
        self._stdout = tempfile.TemporaryFile()
        self._stderr = tempfile.TemporaryFile()
        self._process = subprocess.Popen(
            argv,
            env=env,
            cwd=cwd,
            stdin=subprocess.DEVNULL,
            stdout=self._stdout,
            stderr=self._stderr,
            start_new_session=True,
        )

    def running(self) -> bool:
        return self._process.poll() is None

    def wait(self, timeout: float) -> bool:
        """Waits for the program to exit. Returns whether it did."""
        try:
            self._process.wait(timeout)
            return True
        except subprocess.TimeoutExpired:
            return False

    def stdout(self) -> str:
        """The output so far."""
        return os.pread(self._stdout.fileno(), 1 << 20, 0).decode("utf-8", "replace")

    def finish(self) -> Result:
        """Stops the program if it is still running, with the whole of its process group,
        as a launcher can start the program as a child."""
        exited = not self.running()
        self._signal(signal.SIGTERM)
        if not self.wait(STOP_TIMEOUT):
            self._signal(signal.SIGKILL)
            self._process.wait()
        return Result(
            exited,
            self._process.returncode,
            self._read(self._stdout),
            self._read(self._stderr),
        )

    def _signal(self, sig: signal.Signals) -> None:
        try:
            os.killpg(self._process.pid, sig)
        except ProcessLookupError:
            pass

    @staticmethod
    def _read(file) -> str:
        file.seek(0)
        text = file.read().decode("utf-8", errors="replace")
        file.close()
        return text


def wait_for(predicate: Callable[[], bool], process: Process, timeout: float) -> bool:
    """Waits until predicate holds, while the program runs. Returns whether it held."""
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline and process.running():
        if predicate():
            return True
        time.sleep(0.1)
    return False
