"""Run a client's benchmark program against TestServer.

Measuring what a client costs means timing it from inside that client, in its own language,
so there is one benchmark program per language. What they share is this runner: it starts a
TestServer, hands the program the ports, and turns what it prints into the same table every
other suite prints.

The contract is one JSON document on standard output::

    {"results": [{"case": "round trip", "unit": "ms", "samples": [0.021, ...],
                  "rate": "calls/s", "note": "", "settled": true}, ...]}

``samples`` is one timing per repeat, in ``unit``; the fastest is the estimate, as everywhere
else. The rest are optional. ``rate`` names the unit of the reciprocal, so the rate a figure
works out to is phrased the same way for every client rather than four times over. ``settled``
says whether the case had stopped getting faster before it was measured; a case that had not is
reported as an upper bound rather than as a cost.

A benchmark program is only asked for numbers. Everything said about them - the table, the
spread, whether a case drifted while it ran, the JSON for `compare` - happens here, so that
five languages cannot disagree about it.

The server it is measured against runs unpaced. A round trip is served over and over inside one
update for as long as the client answers within the receive timeout, so paced it is the client's
cost inflated by the part of each update that does not go to RPCs, and once the client's own
turnaround passes that timeout it stops being the client's cost at all and becomes 16.7 ms, the
update rate. Unpaced the number is the client and nothing else.
"""

import json
import os
import subprocess
import sys

from tools.benchmarks import runner, testserver
from tools.benchmarks.report import Result

SCENARIO = "round trips"


def main():
    args = arguments()
    suite = "client, %s" % args.name
    cases, version, settings = run(args.server, args.client)
    results = [record(suite, case) for case in cases]
    environment = {"server": version, "measured against": settings}
    runner.report(results, suite, environment, args.json)
    return 0


def run(server, client):
    """Run a client's benchmark program against a server of its own.

    Returns the cases it printed, the server's version, and what that server was configured to
    do. The settings are read after the program has finished, so that the update budget they
    report is the one the measurement ran under.
    """
    with testserver.running(server, frame_pacing=False) as (rpc_port, stream_port):
        # On a connection of our own, closed again before the program runs, so that what is
        # measured is one client talking to the server, as it was before the warmup.
        with testserver.connect("benchmark_warmup", rpc_port, stream_port) as conn:
            testserver.warm_up(conn)
        cases = parse(measure(client, rpc_port, stream_port))["results"]
        if not cases:
            raise RuntimeError("the benchmark program measured nothing")
        with testserver.connect("benchmark_settings", rpc_port, stream_port) as conn:
            return (
                cases,
                conn.krpc.get_status().version,
                testserver.settings(conn, False),
            )


def arguments():
    parser = runner.parser(__doc__.splitlines()[0])
    parser.add_argument(
        "--name",
        metavar="LANGUAGE",
        required=True,
        help="the client being measured, as it should appear in the report",
    )
    parser.add_argument(
        "--client",
        metavar="PATH",
        required=True,
        help="the client's benchmark program (the bazel target supplies this)",
    )
    return parser.parse_args()


def measure(client, rpc_port, stream_port):
    """Run the client's benchmark program and return what it printed."""
    environment = dict(os.environ)
    environment["RPC_PORT"] = str(rpc_port)
    environment["STREAM_PORT"] = str(stream_port)
    process = subprocess.run(
        [os.path.abspath(client)],
        env=environment,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        check=False,
    )
    if process.returncode != 0:
        raise RuntimeError(
            "%s exited with status %d:\n%s\n%s"
            % (client, process.returncode, process.stdout, process.stderr)
        )
    return process.stdout


def parse(output):
    """Read the results out of what the benchmark program printed.

    The results are the last line: a language's launcher may have had something to say first,
    and that is not a reason to lose a run that otherwise worked.
    """
    lines = [line for line in output.splitlines() if line.strip()]
    if not lines:
        raise RuntimeError("the benchmark program printed nothing")
    try:
        return json.loads(lines[-1])
    except json.JSONDecodeError as exc:
        raise RuntimeError(
            "could not read results from the benchmark program:\n%s" % output
        ) from exc


def record(suite, case):
    """Turn one case a benchmark program printed into a result."""
    return Result(
        suite,
        SCENARIO,
        case["case"],
        case["samples"],
        unit=case["unit"],
        note=case.get("note", ""),
        rate=case.get("rate", ""),
        settled=case.get("settled", True),
    )


if __name__ == "__main__":
    sys.exit(main())
