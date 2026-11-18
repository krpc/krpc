"""Update loop benchmarks: `bazel run //tools/benchmarks:update-loop`.

What a client gets from the server's update loop, and what the loop costs the game. TestServer
runs a model of a game's frame loop: fixed steps at 50 a second, simulated work in each step and
in each frame, and a frame limiter. Clients of different shapes call it, and each point reports
the calls a second the clients got, the frame rate, and the time the server spent in its updates
per frame.

Each client runs in a process of its own, so that a client busy-waiting between calls holds no
lock another client needs. Calls are counted where they are made, so a point reports the calls
the clients completed.

    bazel run //tools/benchmarks:update-loop -- --json before.json
    bazel run //tools/benchmarks:update-loop -- --game light --shape synchronous

Each figure is the median over a few windows of the point, apart from the longest frame, which
is the longest of any window.
"""

import contextlib
import multiprocessing
import os
import random
import sys
import time

import krpc
from krpc.schema.KRPC_pb2 import (
    Argument,
    ConnectionRequest,
    Message,
    ProcedureCall,
    Request,
)
from krpc.transport import TCPIPTransport
from tools.benchmarks import runner, testserver
from tools.benchmarks.report import Result

SUITE = "update loop"

# The port a server listens on unless told otherwise, as the in-game benchmarks find it
DEFAULT_PORT = 50000

# The ways of waiting to time, how many times each, and the figures each reports
WAIT_ITERATIONS = 200
WAIT_FIGURES = ("min_us", "median_us", "p99_us", "max_us")

# The games TestServer models, by name: fixed steps a second, simulated work in each step and in
# each frame in microseconds, and the frame limit. The light game leaves most of a 60 frames/s
# frame free. The heavy one leaves little of it, as a large craft does.
GAMES = {
    "light": {
        "fixed-rate": 50,
        "tick-work-us": 1000,
        "frame-work-us": 2000,
        "frame-limit": 60,
    },
    "heavy": {
        "fixed-rate": 50,
        "tick-work-us": 8000,
        "frame-work-us": 6000,
        "frame-limit": 60,
    },
}

# The game's fixed step, in seconds, for clients that call once per step
TICK = 0.02

# The client's own work between calls, in milliseconds, for the synchronous sweep
GAPS = (0, 0.5, 1, 2, 5, 10, 20, 40)

# The same for clients that call in bursts, by the calls in a burst
BURSTS = ((2, (0.5, 2)), (3, (0.5, 2)), (4, (0, 0.5, 1, 2, 5)))

# A client that calls once per tick, and calls again after this gap in milliseconds on one tick
# in this many
RARE_GAP = 0.5
RARE_EVERY = 16

# The range of gaps, in milliseconds, of a synchronous client whose gap varies from call to call
VARYING_GAPS = (0, 4)

# Requests sent ahead of their responses, for the pipelined client
DEPTHS = (1, 4, 16)

# The size of the value the stream sends on every tick, for the client that stops reading. Large
# enough to fill the socket buffers within a few seconds.
PAYLOAD_SIZE = 256 * 1024

# The time a point runs before it is measured, the number of windows it is measured over, and the
# length of each
WARMUP_SECONDS = 1.0
WINDOWS = 5
WINDOW_SECONDS = 0.5

# How long the client that stops reading goes without reading
STALL_SECONDS = 5.0

# The columns of the table, in order: the case suffix each is recorded under, its unit, the
# rate its reciprocal is, and how its samples combine into the figure shown
METRICS = (
    ("per call", "ms", "calls/s", "median"),
    ("frame", "ms", "frames/s", "median"),
    ("server per frame", "ms", "", "median"),
    ("waiting per frame", "ms", "", "median"),
    ("longest frame", "ms", "", "max"),
    ("budget per frame", "us", "", "median"),
    ("server cpu", "cores", "", "median"),
)


def main():
    assert __doc__ is not None
    parser = runner.parser(__doc__.splitlines()[0])
    parser.add_argument(
        "--game",
        choices=sorted(GAMES),
        action="append",
        help="the game to model; every game when not given",
    )
    parser.add_argument(
        "--preset",
        choices=("game-first", "balanced", "calls-first"),
        help="the preset of the server's rate settings; balanced when not given",
    )
    parser.add_argument(
        "--shape",
        action="append",
        help="run only the shapes whose names start with this, or 'waits' for the waits; "
        "every shape when not given",
    )
    args = parser.parse_args()
    results = []
    environment = {}
    for game in args.game or GAMES:
        results += measure_game(
            args.server, game, args.game, args.shape, args.preset, environment
        )
    runner.report(results, SUITE, environment, args.json, render=table)
    return 0


