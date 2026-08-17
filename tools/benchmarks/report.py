"""The result record and the table every benchmark runner prints.

There are three runners - the in-game server suite, the game-less server suite and the client
suites - and they measure different things in different units. What they share is this module,
so that two runs can be read side by side and, more to the point, handed to ``compare.py``.

Nothing is kept between runs. An absolute timing only means something on the machine and in the
session that produced it, so the way to use these numbers is to run a suite before a change and
again after it, on the same machine, and compare the two files.
"""

import json
import statistics

# A case whose typical sample ran this much further above its fastest is called out under its
# block, since a number that unsteady is not one to draw a conclusion from.
NOISY_SPREAD = 0.25

# A case whose samples moved this much between the start of the measurement and the end, on top
# of however far they scattered, is called out under its block. Drift and noise both widen the
# spread but mean opposite things: noise says the estimate is uncertain, drift says the case was
# still going somewhere while it was measured and the number is wherever it had got to. The
# second is worth another run.
#
# The spread is part of the threshold because the two ends being compared are each the middle of
# a few samples, and samples that scatter widely give middles that differ by about as much on
# their own. A case has only gone somewhere when it went further than its own scatter would have
# carried it; below that it is a noisy case, which is what the spread column already says.
DRIFT = 0.10


class Result:
    """One recorded measurement: the samples taken for a case, and what they cost.

    ``samples`` is one value per chunk, in the unit named by ``unit``. The distribution is one
    sided, since interference only ever makes a sample slower, so the minimum is the best
    estimate of the cost and the spread says how far the machine got in the way of the rest.
    """

    # pylint: disable=too-many-instance-attributes,too-many-arguments
    # pylint: disable=too-many-positional-arguments
    def __init__(
        self,
        suite,
        scenario,
        case,
        samples,
        unit="ns/op",
        baseline=None,
        bytes_per_op=None,
        collections=None,
        exact_allocations=None,
        iterations=None,
        note="",
        context=False,
    ):
        self.suite = suite
        self.scenario = scenario
        self.case = case
        self.samples = list(samples)
        self.unit = unit
        # Cost of the empty case for the same entry point, if this case has one to subtract:
        # a delegate call and the loop bookkeeping, which are not part of what is measured.
        self.baseline = baseline
        self.bytes_per_op = bytes_per_op
        self.collections = collections
        self.exact_allocations = exact_allocations
        self.iterations = iterations
        self.note = note
        # A row that is there to explain the others rather than to be concluded from - the
        # cost of the empty loop, say, which is subtracted from every case beside it. Still
        # worth comparing between runs, but not worth warning that it wobbled.
        self.context = context

    @property
    def best(self):
        return min(self.samples)

    @property
    def median(self):
        return statistics.median(self.samples)

    @property
    def spread(self):
        """How far the typical sample ran above the fastest, as a fraction of the fastest.

        Measured against the median rather than the slowest sample. One sample landing on a
        garbage collection says nothing about how far the estimate can be trusted, and a case
        that allocates will produce one every run; what does say something is where the middle
        of the distribution sits.
        """
        best = self.best
        if best <= 0:
            return 0.0
        return (self.median - best) / best

    @property
    def drift(self):
        """How far the case moved from the start of the measurement to the end, as a fraction
        of the fastest sample. Positive where it ended slower than it started.

        The spread cannot tell a case that wandered from one that was merely noisy, and they
        call for different things: a noisy case has been measured, a drifting one has not
        settled and its figure is wherever it had reached when the samples ran out. Comparing
        thirds rather than the first sample with the last, so that one sample landing badly at
        either end does not read as a trend.
        """
        third = len(self.samples) // 3
        best = self.best
        if third == 0 or best <= 0:
            return 0.0
        start = statistics.median(self.samples[:third])
        end = statistics.median(self.samples[-third:])
        return (end - start) / best

    @property
    def value(self):
        """The number the table shows: the fastest sample, with the cost of the empty case
        taken off where there is one to subtract."""
        if self.baseline is None:
            return self.best
        return self.best - self.baseline

    @property
    def key(self):
        return (self.suite, self.scenario or "", self.case)

    def as_dict(self):
        return {
            "suite": self.suite,
            "scenario": self.scenario,
            "case": self.case,
            "unit": self.unit,
            "samples": self.samples,
            "value": self.value,
            "best": self.best,
            "median": self.median,
            "spread": self.spread,
            "drift": self.drift,
            "baseline": self.baseline,
            "bytes_per_op": self.bytes_per_op,
            "collections": self.collections,
            "exact_allocations": self.exact_allocations,
            "iterations": self.iterations,
            "note": self.note,
            "context": self.context,
        }

    @classmethod
    def from_dict(cls, data):
        return cls(
            data["suite"],
            data["scenario"],
            data["case"],
            data["samples"],
            unit=data["unit"],
            baseline=data["baseline"],
            bytes_per_op=data["bytes_per_op"],
            collections=data["collections"],
            exact_allocations=data["exact_allocations"],
            iterations=data["iterations"],
            note=data["note"],
            context=data.get("context", False),
        )


