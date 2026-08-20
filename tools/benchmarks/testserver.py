"""Starting a TestServer to benchmark against, and describing the one that was started.

TestServer is the kRPC server without the game: the same core, the same protocol, the same
dispatch, running a 60 Hz update loop of its own. It starts in a second, which is what makes
it the thing to measure a change to the server against before spending a KSP launch on it.

A run starts its own server, so the numbers are taken against a process nothing else is
talking to. Set ``RPC_PORT`` and ``STREAM_PORT`` to measure against a server that is already
running instead.
"""

import contextlib
import os
import re
import shutil
import subprocess
import tempfile
import time

import krpc

# What TestServer writes to standard output as it comes up, behind the log line's timestamp
# and severity. The ports come first, then the ready line, so a reader that has seen the ready
# line has already seen them.
RPC_PORT = re.compile(r"rpc_port = (\d+)")
STREAM_PORT = re.compile(r"stream_port = (\d+)")
READY = "Server started successfully"

# How long to wait for those lines before giving up and showing what the server did say.
STARTUP_TIMEOUT_SECONDS = 60

# How long to exercise a freshly started server before anything is measured against it. A
# server that has just started spends its first calls having the paths they run through
# compiled, and whichever case is measured first otherwise pays for that and reports itself as
# a case that was still getting faster.
WARMUP_SECONDS = 1.0


@contextlib.contextmanager
def connection(name, executable=None, frame_pacing=True):
    """Yield a client connected to a TestServer and the frame pacing it is running under,
    starting one unless the environment names a server that is already running.

    ``frame_pacing`` is passed on to a server that is started, and comes back as what was
    arranged. A server that is already running is taken as it is, and the pacing comes back as
    ``None``: whoever started it chose, and nothing here can ask it which way that went.
    """
    if "RPC_PORT" in os.environ:
        with connect(
            name,
            int(os.environ["RPC_PORT"]),
            int(os.environ.get("STREAM_PORT", "50001")),
        ) as conn:
            yield conn, None
        return
    if executable is None:
        raise ValueError(
            "no TestServer to run, and RPC_PORT does not name one to connect to"
        )
    with running(executable, frame_pacing=frame_pacing) as (rpc_port, stream_port):
        with connect(name, rpc_port, stream_port) as conn:
            yield conn, frame_pacing


@contextlib.contextmanager
def connect(name, rpc_port, stream_port):
    conn = krpc.connect(
        name=name, address="localhost", rpc_port=rpc_port, stream_port=stream_port
    )
    try:
        yield conn
    finally:
        conn.close()


@contextlib.contextmanager
def running(executable, frame_pacing=True):
    """Start TestServer on ephemeral ports and yield the ports it chose.

    Unpaced, the server runs its update loop as fast as it will go instead of 60 times a
    second, which is what a round trip has to be measured against to be the client's cost
    rather than the rate of the loop it landed in. ``run_client.py`` has the long version.
    """
    with _log() as (sink, log):
        command = [os.path.abspath(executable)]
        if not frame_pacing:
            command.append("--no-frame-pacing")
        process = subprocess.Popen(  # pylint: disable=consider-using-with
            command,
            stdout=sink,
            stderr=subprocess.STDOUT,
            env=_environment(),
        )
        try:
            yield _wait_for_ports(process, log)
        finally:
            process.terminate()
            process.wait()


@contextlib.contextmanager
def _log():
    """A file for a server's output, open for writing and for reading.

    The output goes to a file rather than a pipe, so that nothing has to keep reading it for
    the server to stay unblocked once the run is under way. It is shown if the server fails to
    start, where it is the only account of why.

    Writer and reader are separate handles on a named file, so what is temporary is the
    directory holding it rather than the file itself: Windows allows no second handle on the
    one a temporary file keeps open.
    """
    directory = tempfile.mkdtemp(prefix="krpc-benchmark-log-")
    try:
        path = os.path.join(directory, "testserver.log")
        with open(path, "w", encoding="utf-8") as sink:
            with open(path, "r", encoding="utf-8") as reader:
                yield sink, reader
    finally:
        shutil.rmtree(directory, ignore_errors=True)


def _environment():
    """The environment the server's launcher needs to find the .NET runtime.

    A rules_dotnet binary locates its runfiles - the runtime and the assemblies - through the
    bash runfiles library, which reads RUNFILES_DIR, and the dotnet host resolves the
    runfiles-root-relative assembly paths in its deps.json against the working directory. A
    py_binary under `bazel run` starts in the runfiles tree, so both follow from where this
    process already is.
    """
    env = dict(os.environ)
    env.setdefault("RUNFILES_DIR", os.path.dirname(os.getcwd()))
    return env


def _wait_for_ports(process, log):
    deadline = time.time() + STARTUP_TIMEOUT_SECONDS
    while time.time() < deadline:
        output = log.read()
        log.seek(0)
        if READY in output:
            rpc_port = RPC_PORT.search(output)
            stream_port = STREAM_PORT.search(output)
            if rpc_port and stream_port:
                return int(rpc_port.group(1)), int(stream_port.group(1))
        if process.poll() is not None:
            raise RuntimeError(
                "TestServer exited with status %d before it was ready:\n%s"
                % (process.returncode, output)
            )
        time.sleep(0.05)
    raise RuntimeError(
        "TestServer did not start within %d seconds:\n%s"
        % (STARTUP_TIMEOUT_SECONDS, log.read())
    )


def warm_up(conn, seconds=WARMUP_SECONDS):
    """Make calls whose timings are thrown away, to get the server's cold start out of the way.

    Each case settles itself before it is measured, which covers the client warming up and the
    load the case itself puts on the server. What it cannot cover is the first case of a run
    arriving at a server that has never executed a call: that cost belongs to the run rather
    than to the case that happened to go first.
    """
    deadline = time.time() + seconds
    while time.time() < deadline:
        conn.test_service.float_to_string(3.14159)


def environment(conn):
    """The server settings that decide what a throughput or latency number can mean.

    How many calls an update handles, and whether the server waits for one, are the difference
    between a round trip measured in microseconds and one measured in frames. Two runs taken
    under different settings are not comparable, so every report carries them.
    """
    status = conn.krpc.get_status()
    return {
        "server": status.version,
        "one rpc per update": str(status.one_rpc_per_update),
        "adaptive rate control": str(status.adaptive_rate_control),
        "max time per update": "%d us" % status.max_time_per_update,
        "blocking recv": str(status.blocking_recv),
        "recv timeout": "%d us" % status.recv_timeout,
    }


def settings(conn, frame_pacing):
    """The settings of one server, as a single line, for a run that used more than one.

    Frame pacing is not a server setting and cannot be asked for over the protocol: the game
    paces its updates itself and kRPC cannot opt out there, so it is TestServer's own switch
    and only whoever started it knows which way it went. ``None`` says nobody here did.
    """
    status = conn.krpc.get_status()
    pacing = {True: "60 updates/s", False: "off", None: "not known"}[frame_pacing]
    return ", ".join(
        [
            "frame pacing %s" % pacing,
            "adaptive rate control %s" % status.adaptive_rate_control,
            "max time per update %d us" % status.max_time_per_update,
            "recv timeout %d us" % status.recv_timeout,
        ]
    )
