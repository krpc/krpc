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

# The units whose reciprocal is a rate per second, and how many of each go into a second. A
# case measured in anything else reports no rate.
PER_SECOND = {"s": 1.0, "ms": 1e3, "us": 1e6, "ns": 1e9, "ns/op": 1e9}

# Every figure in a table of one suite per column is printed to the same number of places, so
# that a column of them reads down as a column. The last of these places are the significant
# digits of a client's round trip in milliseconds. A table of one suite still prints as many
# as its figures are worth.
FIGURE = "%.5f"

# A case whose samples moved this much between the start of the measurement and the end, on
# top of however far they scattered, is called out under its block. Drift and noise both widen
# the spread but mean opposite things: noise makes the estimate uncertain, where drift means
# the case was still moving while it was measured and the number is wherever it had got to.
# The spread is part of the threshold because the two ends being compared are each the middle
# of a few samples, and samples that scatter widely give middles that differ by about as much
# on their own.
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
        settled=True,
        rate="",
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
        # A row that explains the others, such as the cost of the empty loop, which is
        # subtracted from every case beside it. Still worth comparing between runs, and not
        # worth warning that it wobbled.
        self.context = context
        # Whether the case had stopped getting faster before it was measured. Where it had
        # not, the figure is where it had reached and not the cost of the case, and the report
        # says so beside it.
        self.settled = settled
        # The name of one operation, for a figure worth reading as a rate as well as a cost: a
        # round trip measured in milliseconds is also so many calls a second. The measurement
        # names the thing counted, and the report works the rate out.
        self.rate = rate

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
            "settled": self.settled,
            "rate": self.rate,
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
            settled=data.get("settled", True),
            rate=data.get("rate", ""),
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


def by_suite(results, title, environment=None):
    """Render results from several suites as one table: a row per case, a column per suite.

    Suites are only worth reading side by side where they measured the same cases in the same
    units, which is what the client benchmarks do and why they have a table of their own. A
    case a suite did not measure leaves its cell blank rather than dropping the row.
    """
    lines = ["", "kRPC benchmarks - %s" % title]
    for name, value in (environment or {}).items():
        lines.append("  %s: %s" % (name, value))
    suites = _fastest_first(results)
    for scenario in _scenarios(results):
        lines.append("")
        lines.extend(
            _columns(
                scenario, suites, [x for x in results if (x.scenario or "") == scenario]
            )
        )
    lines.append("")
    lines.append(
        "  timings hold for this machine and this session only: compare runs, not numbers"
    )
    return lines


def _fastest_first(results):
    """The suites in the order their columns read best: fastest first, on the first case they
    report.

    A table of clients is read to rank them, and the order the runner happened to measure them
    in ranks nothing. The first case decides it rather than every case at once, so that the
    order is one a reader can check against the top row rather than a scoring of their own.
    """
    suites = _suites(results)
    first = _cases(results)[0]
    found = {x.suite: x for x in results if x.case == first}
    return sorted(suites, key=lambda x: found[x].value if x in found else float("inf"))


def _columns(scenario, suites, results):
    cases = _cases(results)
    found = {(x.suite, x.case): x for x in results}
    case_width = max([len("case")] + [len(x) for x in cases])
    unit_width = max([len("unit")] + [len(x.unit) for x in results])
    width = max([len(FIGURE % 0)] + [len(_column(x)) for x in suites])

    def block(cell, heading=None, unit=False):
        """One block of the table: a row per case, a cell per suite, worked out by ``cell``."""
        rows = [] if heading is None else ["    %s" % heading]
        for case in cases:
            measured = [found[(s, case)] for s in suites if (s, case) in found]
            cells = [(case, case_width, False)]
            cells += [
                (cell(found.get((s, case)), measured), width, True) for s in suites
            ]
            if unit:
                cells += [("", 1, False), (measured[0].unit, unit_width, False)]
            rows.append(_row(cells))
        return rows

    header = _row(
        [("case", case_width, False)]
        + [(_column(x), width, True) for x in suites]
        + [("", 1, False), ("unit", unit_width, False)]
    )
    lines = []
    if scenario:
        lines.append("  %s" % scenario)
    lines.append(header)
    lines.append("    " + "-" * (len(header) - 4))
    lines.extend(block(_figure, unit=True))

    # The reciprocals as a block of their own. One suite's table says what each figure works
    # out to a second in a note under it, which here would be a line per case per suite.
    if any(per_second(x) is not None for x in results):
        lines.append("")
        lines.extend(block(_rate, heading=_counted(results)))

    # The cost of each suite against the one that measured the case fastest, which is the
    # comparison a table of several of them is read for: the figures above it hold for the
    # machine and the session that took them, and these hold wherever they were taken.
    if len(suites) > 1:
        lines.append("")
        lines.extend(block(_slower, heading="slower than the fastest"))

    for result in results:
        for warning in warnings(result):
            lines.append(
                "    %s, %s: %s" % (_column(result.suite), result.case, warning)
            )
    return lines


