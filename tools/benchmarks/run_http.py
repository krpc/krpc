"""HTTP benchmarks: `bazel run //tools/benchmarks:http`.

The round trips of the client suites, over the JSON over HTTP server. HTTP has no client
library, so the calls are made with Python's standard library ``http.client``, and the figures
are the server's HTTP protocol plus one plain HTTP client. Read against the python column of
``:clients``, they compare the two protocols from the same language.

A call over one keep-alive connection is what a program sends. A call on a new connection each
time is what ``curl`` sends, and it adds a TCP handshake to each round trip.

    bazel run //tools/benchmarks:http -- --json before.json

Set ``KRPC_TEST_HTTP_PORT`` to measure a server that is already running. Its cases call the
TestService, so that server is a TestServer. ``tools/benchmarks/server/test_http.py`` measures
the game.
"""

import os
import sys
import time
import types

import krpc_http

from tools.benchmarks import run_python, runner, testserver
from tools.benchmarks.report import Result

SUITE = "client, http"

# HTTP runs over TCP/IP, so its cases share a block with the protocol buffer clients' TCP/IP
# cases, for the table of every client to set them side by side.
BLOCK = "%s over %s" % (run_python.SCENARIO, testserver.LABELS[testserver.TCP])

SERVICE = "/test-service"

# The cases of the other client suites: a name, a path and the arguments, by parameter name.
CASES = (
    ("round trip", SERVICE + "/float-to-string", {"value": 3.14159}),
    (
        "round trip, 3 arguments",
        SERVICE + "/add-multiple-values",
        {"x": 3.14159, "y": 1, "z": 2},
    ),
    (
        "round trip, list of %d values" % run_python.LIST_VALUES,
        SERVICE + "/increment-list",
        {"l": list(range(run_python.LIST_VALUES))},
    ),
)

# The case each call of which opens a connection of its own, and the call it makes
NEW_CONNECTION = "round trip, new connection"


def main():
    assert __doc__ is not None
    args = runner.arguments(__doc__.splitlines()[0])
    results, environment = measure(args.server)
    runner.report(results, SUITE, environment, args.json)
    return 0


def measure(server):
    """Measure the round trips against a server of its own, unpaced, and say what it ran
    against. ``run_client.py`` gives the reason for the pacing."""
    if "KRPC_TEST_HTTP_PORT" in os.environ:
        return run(int(os.environ["KRPC_TEST_HTTP_PORT"]), None)
    with testserver.running(
        server, frame_pacing=False, transport=testserver.HTTP
    ) as endpoint:
        return run(endpoint.address, False)


def run(port, frame_pacing, suite=SUITE, block=BLOCK, cases=CASES):
    """Measure the cases against the HTTP server on a port, and say what it ran against."""
    client = krpc_http.Client(port=port)
    try:
        _warm_up(client, cases[0])
        results = measure_calls(client, suite, block, cases)
        status = client.post("/krpc/get-status")
    finally:
        client.close()
    server_settings = types.SimpleNamespace(**status["settings"])
    return results, {
        "server": status["version"],
        "measured against": testserver.describe(server_settings, frame_pacing),
    }


def measure_calls(client, suite, block, cases):
    """Time each case over the client's connection, then the first on a new connection per
    call."""
    results = []
    for case, path, arguments in cases:
        results.append(
            _round_trip(
                suite,
                block,
                case,
                lambda path=path, arguments=arguments: client.post(path, **arguments),
            )
        )
    _, first_path, first_arguments = cases[0]

    def new_connection():
        once = krpc_http.Client(client.connection.host, client.connection.port)
        try:
            once.call("POST", first_path, first_arguments)
        finally:
            once.close()

    results.append(_round_trip(suite, block, NEW_CONNECTION, new_connection))
    return results


def _round_trip(suite, block, case, call):
    samples, settled = run_python.timed_loop(call)
    return Result(
        suite, block, case, samples, unit="ms", rate=run_python.RATE, settled=settled
    )


def _warm_up(client, case, seconds=testserver.WARMUP_SECONDS):
    """``testserver.warm_up``, over HTTP."""
    _, path, arguments = case
    deadline = time.time() + seconds
    while time.time() < deadline:
        client.post(path, **arguments)


if __name__ == "__main__":
    sys.exit(main())