def measure_game(executable, game, games, shapes, preset, environment):
    """Run every shape against one game. Each point gets a TestServer of its own, so that
    adaptive rate control carries nothing from one point to the next."""
    environment["%s game" % game] = ", ".join(options(game, preset))
    results = []
    # The waits are the runtime's, the same whatever the game, so they run once
    if _selected("waits", shapes) and game == (games or list(GAMES))[0]:
        with serving(executable, game, preset, environment) as (conn, _, _):
            results += measure_waits(conn)
    for shape, points in SHAPES:
        if not _selected(shape, shapes):
            continue
        scenario = "%s game, %s" % (game, shape)
        for label, clients in points:
            served = serving(executable, game, preset, environment)
            with served as (_, server, counters):
                if clients == "stall":
                    results += measure_stall(scenario, label, server, counters)
                else:
                    results += measure_point(scenario, label, clients, server, counters)
    return results


@contextlib.contextmanager
def serving(executable, game, preset, environment):
    """Start a TestServer modeling a game, and yield a connection to it, its host and port,
    and its counters."""
    with testserver.running(executable, extra=options(game, preset)) as endpoint:
        with testserver.connect("update loop counters", endpoint) as conn:
            environment.setdefault("server", conn.krpc.get_status().version)
            environment.setdefault(
                "settings", testserver.settings(conn, frame_pacing=pacing(game))
            )
            process = endpoint.process
            counters = Counters(conn, process.pid if process else None)
            yield conn, ("localhost", endpoint.address), counters


def options(game, preset):
    """TestServer's options for a game, and for a preset when one is given."""
    result = ["--%s=%s" % (name, value) for name, value in GAMES[game].items()]
    if preset:
        result.append("--preset=%s" % preset)
    return result


def pacing(game):
    """The frame loop a game runs, for the settings line."""
    return "%(frame-limit)g frames/s, fixed steps at %(fixed-rate)g/s" % GAMES[game]


def _selected(name, shapes):
    return not shapes or any(name.startswith(x) for x in shapes)


def _sweep(gaps, client):
    return [("gap %g ms" % gap, [client(gap)]) for gap in gaps]


# The client shapes, each a list of points: a label, and the clients that run at the same time.
# A client is a tuple naming what it does and its parameters.
SHAPES = (
    ("synchronous", _sweep(GAPS, lambda gap: ("sync", gap))),
    *(
        (
            "bursts of %d per tick" % calls,
            _sweep(gaps, lambda gap, calls=calls: ("burst", calls, gap)),
        )
        for calls, gaps in BURSTS
    ),
    ("one call per tick", [("", [("burst", 1, 0)])]),
    (
        "one call per tick, sometimes two",
        [
            (
                "a second after %g ms on 1 tick in %d" % (RARE_GAP, RARE_EVERY),
                [("rare", RARE_EVERY, RARE_GAP)],
            )
        ],
    ),
    (
        "varying gap",
        [("gap %g to %g ms" % VARYING_GAPS, [("varying",) + VARYING_GAPS])],
    ),
    ("idle", [("", [("idle",)])]),
    (
        "two clients",
        [("gap 0.1 ms and gap 2 ms", [("sync", 0.1), ("sync", 2)])],
    ),
    ("pipelined", [("depth %d" % x, [("pipelined", x)]) for x in DEPTHS]),
    ("not reading a stream", [("", "stall")]),
)


class Counters:
    """The server's running totals, read over a connection of their own, and the server
    process's CPU time."""

    def __init__(self, conn, pid):
        self.benchmark = conn.benchmark
        # None where the server is not a process of this machine's, such as the game
        self.pid = pid

    def read(self):
        counters = dict(self.benchmark.loop_counters())
        counters["cpu_seconds"] = cpu_seconds(self.pid)
        return counters


def cpu_seconds(pid):
    """The CPU time a process has used, or ``None`` where /proc does not say."""
    if pid is None:
        return None
    try:
        with open("/proc/%d/stat" % pid, encoding="ascii") as stat:
            fields = stat.read().rsplit(")", 1)[1].split()
    except OSError:
        return None
    # utime and stime, the 14th and 15th fields, counting the command as the 2nd
    return (int(fields[11]) + int(fields[12])) / os.sysconf("SC_CLK_TCK")


def measure_waits(conn):
    """How long each way of waiting takes on the server's runtime, measured server side."""
    benchmark = conn.benchmark
    recorded = []
    for kind in benchmark.wait_kinds:
        figures = benchmark.wait(kind, WAIT_ITERATIONS)
        for figure in WAIT_FIGURES:
            recorded.append(
                Result(
                    SUITE,
                    "waits",
                    "%s: %s" % (kind, figure),
                    [figures[figure]],
                    unit="us",
                    context=True,
                )
            )
    return recorded


