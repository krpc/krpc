"""Each step of a call on its own, in game: parsing the request, decoding the arguments, the
call, encoding the result and serializing the response. The same cases as
``//tools/benchmarks:testserver -- --steps``, on KSP's Mono, plus game calls on the reference
craft.

Run it with::

    bazel run //tools/benchmarks:server -- tools/benchmarks/server/test_steps.py
"""

from tools.benchmarks import chunking, steps
from tools.benchmarks.server.harness import RESULTS, SUITE, BenchmarkTestCase
from tools.benchmarks.server.test_reference_craft import CRAFT_DIRECTORY


class TestSteps(BenchmarkTestCase):
    """The reference craft in orbit, for the game calls."""

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Parts", directory=CRAFT_DIRECTORY)
        cls.set_circular_orbit("Kerbin", 100000)

    def test_steps(self):
        conn = self.connect()
        part = self.vessel.parts.root
        cases = steps.cases(conn) + [
            ("part.name", conn.get_call(getattr, part, "name")),
            ("part.mass", conn.get_call(getattr, part, "mass")),
            ("vessel.parts.all", conn.get_call(getattr, self.vessel.parts, "all")),
        ]
        benchmark = conn.benchmark
        for step in benchmark.call_steps:
            for name, call in cases:
                RESULTS.append(
                    chunking.result(
                        SUITE,
                        "steps, " + step,
                        name,
                        chunking.chunks(
                            lambda count, call=call, step=step: benchmark.call_step(
                                call, step, count
                            )
                        ),
                    )
                )
