"""The update loop in game: what clients of different shapes get from the server, and what the
loop costs the game's frame. The same shapes as ``//tools/benchmarks:update-loop``, which runs
them against TestServer's model of a game loop. These measure the game itself, to check the
model against.

Also how the ways of waiting behave on the game's runtime, as KSP's Mono is not the .NET that
TestServer runs on.

Run it with::

    bazel run //tools/benchmarks:server -- tools/benchmarks/server/test_update_loop.py
"""

from tools.benchmarks import run_update_loop
from tools.benchmarks.server.harness import RESULTS, SUITE, BenchmarkTestCase
from tools.benchmarks.server.test_reference_craft import CRAFT_DIRECTORY

# The shapes to run in game. The client that stops reading a stream is left out, as it blocks
# the game until the client closes.
SHAPES = (
    "synchronous",
    "bursts",
    "one call per tick",
    "varying gap",
    "idle",
    "two clients",
    "pipelined",
)


class TestUpdateLoop(BenchmarkTestCase):
    """The reference craft in orbit, so the game has its usual per-tick work and nothing on
    the ground."""

    scenario = "update loop, Parts in orbit"

    @classmethod
    def setUpClass(cls):
        cls.launch("Parts", directory=CRAFT_DIRECTORY)
        cls.set_circular_orbit("Kerbin", 100000)

    def test_shapes(self):
        conn = self.connect()
        counters = run_update_loop.Counters(conn, None)
        server = (self.game().address, run_update_loop.DEFAULT_PORT)
        for shape, points in run_update_loop.SHAPES:
            if not shape.startswith(SHAPES):
                continue
            for label, clients in points:
                for result in run_update_loop.measure_point(
                    "%s, %s" % (self.scenario, shape), label, clients, server, counters
                ):
                    result.suite = SUITE
                    RESULTS.append(result)

    def test_waits(self):
        for result in run_update_loop.measure_waits(self.connect()):
            result.suite = SUITE
            result.scenario = "%s, %s" % (self.scenario, result.scenario)
            RESULTS.append(result)
