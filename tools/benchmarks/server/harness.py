"""The timing harness the in-game benchmark scripts are written against.

The measuring itself happens server side, through two services, and they answer different
questions:

 * ``Benchmark.Call`` runs a procedure call through the server's own call path - argument
   decode, dispatch, the procedure, result encode - so it says what the server pays for a
   remote procedure. Any procedure of any service, named from here, with no server-side code
   per case. It is the same service TestServer exposes, so a number taken here and one taken
   game-less are the same measurement. Encoding a result allocates whatever the reply is made
   of, so the allocation figure for these cases is the whole call's and never zero; what it is
   good for is noticing that it moved.

   KSP's Mono provides no per-thread allocation counter, so in game the figure is the change
   in the size of the heap, and it is reported only for a loop no collection ran during.
 * ``TestingTools.BenchmarkPart`` and ``BenchmarkVessel`` run a named case from a registry
   that needs the game: the ways of getting from an identifier back to a game object, which no
   remote procedure exposes on its own.

This module decides how big a chunk is and how many of them to take, subtracts the cost of the
loop itself where there is one to subtract, and records the result for ``conftest.py`` to
report.

A benchmark script subclasses ``BenchmarkTestCase`` and is otherwise an ordinary ``krpctest``
test: it sets up its scene in ``setUpClass`` and measures in ``test_`` methods. Nothing here
asserts on a measurement. A benchmark exists to produce a number for a run to be compared
against, and a number that fails is a number that was not reported.

Nothing pauses the game. A timed loop runs on the game's main thread, inside the server's
update, so physics and rendering cannot interleave with it however long it takes; and a server
configured to pause with the game stops answering RPCs the moment the game is paused, which
would leave the suite waiting for a reply that never comes.
"""

import krpctest

from tools.benchmarks import chunking
from tools.benchmarks.report import Result

# Every measurement taken in this session, in the order they were taken. conftest.py prints
# these as a table once the run finishes, and writes them to --json.
RESULTS = []

SUITE = "server, in-game"


class BenchmarkTestCase(krpctest.TestCase):
    """A krpctest.TestCase that can measure server-side cases.

    Subclasses set ``scenario`` to name the scene they set up, since a number only means
    something alongside the craft it was taken on.
    """

    scenario = None

    # Cost of the empty case, keyed by test class and entry point. Measured once per class,
    # since the empty case says what the timing loop itself costs and nothing about the
    # scene, but recorded per class so every scenario's report stands on its own.
    _baselines = {}

    def measure_call(self, name, call):
        """Measure what the server pays for a remote procedure call.

        ``call`` is a ``KRPC.ProcedureCall``, built with ``Client.get_call``, so any procedure
        of any service can be measured from here. There is no empty case to subtract: the loop
        overhead is single-digit nanoseconds against a call that costs hundreds, and there is
        no delegate shape to match it against.
        """
        benchmark = self.connect().benchmark
        return self._measure(
            None, lambda case, count: benchmark.call(call, count), name
        )

    def measure_part(self, part, case):
        """Measure a case from the part registry against the given part."""
        tools = self.connect().testing_tools
        return self._measure(
            "part", lambda name, count: tools.benchmark_part(part, name, count), case
        )

    def measure_vessel(self, vessel, case):
        """Measure a case from the vessel registry against the given vessel."""
        tools = self.connect().testing_tools
        return self._measure(
            "vessel",
            lambda name, count: tools.benchmark_vessel(vessel, name, count),
            case,
        )

    def record(self, case, samples, unit, note=""):
        """Record samples that did not come from a benchmark RPC, so they appear in the
        report alongside the ones that did."""
        result = Result(SUITE, self.scenario, case, samples, unit=unit, note=note)
        RESULTS.append(result)
        return result

    # How long to let the stream update reading settle before sampling it, how many samples
    # to take, and how long to leave between them. time_per_stream_update is an exponential
    # moving average, so it takes a few updates to reach the value for the streams that were
    # just added.
    stream_settle_seconds = 2
    stream_samples = 10
    stream_sample_interval = 0.1

    def measure_stream_update(self, parts, attribute="mass"):
        """Measure how long the server spends updating one stream per part, from its own
        timing rather than a round trip. This is the workload the accessors exist for: every
        stream is re-evaluated every fixed update, so its cost is frame time in game.

        Unlike the other cases this needs the simulation running, and it measures whatever
        the game is doing at the time as well, so it is the noisiest thing here."""
        conn = self.connect()
        streams = [conn.add_stream(getattr, part, attribute) for part in parts]
        try:
            # A stream the client has not read yet is not started, and the server skips it,
            # so starting them explicitly is what makes this measure anything at all.
            for stream in streams:
                stream.start(wait=False)
            self.wait(self.stream_settle_seconds)
            status = conn.krpc.get_status()
            # Scene sanity rather than a measurement: each stream is evaluated every fixed
            # update, so the rate is a large multiple of the number of streams. Anything less
            # means they are not all running, and the timing below is an empty loop.
            self.assertEqual(len(streams), status.stream_rpcs)
            self.assertGreater(status.stream_rpc_rate, len(streams))
            samples = []
            for _ in range(self.stream_samples):
                samples.append(conn.krpc.get_status().time_per_stream_update * 1e3)
                self.wait(self.stream_sample_interval)
        finally:
            for stream in streams:
                stream.remove()
        return self.record(
            "stream update",
            samples,
            "ms",
            note="%d streams over part.%s" % (len(streams), attribute),
        )

    def _measure(self, entry_point, run, case):
        baseline = None
        if entry_point is not None and case != "empty":
            baseline = self._baseline(entry_point, run)
        result = chunking.result(
            SUITE,
            self.scenario,
            case,
            chunking.chunks(lambda count: run(case, count)),
            baseline=baseline,
        )
        RESULTS.append(result)
        return result

    def _baseline(self, entry_point, run):
        key = (type(self), entry_point)
        if key not in self._baselines:
            result = chunking.result(
                SUITE,
                self.scenario,
                "empty (%s)" % entry_point,
                chunking.chunks(lambda count: run("empty", count)),
                note="loop and dispatch overhead, subtracted from the cases below",
                context=True,
            )
            RESULTS.append(result)
            self._baselines[key] = result.best
        return self._baselines[key]