def number(value, width=None):
    """Format a measurement for a column: enough digits to tell two runs apart, and no more."""
    if value is None:
        text = ""
    elif value == 0:
        text = "0"
    elif abs(value) >= 1000:
        text = "%.0f" % value
    elif abs(value) >= 1:
        text = "%.2f" % value
    else:
        text = "%.4g" % value
    return text if width is None else text.rjust(width)


def table(results, title, environment=None):
    """Render the results as a table: one block per scenario, one row per case.

    ``environment`` is the run's context - what it ran against, and any server setting that
    changes what the numbers mean - printed above the blocks, since a throughput figure without
    them cannot be compared with anything.
    """
    lines = ["", "kRPC benchmarks - %s" % title]
    for name, value in (environment or {}).items():
        lines.append("  %s: %s" % (name, value))
    for scenario in _scenarios(results):
        lines.append("")
        lines.extend(
            _block(scenario, [x for x in results if (x.scenario or "") == scenario])
        )
    lines.append("")
    if any(x.exact_allocations is False for x in results):
        # KSP's Mono has no GC.GetAllocatedBytesForCurrentThread, so in game the figure is
        # the change in the size of the heap, which a collection inside the window invalidates.
        lines.append(
            "  no per-thread allocation counter on this runtime: bytes/op is the change in"
        )
        lines.append(
            "  heap size, and blank where a collection ran and made that meaningless"
        )
    lines.append(
        "  timings hold for this machine and this session only: compare runs, not numbers"
    )
    return lines


def _scenarios(results):
    seen = []
    for result in results:
        scenario = result.scenario or ""
        if scenario not in seen:
            seen.append(scenario)
    return seen


def _block(scenario, results):
    # Only show the allocation columns for a suite that measured allocations; a client
    # benchmark, which times a round trip from outside the server, has nothing to put there.
    allocations = any(x.bytes_per_op is not None for x in results)
    case_width = max([len("case")] + [len(x.case) for x in results])
    unit_width = max([len("unit")] + [len(x.unit) for x in results])

    columns = [
        ("case", case_width, False),
        ("value", 10, True),
        ("unit", unit_width, False),
    ]
    if allocations:
        columns += [("bytes/op", 9, True), ("gc", 3, True)]
    columns.append(("spread", 7, True))

    header = _row(columns)
    lines = []
    if scenario:
        lines.append("  %s" % scenario)
    lines.append(header)
    lines.append("    " + "-" * (len(header) - 4))
    for result in results:
        cells = [
            (result.case, case_width, False),
            (number(result.value), 10, True),
            (result.unit, unit_width, False),
        ]
        if allocations:
            cells += [
                (number(result.bytes_per_op), 9, True),
                (
                    "" if result.collections is None else str(result.collections),
                    3,
                    True,
                ),
            ]
        cells.append(("%.1f%%" % (100 * result.spread), 7, True))
        lines.append(_row(cells))
    for result in results:
        if result.note:
            lines.append("    %s: %s" % (result.case, result.note))
        if abs(result.drift) > DRIFT + result.spread and not result.context:
            lines.append(
                "    %s: ran %.0f%% %s by the end than at the start, so it was still "
                "settling rather than noisy - measure it again"
                % (
                    result.case,
                    100 * abs(result.drift),
                    "slower" if result.drift > 0 else "faster",
                )
            )
        elif result.spread > NOISY_SPREAD and not result.context:
            lines.append(
                "    %s: samples spread %.0f%%, too unsteady to draw a conclusion from"
                % (result.case, 100 * result.spread)
            )
    return lines


def _row(cells):
    return (
        "    "
        + "  ".join(
            text.rjust(width) if right else text.ljust(width)
            for text, width, right in cells
        )
    ).rstrip()


def write_json(results, path, title, environment=None):
    """Write the results to a file, for compare.py to read back."""
    with open(path, "w", encoding="utf-8") as file:
        json.dump(
            {
                "title": title,
                "environment": environment or {},
                "results": [result.as_dict() for result in results],
            },
            file,
            indent=2,
        )


def read_json(path):
    """Read back what write_json wrote: the title, the environment and the results."""
    with open(path, encoding="utf-8") as file:
        data = json.load(file)
    return (
        data["title"],
        data["environment"],
        [Result.from_dict(x) for x in data["results"]],
    )