def measure_point(scenario, label, clients, server, counters):
    """Run the clients against the server, a host and port, and measure the point over
    several windows."""
    context = multiprocessing.get_context("spawn")
    stop = context.Event()
    running = []
    for spec in clients:
        count = context.Value("Q", 0, lock=False)
        ready = context.Event()
        process = context.Process(
            target=run_client, args=(spec, server, count, ready, stop)
        )
        process.start()
        running.append((process, count, ready))
    try:
        for _, _, ready in running:
            if not ready.wait(30):
                raise RuntimeError("a client did not connect")
        time.sleep(WARMUP_SECONDS)
        windows = []
        each = [[] for _ in running]
        before = counters.read()
        calls_before = [count.value for _, count, _ in running]
        for _ in range(WINDOWS):
            time.sleep(WINDOW_SECONDS)
            after = counters.read()
            calls_after = [count.value for _, count, _ in running]
            windows.append(window(before, after, sum(calls_after) - sum(calls_before)))
            for i, (start, end) in enumerate(zip(calls_before, calls_after)):
                each[i].append(window(before, after, end - start))
            before, calls_before = after, calls_after
    finally:
        stop.set()
        for process, _, _ in running:
            process.join(10)
            if process.is_alive():
                process.terminate()
    recorded = results(scenario, label, windows)
    if len(running) > 1:
        # Each client's own calls, as a row of its own under the point's
        for i, client in enumerate(each):
            recorded += results(
                scenario, "%s, client %d" % (label, i + 1), client, ["per call"]
            )
    return recorded


def measure_stall(scenario, label, server, counters):
    """Run a client that adds a stream and stops reading, then read how long the longest frame
    took. The counters are read once the client has gone, as a server blocked writing to it
    answers nothing until then."""
    before = counters.read()
    client = RawClient(server)
    client.call(add_stream_request(1))
    time.sleep(STALL_SECONDS)
    client.close()
    after = counters.read()
    return results(scenario, label, [window(before, after, 0)])


def window(before, after, calls):
    """The figures for one window, from the counters at each end of it."""

    def delta(name):
        return after[name] - before[name]

    elapsed = delta("time")
    frames = delta("frames")
    per_frame = 1000.0 / frames if frames else None
    figures = {
        "per call": elapsed * 1000.0 / calls if calls else None,
        "frame": delta("frame_seconds") * per_frame if frames else None,
        "server per frame": delta("update_seconds") * per_frame if frames else None,
        "waiting per frame": delta("poll_seconds") * per_frame if frames else None,
        "longest frame": after["max_frame_seconds"] * 1000.0,
        "budget per frame": after["budget_per_frame"],
        "server cpu": None,
    }
    if before["cpu_seconds"] is not None and elapsed > 0:
        figures["server cpu"] = delta("cpu_seconds") / elapsed
    return figures


def results(scenario, label, windows, names=None):
    """One result per figure, or per figure named, its samples taken one per window."""
    recorded = []
    for name, unit, rate, _ in METRICS:
        if names is not None and name not in names:
            continue
        samples = [x[name] for x in windows if x[name] is not None]
        if not samples:
            continue
        case = "%s: %s" % (label, name) if label else name
        recorded.append(
            Result(SUITE, scenario, case, samples, unit=unit, rate=rate, context=True)
        )
    return recorded


def run_client(spec, server, count, ready, stop):
    """The body of a client process: connect, signal that it has, and call in its shape until
    told to stop."""
    kind = spec[0]
    if kind == "pipelined":
        run_pipelined(spec[1], server, count, ready, stop)
        return
    host, port = server
    conn = krpc.connect(name="update loop client", address=host, port=port)
    try:
        ready.set()
        if kind == "idle":
            stop.wait()
        elif kind == "sync":
            run_sync(conn, spec[1] / 1000.0, count, stop)
        elif kind == "burst":
            run_burst(conn, spec[1], spec[2] / 1000.0, count, stop)
        elif kind == "rare":
            run_rare(conn, spec[1], spec[2] / 1000.0, count, stop)
        elif kind == "varying":
            run_varying(conn, spec[1] / 1000.0, spec[2] / 1000.0, count, stop)
    finally:
        conn.close()


def run_sync(conn, gap, count, stop):
    call = conn.krpc.get_client_name
    while not stop.is_set():
        call()
        count.value += 1
        busy_wait(gap)


def run_burst(conn, calls, gap, count, stop):
    call = conn.krpc.get_client_name
    while not stop.is_set():
        for i in range(calls):
            if i:
                busy_wait(gap)
            call()
            count.value += 1
        time.sleep(TICK)


def run_rare(conn, every, gap, count, stop):
    call = conn.krpc.get_client_name
    tick = 0
    while not stop.is_set():
        call()
        count.value += 1
        tick += 1
        if tick % every == 0:
            busy_wait(gap)
            call()
            count.value += 1
        time.sleep(TICK)


