"""Server dispatch benchmarks, game-less: `bazel run //tools/benchmarks:testserver`.

What the server pays per remote procedure call - argument decode, dispatch, the procedure
itself, result encode - measured against TestServer, which starts in a second and needs no
game. This is the same measurement the in-game suite takes through TestingTools, so a change
to the server can be prototyped here and confirmed there.

The procedures measured are TestService's, chosen to isolate one part of the call path each,
and are all trivial: what is being measured is the machinery around them, not the work they
do.

    bazel run //tools/benchmarks:testserver -- --json before.json
"""

import sys

from tools.benchmarks import chunking, runner, testserver

SUITE = "server, game-less"


def main():
    args = runner.arguments(__doc__.splitlines()[0])
    # Paced, as the game is. These cases time a loop the server runs inside one call, so the
    # update the call landed in is amortized away and the pacing does not reach the numbers.
    with testserver.connection("benchmark_dispatch", args.server) as (conn, _):
        testserver.warm_up(conn)
        environment = testserver.environment(conn)
        results = [measure(conn, name, call, note) for name, call, note in cases(conn)]
    runner.report(results, SUITE, environment, args.json)
    return 0


def cases(conn):
    """The calls to measure, and what each one isolates."""
    service = conn.test_service
    obj = service.create_test_object("benchmark")
    # The property starts out null, and a procedure that is not marked nullable refuses to
    # return one, so there would be nothing to measure until it has been set.
    service.string_property = "benchmark"
    return [
        (
            "no arguments",
            conn.get_call(getattr, service, "string_property"),
            "the floor: dispatch and a string result, nothing to decode",
        ),
        (
            "one argument",
            conn.get_call(service.float_to_string, 3.14159),
            "one value decoded",
        ),
        (
            "three arguments",
            conn.get_call(service.add_multiple_values, 3.14159, 1, 2),
            "three values decoded, of three types",
        ),
        (
            "object method",
            conn.get_call(obj.get_value),
            "the instance is an identifier the object store has to resolve",
        ),
        (
            "object argument and result",
            conn.get_call(service.echo_test_object, obj),
            "the store on both sides of the call: one lookup in, one dedup out",
        ),
    ]


def measure(conn, name, call, note=""):
    """Run one call in a loop server side and record what it cost."""
    return chunking.result(
        SUITE,
        None,
        name,
        # The Benchmark service has no pre-generated stubs in any client, so the python client
        # builds it from the definitions the server hands over on connecting.
        chunking.chunks(lambda count: conn.benchmark.call(call, count)),
        note=note,
    )


if __name__ == "__main__":
    sys.exit(main())
