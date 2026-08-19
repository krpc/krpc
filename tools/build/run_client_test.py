"""Runs a client's comms tests against a TestServer instance.

  run_client_test.py CONFIG

CONFIG is the json file written by the client_test rule, naming the server and
test executables (runfiles-root-relative) and the server transport to use.

The server picks its ports at startup and reports them on stdout, so the harness
starts it, waits for it to say it is up, and passes the ports it printed to the
test executable in the environment. For the serial transport there are no ports:
socat pairs two pseudo-terminals and the server and test each get one end.

The server's output only reaches the test log when something goes wrong -- on a
failing test, or on the harness being killed, which is how the test runner
reports a timeout. Without it a server-side failure is invisible: the client just
blocks until the test times out.
"""

import argparse
import json
import os
import re
import signal
import subprocess
import sys
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

# The virtual serial link is not always usable the instant the server reports
# itself started, so let it settle before the client opens its end.
SERIAL_SETTLE_TIME = 1


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


def port(output, name):
    """Read a port the server reported, e.g. "rpc_port = 50000"."""
    match = re.search(r"\b%s = (\d+)" % name, output)
    if match is None:
        raise RuntimeError("the server did not report %s:\n%s" % (name, output))
    return match.group(1)


def start_socat():
    """Pair two pseudo-terminals, returning the process and both ends' paths.

    The links are made in a directory of their own so the two names cannot
    collide with anything the server or the test writes.
    """
    directory = os.path.abspath("serial-ports")
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


def run(config):
    # Tests run from the runfiles root of the main repository, so the paths in
    # the config resolve against the working directory. The executables are
    # launched by absolute path: relative ones are interpreted against the
    # search path rather than the working directory on some platforms.
    server = os.path.abspath(config["server"])
    test = os.path.abspath(config["test"])
    serial = config["server_type"] == "serialio"

    # The executables locate their own runfiles -- the .NET runtime and
    # assemblies, the python interpreter and its packages -- through RUNFILES_DIR
    # when they cannot find a runfiles tree of their own beside them. They share
    # this test's tree, which holds everything they need.
    environment = dict(os.environ)
    if "RUNFILES_DIR" not in environment and "TEST_SRCDIR" in environment:
        environment["RUNFILES_DIR"] = environment["TEST_SRCDIR"]

    socat = None
    server_process = None
    try:
        arguments = ["--type=" + config["server_type"]]
        test_environment = dict(environment)
        if serial:
            socat, server_port, client_port = start_socat()
            print(
                "Virtual serial ports established: %s <-> %s"
                % (server_port, client_port)
            )
            # Transfers run on a background thread whose failures show up only in
            # the server's log, so ask for the detail needed to diagnose them.
            arguments += ["--debug", "--port=" + server_port]
            test_environment["PORT"] = client_port

        server_process = start([server] + arguments, env=environment)
        reader = Reader(server_process, SERVER_READY)
        reader.wait("the server")

        if serial:
            time.sleep(SERIAL_SETTLE_TIME)
            print("Server started, port = %s" % server_port)
        else:
            rpc_port = port(reader.output, "rpc_port")
            stream_port = port(reader.output, "stream_port")
            test_environment["RPC_PORT"] = rpc_port
            test_environment["STREAM_PORT"] = stream_port
            print(
                "Server started, rpc port = %s, stream port = %s"
                % (rpc_port, stream_port)
            )

        # Dump the server's output if the harness is killed, which is how the
        # test runner reports a timeout on the platforms that can signal it.
        def on_terminate(_signum, _frame):
            report(reader.output)
            sys.exit(1)

        signal.signal(signal.SIGTERM, on_terminate)

        sys.stdout.flush()
        result = subprocess.run([test], env=test_environment, check=False).returncode
        if result != 0:
            report(reader.output)
        return result
    finally:
        for process in (server_process, socat):
            if process is not None:
                stop(process)


def report(output):
    sys.stdout.write("=== server output ===\n")
    sys.stdout.write(output)
    sys.stdout.flush()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("config")
    options = parser.parse_args()
    with open(options.config, "r", encoding="utf-8") as config_file:
        return run(json.load(config_file))


if __name__ == "__main__":
    sys.exit(main())
