"""Every client in one table: `bazel run //tools/benchmarks:clients`.

The measurement each client's own target takes - a TestServer of its own, a warmup, then the
round trip cases timed from inside the client - run once per language and reported as a column
apiece, so that what a call costs in one client can be read against another.

    bazel run //tools/benchmarks:clients -- --json before.json

The clients are measured one after another rather than at once: run together they would be
timing each other's load as well as their own. Each gets a server of its own, so a column here
and a run of that client's target are the same measurement. Every transport the machine has is
measured, and reported as a table apiece.

The file `--json` writes holds every client's results together, and `//tools/benchmarks:compare`
reads it as it reads any other run, reporting a block per client.
"""

import sys

from tools.benchmarks import report, run_client, run_python, runner

SUITE = "clients"

# The python client is measured in this process, since it needs no program of its own; the
# others arrive as `--client language=path` from the bazel target.
PYTHON = "python"


def main():
    args = arguments()
    results = []
    environment = {}
    for name, client in [(PYTHON, None)] + args.client:
        measured, version, settings = measure(args, name, client)
        results += measured
        # One line for the lot, not one per client: they are measured against servers started
        # the same way, and a table whose columns ran under different settings would be
        # reporting two things at once.
        environment.setdefault("server", version)
        environment.setdefault("measured against", settings)
    runner.report(results, SUITE, environment, args.json, render=report.by_suite)
    return 0


def measure(args, name, client):
    """Measure one client, and say what it was measured against."""
    if client is None:
        results, environment = run_python.measure(args.server)
    else:
        results, environment = run_client.measure(
            args.server,
            "client, %s" % name,
            client,
            dict(args.client_localsocket).get(name),
        )
    return results, environment["server"], environment["measured against"]


def arguments():
    parser = runner.parser(__doc__.splitlines()[0])
    parser.add_argument(
        "--client",
        metavar="LANGUAGE=PATH",
        action="append",
        default=[],
        type=_client,
        help="a client's benchmark program (the bazel target supplies these)",
    )
    parser.add_argument(
        "--client-localsocket",
        metavar="LANGUAGE=PATH",
        action="append",
        default=[],
        type=_client,
        help="the same program built for the local socket transport, for a client that "
        "chooses its transport when it is built rather than when it runs (the bazel "
        "target supplies these)",
    )
    return parser.parse_args()


def _client(value):
    """Read one `--client LANGUAGE=PATH`, which names a column and what fills it."""
    name, separator, path = value.partition("=")
    if not separator or not name or not path:
        raise ValueError("expected LANGUAGE=PATH, got %r" % value)
    return name, path


if __name__ == "__main__":
    sys.exit(main())
