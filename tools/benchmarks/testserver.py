"""Starting a TestServer to benchmark against, and describing the one that was started.

TestServer is the kRPC server without the game: the same core, the same protocol, the same
dispatch, running a 60 Hz update loop of its own. It starts in a second, which is what makes
it the thing to measure a change to the server against before spending a KSP launch on it.

A server carries calls over one transport or another, and which one is part of what a client
costs rather than a detail of it, so a client suite measures every transport this machine has.
An ``Endpoint`` is one server's answer to where it is listening, and holds everything that
differs between them, so that starting a server, connecting to it, and telling a benchmark
program in another language where to find it are each written once.

A run starts its own server, so the numbers are taken against a process nothing else is
talking to. Set ``KRPC_TEST_TCPIP_PORT`` or ``KRPC_TEST_LOCALSOCKET_PATH`` to measure
against a server that is already running instead; a run against one of those measures the
transport that server speaks and no other.
"""

import contextlib
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

import krpc

# The lines TestServer writes to standard output as it comes up, behind the log line's
# timestamp and severity. Where it is listening comes first, then the ready line, so a reader
# that has seen the ready line has already seen the rest.
ADDRESSES = {
    "tcpip_port": re.compile(r"tcpip_port = (\d+)"),
    "localsocket_path": re.compile(r"localsocket_path = (\S+)"),
}
READY = "Server started successfully"

# The transports a client can reach a server over: protocol buffers over TCP/IP, and the same
# over a unix domain socket. How each reads in a report, and which of TestServer's protocols
# serves it.
TCP = "tcp"
LOCAL_SOCKET = "localsocket"
TRANSPORTS = (TCP, LOCAL_SOCKET)
LABELS = {TCP: "TCP/IP", LOCAL_SOCKET: "a local socket"}
SERVER_TYPES = {TCP: "protobuf", LOCAL_SOCKET: "localsocket"}

# Every variable a benchmark program reads to find the server. A program looks for the socket
# path first and falls back to the port, so setting only the one the transport being measured
# uses also picks that transport inside the program. The other has to be cleared, not
# inherited from whatever started the run.
VARIABLES = ("KRPC_TEST_TCPIP_PORT", "KRPC_TEST_LOCALSOCKET_PATH")

# The time to wait for those lines before giving up and showing what the server printed.
STARTUP_TIMEOUT_SECONDS = 60

# The time to exercise a freshly started server before anything is measured against it. A
# server that has just started spends its first calls having the paths they run through
# compiled, and whichever case is measured first otherwise pays for that and reports itself as
# a case that was still getting faster.
WARMUP_SECONDS = 1.0


class Endpoint:
    """Where a TestServer is listening, and how a client reaches it.

    One per transport: ``address`` is a port for TCP/IP and a socket path for a local socket, and
    everything that reads it goes through this rather than asking which transport it is
    holding.
    """

    def __init__(self, transport, address):
        self.transport = transport
        self.address = address

    def open(self, name):
        """Open a python client on this endpoint."""
        if self.transport == LOCAL_SOCKET:
            return krpc.connect_local(name=name, path=self.address)
        return krpc.connect(name=name, address="localhost", port=self.address)

    def variables(self):
        """What to put in a benchmark program's environment to send it here."""
        if self.transport == LOCAL_SOCKET:
            return {"KRPC_TEST_LOCALSOCKET_PATH": self.address}
        return {"KRPC_TEST_TCPIP_PORT": str(self.address)}


def transports():
    """The transports a run measures against, in the order they should be reported.

    Both of them, unless the environment names a server that is already running: that server
    speaks one protocol, and which one is said by whether its ports or its socket paths were
    given.
    """
    external = from_environment()
    if external is not None:
        return (external.transport,)
    return TRANSPORTS


def from_environment():
    """The server the environment names, or ``None`` if it names none."""
    if "KRPC_TEST_LOCALSOCKET_PATH" in os.environ:
        return Endpoint(LOCAL_SOCKET, os.environ["KRPC_TEST_LOCALSOCKET_PATH"])
    if "KRPC_TEST_TCPIP_PORT" in os.environ:
        return Endpoint(TCP, int(os.environ["KRPC_TEST_TCPIP_PORT"]))
    return None