def _figure(result, measured):
    # pylint: disable=unused-argument
    return "" if result is None else FIGURE % result.value


def _rate(result, measured):
    # pylint: disable=unused-argument
    value = None if result is None else per_second(result)
    return "" if value is None else "%d" % round(value)


def _slower(result, measured):
    """How far above the fastest measurement of the same case this one is."""
    if result is None:
        return ""
    best = min(x.value for x in measured)
    if best <= 0 or result.value <= best:
        return "-"
    return "+%.0f%%" % (100 * (result.value - best) / best)


def _counted(results):
    """What the block of reciprocals counts, where every case in it counts the same thing."""
    named = {x.rate for x in results if x.rate}
    return named.pop() if len(named) == 1 else "per second"


def _column(suite):
    """A suite's name as a column heading: what tells it from the others beside it."""
    return suite.split(", ")[-1]


def _suites(results):
    seen = []
    for result in results:
        if result.suite not in seen:
            seen.append(result.suite)
    return seen


def _cases(results):
    seen = []
    for result in results:
        if result.case not in seen:
            seen.append(result.case)
    return seen


def _scenarios(results):
    seen = []
    for result in results:
        scenario = result.scenario or ""
        if scenario not in seen:
            seen.append(scenario)
    return seen


def _block(scenario, results):
    # Only show the allocation columns for a suite that measured allocations. A client
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
        note = _joined(rate_note(result), result.note)
        if note:
            lines.append("    %s: %s" % (result.case, note))
        for warning in warnings(result):
            lines.append("    %s: %s" % (result.case, warning))
    return lines


def per_second(result):
    """How many times a second the case's figure works out to, where that means anything: it
    has to count something it named, and be measured in a unit of time to be inverted.
    """
    scale = PER_SECOND.get(result.unit)
    if not result.rate or scale is None or result.value <= 0:
        return None
    return scale / result.value


def rate_note(result):
    """What the figure works out to a second, worded for a note.

    Worded here rather than by whoever measured it, so that a rate reads the same way for every
    client and every suite rather than once per language.
    """
    value = per_second(result)
    return "" if value is None else "%d %s" % (round(value), result.rate)


def _joined(*parts):
    return "; ".join(part for part in parts if part)


def warnings(result):
    """What is worth saying about a measurement besides the number it reports.

    Said here rather than by whoever measured it, so that a case is described the same way
    wherever it is reported and however many languages took the measurement.
    """
    said = []
    if not result.settled:
        said.append(
            "still getting faster when it was measured, so this is an upper bound"
        )
    if result.context:
        return said
    if abs(result.drift) > DRIFT + result.spread:
        said.append(
            "ran %.0f%% %s by the end than at the start, so it was still settling rather "
            "than noisy - measure it again"
            % (100 * abs(result.drift), "slower" if result.drift > 0 else "faster")
        )
    elif result.spread > NOISY_SPREAD:
        said.append(
            "samples spread %.0f%%, too unsteady to draw a conclusion from"
            % (100 * result.spread)
        )
    return said


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
