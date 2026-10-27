"""Starting a TestServer, or the socat pair a serial test talks over, and reading what it
reports as it comes up. Shared by the client test harness and the conformance driver."""

import os
import re
import subprocess
import sys
import tempfile
import threading
import time

# The line the server prints once it is listening, and the one socat prints once
# the pseudo-terminal pair is connected.
SERVER_READY = "Server started successfully"
SOCAT_READY = "starting data transfer loop"

# Generous enough that a loaded machine does not fail the test, short enough that
# a server that will never come up is reported rather than run into the test
# runner's own timeout.
READY_TIMEOUT = 120


class Reader:
    """Collects a process's output, flagging when a marker line appears.

    The process writes to a pipe, which fills and blocks the writer if nobody
    drains it, so the reading runs on a thread for the lifetime of the process
    rather than only while the harness is waiting for the marker.
    """

    def __init__(self, process, marker):
        self._process = process
        self._marker = marker
        self._lines = []
        self._lock = threading.Lock()
        self.found = threading.Event()
        self._thread = threading.Thread(target=self._read, daemon=True)
        self._thread.start()

    def _read(self):
        for line in self._process.stdout:
            with self._lock:
                self._lines.append(line)
            if self._marker in line:
                self.found.set()

    @property
    def output(self):
        with self._lock:
            return "".join(self._lines)

    def wait(self, what):
        """Block until the marker appears, or raise if it never does."""
        deadline = time.monotonic() + READY_TIMEOUT
        while not self.found.wait(0.1):
            if self._process.poll() is not None:
                raise RuntimeError(
                    "%s exited with %d before it was ready:\n%s"
                    % (what, self._process.returncode, self.output)
                )
            if time.monotonic() > deadline:
                raise RuntimeError(
                    "%s was not ready after %d seconds:\n%s"
                    % (what, READY_TIMEOUT, self.output)
                )


def socket_directory():
    """A directory to put a socket in, short enough for the path of one to fit in a socket
    address. The directory a test is given for its temporary files is nested far deeper than
    an address has room for, so the platform's own is used directly."""
    if sys.platform != "win32":
        return "/tmp"
    local = os.environ.get("LOCALAPPDATA")
    return os.path.join(local, "Temp") if local else tempfile.gettempdir()


def start(command, **kwargs):
    """Start a process with its stdout and stderr merged onto a readable pipe."""
    return subprocess.Popen(
        command,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        bufsize=1,
        **kwargs,
    )


def stop(process):
    """Shut a process down, without waiting on one that ignores the request."""
    if process.poll() is not None:
        return
    process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()


def setting(output, name):
    """Read a value the server reported, e.g. "tcpip_port = 50000"."""
    match = re.search(r"\b%s = (.+)" % name, output)
    if match is None:
        raise RuntimeError("the server did not report %s:\n%s" % (name, output))
    return match.group(1).strip()


def start_socat(directory="serial-ports"):
    """Pair two pseudo-terminals, returning the process and both ends' paths.

    The links are made in a directory of their own so the two names cannot
    collide with anything the server or the test writes.
    """
    directory = os.path.abspath(directory)
    os.makedirs(directory, exist_ok=True)
    process = start(
        [
            "socat",
            "-d",
            "-d",
            "PTY,raw,echo=0,link=server-port",
            "PTY,raw,echo=0,link=client-port",
        ],
        cwd=directory,
    )
    Reader(process, SOCAT_READY).wait("socat")
    return (
        process,
        os.path.join(directory, "server-port"),
        os.path.join(directory, "client-port"),
    )