def run_varying(conn, low, high, count, stop):
    call = conn.krpc.get_client_name
    gaps = random.Random(0)
    while not stop.is_set():
        call()
        count.value += 1
        busy_wait(gaps.uniform(low, high))


def run_pipelined(depth, server, count, ready, stop):
    """Send ``depth`` requests ahead of their responses, then read the responses, over and
    over. A raw connection, as no client sends requests ahead from one thread."""
    client = RawClient(server)
    try:
        ready.set()
        request_id = 0
        while not stop.is_set():
            for _ in range(depth):
                request_id += 1
                client.send(get_client_name_request(request_id))
            for _ in range(depth):
                client.response()
            count.value += depth
    finally:
        client.close()


def busy_wait(seconds):
    """Spin for the given time, as a client doing work between calls."""
    if seconds <= 0:
        return
    end = time.perf_counter() + seconds
    while time.perf_counter() < end:
        pass


class RawClient:
    """A connection that sends and reads messages itself, for what no client does: sending
    requests ahead of their responses, or not reading at all."""

    def __init__(self, server):
        self.transport = TCPIPTransport(*server)
        self.transport.connect()
        self.transport.send_message(
            Message(
                connection_request=ConnectionRequest(
                    client_name="update loop raw client",
                    protocol_version=krpc.PROTOCOL_VERSION,
                )
            )
        )
        message = Message.FromString(self.transport.receive_message_data())
        if message.WhichOneof("content") != "connection_response":
            raise RuntimeError("the server did not accept the connection")

    def send(self, request):
        self.transport.send_request(request)

    def response(self):
        """Read messages until a response, skipping stream updates."""
        while True:
            message = Message.FromString(self.transport.receive_message_data())
            if message.WhichOneof("content") == "response":
                return message.response

    def call(self, request):
        self.send(request)
        return self.response()

    def close(self):
        try:
            self.transport.close()
        except OSError:
            pass


def get_client_name_request(request_id):
    request = Request(id=request_id)
    request.calls.add(service="KRPC", procedure="GetClientName")
    return request


def add_stream_request(request_id):
    """A request to add a stream whose value changes on every tick."""
    payload = ProcedureCall(
        service="Benchmark",
        procedure="Payload",
        arguments=[Argument(position=0, value=varint(PAYLOAD_SIZE))],
    )
    request = Request(id=request_id)
    request.calls.add(
        service="KRPC",
        procedure="AddStream",
        arguments=[Argument(position=0, value=payload.SerializeToString())],
    )
    return request


def varint(value):
    encoded = bytearray()
    while True:
        byte = value & 0x7F
        value >>= 7
        if value:
            encoded.append(byte | 0x80)
        else:
            encoded.append(byte)
            return bytes(encoded)


def table(results, title, environment=None):
    """A block per scenario, a row per point, a column per figure."""
    lines = ["", "kRPC benchmarks - %s" % title]
    for name, value in (environment or {}).items():
        lines.append("  %s: %s" % (name, value))
    scenarios = []
    for result in results:
        if result.scenario not in scenarios:
            scenarios.append(result.scenario)
    for scenario in scenarios:
        metrics = METRICS
        if scenario == "waits":
            metrics = [(x, "us", "", "median") for x in WAIT_FIGURES]
        columns = [
            rate or "%s (%s)" % (name.removesuffix("_us"), unit)
            for name, unit, rate, _ in metrics
        ]
        found = {}
        labels = []
        for result in results:
            if result.scenario != scenario:
                continue
            label, _, name = result.case.rpartition(": ")
            if label not in labels:
                labels.append(label)
            found[(label, name)] = result
        width = max([len("point")] + [len(x) for x in labels])
        lines.append("")
        lines.append("  %s" % scenario)
        header = "    %s  %s" % (
            "point".ljust(width),
            "  ".join(x.rjust(max(len(x), 9)) for x in columns),
        )
        lines.append(header)
        lines.append("    " + "-" * (len(header) - 4))
        for label in labels:
            cells = []
            for (name, _, rate, combine), column in zip(metrics, columns):
                result = found.get((label, name))
                cells.append(_cell(result, rate, combine).rjust(max(len(column), 9)))
            lines.append("    %s  %s" % (label.ljust(width), "  ".join(cells)))
    lines.append("")
    lines.append(
        "  the median of %d windows of %g s, and the longest frame of any"
        % (WINDOWS, WINDOW_SECONDS)
    )
    lines.append(
        "  timings hold for this machine and this session only: compare runs, not numbers"
    )
    return lines


def _cell(result, rate, combine):
    if result is None:
        return "-"
    value = max(result.samples) if combine == "max" else result.median
    if rate:
        value = 1000.0 / value if value else 0
    if value >= 100:
        return "%.0f" % value
    if value >= 10:
        return "%.1f" % value
    return "%.2f" % value


if __name__ == "__main__":
    sys.exit(main())
