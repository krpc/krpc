"""Round trips in game, from the python client over TCP/IP and over the HTTP server, from the same
process. The cases of ``//tools/benchmarks:http``, on the Benchmark service, as the game has no
TestService, and a game property.

Run it with::

    bazel run //tools/benchmarks:server -- tools/benchmarks/server/test_http.py
"""

import krpc_http

from tools.benchmarks import run_http, run_python
from tools.benchmarks.report import Result
from tools.benchmarks.server.harness import RESULTS, SUITE, BenchmarkTestCase
from tools.benchmarks.server.test_reference_craft import CRAFT_DIRECTORY

# The port of the test game's HTTP server, in krpctools/game/ksp/settings.cfg
HTTP_PORT = 50003

VALUES = list(range(run_python.LIST_VALUES))

CASES = (
    ("round trip", "/benchmark/echo-string", {"value": "3.14159"}),
    ("round trip, 3 arguments", "/benchmark/add", {"x": 3.14159, "y": 1, "z": 2}),
    (
        "round trip, list of %d values" % run_python.LIST_VALUES,
        "/benchmark/echo-list",
        {"value": VALUES},
    ),
)


class TestHTTP(BenchmarkTestCase):
    """The reference craft in orbit, for the game property."""

    scenario = "round trips, Parts in orbit"

    @classmethod
    def setUpClass(cls):
        cls.launch("Parts", directory=CRAFT_DIRECTORY)
        cls.set_circular_orbit("Kerbin", 100000)

    def test_python(self):
        conn = self.connect()
        benchmark = conn.benchmark
        space_center = conn.space_center
        calls = (
            lambda: benchmark.echo_string("3.14159"),
            lambda: benchmark.add(3.14159, 1, 2),
            lambda: benchmark.echo_list(VALUES),
            lambda: space_center.active_vessel,
        )
        names = [case for case, _, _ in CASES] + ["round trip, active vessel"]
        for name, call in zip(names, calls):
            self._record("python", name, call)

    def test_http(self):
        client = krpc_http.Client(self.game().address, HTTP_PORT)
        try:
            for result in run_http.measure_calls(
                client, SUITE, self._block("http"), CASES
            ):
                RESULTS.append(result)
            self._record(
                "http",
                "round trip, active vessel",
                lambda: client.get("/space-center/active-vessel"),
            )
        finally:
            client.close()

    def _block(self, client):
        return "%s, %s" % (self.scenario, client)

    def _record(self, client, case, call):
        samples, settled = run_python.timed_loop(call)
        RESULTS.append(
            Result(
                SUITE,
                self._block(client),
                case,
                samples,
                unit="ms",
                rate=run_python.RATE,
                settled=settled,
            )
        )