@contextlib.contextmanager
def connection(name, executable=None, frame_pacing=True, transport=TCP):
    """Yield a client connected to a TestServer and the frame pacing it is running under,
    starting one unless the environment names a server that is already running.

    ``frame_pacing`` is passed on to a server that is started, and comes back as what was
    arranged. A server that is already running is taken as it is, and the pacing comes back as
    ``None``: whoever started it chose, and nothing here can ask it which way that went.
    """
    external = from_environment()
    if external is not None:
        with connect(name, external) as conn:
            yield conn, None
        return
    if executable is None:
        raise ValueError(
            "no TestServer to run, and nothing in the environment names one to connect to"
        )
    with running(
        executable, frame_pacing=frame_pacing, transport=transport
    ) as endpoint:
        with connect(name, endpoint) as conn:
            yield conn, frame_pacing


@contextlib.contextmanager
def connect(name, endpoint):
    conn = endpoint.open(name)
    try:
        yield conn
    finally:
        conn.close()


@contextlib.contextmanager
def running(executable, frame_pacing=True, transport=TCP):
    """Start TestServer for one transport and yield the endpoint it is listening on.

    Unpaced, the server runs its update loop as fast as it will go instead of 60 times a
    second, which is what a round trip has to be measured against to be the client's cost
    rather than the rate of the loop it landed in. ``run_client.py`` has the long version.
    """
    with _log() as (sink, log):
        with _sockets(transport) as directory:
            command = [
                os.path.abspath(executable),
                "--type=%s" % SERVER_TYPES[transport],
            ]
            if directory is not None:
                command.append(
                    "--localsocket-path=%s" % os.path.join(directory, "krpc.sock")
                )
            if not frame_pacing:
                command.append("--no-frame-pacing")
            process = subprocess.Popen(
                command,
                stdout=sink,
                stderr=subprocess.STDOUT,
                env=_environment(),
            )
            try:
                yield _wait_for_endpoint(process, log, transport)
            finally:
                process.terminate()
                process.wait()


def socket_directory():
    """A directory to put a socket in, short enough for the path of one to fit in a socket
    address. The directory a run is given for its temporary files is nested far deeper than an
    address has room for, so the platform's own is used directly."""
    if sys.platform != "win32":
        return "/tmp"
    local = os.environ.get("LOCALAPPDATA")
    return os.path.join(local, "Temp") if local else tempfile.gettempdir()


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
            with open(path, encoding="utf-8") as reader:
                yield sink, reader
    finally:
        shutil.rmtree(directory, ignore_errors=True)


@contextlib.contextmanager
def _sockets(transport):
    """A directory for a local socket server's sockets, or ``None`` for a transport with none.

    A socket address holds far less than a path may be long, and a runfiles tree is nested
    deeply enough to overrun it, so the sockets go somewhere short of their own and are removed
    with the server that was listening on them.
    """
    if transport != LOCAL_SOCKET:
        yield None
        return
    directory = tempfile.mkdtemp(prefix="krpc-benchmark-", dir=socket_directory())
    try:
        yield directory
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


def _wait_for_endpoint(process, log, transport):
    name = "localsocket_path" if transport == LOCAL_SOCKET else "tcpip_port"
    deadline = time.time() + STARTUP_TIMEOUT_SECONDS
    while time.time() < deadline:
        output = log.read()
        log.seek(0)
        if READY in output:
            found = ADDRESSES[name].search(output)
            if found is not None:
                address = found.group(1)
                return Endpoint(
                    transport, address if transport == LOCAL_SOCKET else int(address)
                )
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
        "one rpc per update": str(status.settings.one_rpc_per_update),
        "adaptive rate control": str(status.settings.adaptive_rate_control),
        "max time per update": "%d us" % status.settings.max_time_per_update,
        "blocking recv": str(status.settings.blocking_recv),
        "recv timeout": "%d us" % status.settings.recv_timeout,
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
            "adaptive rate control %s" % status.settings.adaptive_rate_control,
            "max time per update %d us" % status.settings.max_time_per_update,
            "recv timeout %d us" % status.settings.recv_timeout,
        ]
    )
