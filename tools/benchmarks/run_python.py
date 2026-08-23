"""Python client benchmarks: `bazel run //tools/benchmarks:python`.

What a client pays, measured from outside the server against TestServer: the round trip for a
remote procedure call, and what a call carrying a collection of values costs. Everything here
includes the network, the client's own encoding and decoding, and the server's dispatch, which
is the cost a program written against kRPC sees.

Every figure is a time per operation, so lower is always better; the rates they work out to
are in the notes under each block.

Each case is measured over every transport the machine has, TCP/IP and a local socket, since
which one carries a call is part of what the call costs.

    bazel run //tools/benchmarks:python -- --json before.json
"""

import sys
import time

from tools.benchmarks import runner, testserver
from tools.benchmarks.report import Result

SUITE = "client, python"
SCENARIO = "round trips"

# What a round trip's figure counts, for the report to work its reciprocal out from.
RATE = "calls/s"

# How long one timed loop should run for. Long enough that the clock and a stray scheduling
# delay do not decide the answer, short enough that a whole run stays in seconds.
TARGET_SECONDS = 0.2

# How many timed loops to take. More than the server-side suites take, because a round trip
# crosses a socket and a scheduler as well as the server, and every one of those can only make
# a sample slower.
SAMPLES = 9

# How long one discarded chunk of calls runs for while a case is being settled, and how many
# of them at a time are asked whether it has stopped getting faster.
SETTLE_CHUNK_SECONDS = 0.1
SETTLE_CHUNKS = 3

# How much better the last few chunks have to be than everything before them for a case to
# count as still improving. Below this it has stopped going anywhere and can be measured.
SETTLE_TOLERANCE = 0.02

# How long to keep settling one case before measuring it anyway. Reaching this means the
# figure is whatever the case had reached rather than what it costs, so it is reported.
SETTLE_TIMEOUT_SECONDS = 10.0

# How many values the collection case sends and gets back. A call carries a value at a time, so
# what one costs to encode and decode is lost in the round trip it arrives in; a list makes that
# per-value cost most of what the case measures. The same count for every client, so that the
# figures can be read against each other.
LIST_VALUES = 100


def main():
    args = runner.arguments(__doc__.splitlines()[0])
    results, environment = measure(args.server)
    runner.report(results, SUITE, environment, args.json)
    return 0


def measure(server):
    """Measure the client over every transport, and say what it ran against.

    A server per transport, each unpaced, so that a round trip is the client's cost rather than
    the update it landed in. `run_client.py` has the long version.
    """
    results = []
    environment = {}
    for transport in testserver.transports():
        with testserver.connection(
            "benchmark_python", server, frame_pacing=False, transport=transport
        ) as (conn, pacing):
            testserver.warm_up(conn)
            results += measure_calls(conn, transport)
            environment.setdefault("server", conn.krpc.get_status().version)
            # What the server was told is the same whichever transport it serves, so the first
            # one measured says it for the run.
            environment.setdefault(
                "measured against", testserver.settings(conn, pacing)
            )
    return results, environment


def measure_calls(conn, transport):
    """Round-trip time for a remote procedure call, with and without arguments to encode, and
    for one whose argument and result are a collection of values."""
    service = conn.test_service
    values = list(range(LIST_VALUES))
    block = "%s over %s" % (SCENARIO, testserver.LABELS[transport])
    return [
        round_trip(block, "round trip", lambda: service.float_to_string(3.14159)),
        round_trip(
            block,
            "round trip, 3 arguments",
            lambda: service.add_multiple_values(3.14159, 1, 2),
        ),
        round_trip(
            block,
            "round trip, list of %d values" % LIST_VALUES,
            lambda: service.increment_list(values),
        ),
    ]


def round_trip(block, case, call):
    samples, settled = timed_loop(call)
    return Result(SUITE, block, case, samples, unit="ms", rate=RATE, settled=settled)


def timed_loop(call):
    """Time the call in a loop, several times over, and return milliseconds per call along
    with whether the case had settled before any of it was measured."""
    per_call, settled = settle(call)
    iterations = max(int(TARGET_SECONDS * 1e3 / per_call), 1)

    samples = []
    for _ in range(SAMPLES):
        start = time.perf_counter()
        for _ in range(iterations):
            call()
        samples.append((time.perf_counter() - start) * 1e3 / iterations)
    return samples, settled


def settle(call):
    """Make discarded calls until they stop getting faster, and return what one costs along
    with whether it got there.

    A fixed warmup cannot know when it is done. Both ends of a round trip get faster under
    load for a while - the server's rate control adapts to what it is being asked for, and its
    runtime recompiles the paths it finds itself running - and a case measured before that
    finishes returns a curve rather than a cost, whose fastest sample is merely the last one
    and whose spread is the distance it travelled. Timing chunks until the best of the last
    few stops improving on everything before them measures a case that has arrived, and every
    case is settled on its own, since one settled by the traffic of the case before it will
    say so within a few chunks.

    The cost of a call also falls out of the last chunk, which is what sizes the timed loops.
    """
    chunks = [_chunk(call)]
    deadline = time.perf_counter() + SETTLE_TIMEOUT_SECONDS
    while time.perf_counter() < deadline:
        chunks.append(_chunk(call))
        if len(chunks) > SETTLE_CHUNKS:
            recent = min(chunks[-SETTLE_CHUNKS:])
            earlier = min(chunks[:-SETTLE_CHUNKS])
            if recent >= earlier * (1 - SETTLE_TOLERANCE):
                return recent, True
    return min(chunks[-SETTLE_CHUNKS:]), False


def _chunk(call):
    """Call for a short while and return the milliseconds one call took."""
    start = time.perf_counter()
    calls = 0
    while time.perf_counter() - start < SETTLE_CHUNK_SECONDS:
        call()
        calls += 1
    return (time.perf_counter() - start) * 1e3 / max(calls, 1)


if __name__ == "__main__":
    sys.exit(main())
