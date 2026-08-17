"""Compare two benchmark runs.

This is the whole point of the suite: run it, change something, run it again, and read what
moved. Absolute numbers drift between sessions and between machines, so a run is only ever
meaningful against another run taken the same way.

    bazel run //tools/benchmarks:compare -- before.json after.json

A change is only called a result when it is larger than this machine moves on its own, and
larger than a floor below which nothing on any machine is worth reading. A run compared with
itself therefore reports nothing.

How far a machine moves on its own is measured, not guessed at, by running the suite twice
without changing anything and passing both runs as the noise floor:

    bazel run //tools/benchmarks:compare -- before.json after.json --noise a.json b.json

Without one, the spread within each run stands in for it. That is the weaker test of the two,
since the spread describes how far the samples of one run scattered rather than how far the
figure moves when the whole run is taken again, and the two come apart in both directions: a
case whose samples land on a step of their own can read as perfectly steady and still move
between runs, and a case measured across a long tail can read as wild and still land on the
same number every time.
"""

import argparse
import sys

from tools.benchmarks.report import number, read_json
from tools.benchmarks.runner import path

# The smallest change worth calling a result, whatever any run did. A case whose own samples
# agree closely still drifts between runs by more than this, so without a floor the steadiest
# cases would be the ones reported as having moved.
MINIMUM_CHANGE = 0.03


def main():
    parser = argparse.ArgumentParser(description="Compare two benchmark result files.")
    parser.add_argument("before", metavar="BEFORE", help="the run to compare against")
    parser.add_argument("after", metavar="AFTER", help="the run to compare")
    parser.add_argument(
        "--noise",
        metavar="PATH",
        nargs="+",
        default=None,
        help="two or more runs of a build that did not change, to measure this machine's "
        "own movement and use that as the threshold for each case",
    )
    args = parser.parse_args()

    before_path = path(args.before)
    after_path = path(args.after)
    before_title, _, before = read_json(before_path)
    after_title, _, after = read_json(after_path)
    if before_title != after_title:
        print(
            "warning: comparing %s with %s, which measure different things"
            % (before_title, after_title)
        )
    before = {x.key: x for x in before}
    after = {x.key: x for x in after}
    if args.noise is not None and len(args.noise) < 2:
        parser.error("--noise needs at least two runs to measure anything")
    floors = noise_floor(args.noise) if args.noise else None

    for line in compare(after_title, before_path, after_path, before, after, floors):
        print(line)
    return 0


def noise_floor(paths):
    """The most each case moved between runs of a build that did not change.

    Every pair of the runs given is compared, and a case keeps the largest movement any pair
    showed, so that two runs that happened to agree do not vouch for a case that a third
    would have caught moving. Two runs are enough to be worth having and are a rough estimate
    of the floor; more of them make it a better one.
    """
    runs = []
    for name in paths:
        _, _, results = read_json(path(name))
        runs.append({x.key: x for x in results})
    floors = {}
    for index, first in enumerate(runs):
        for second in runs[index + 1 :]:
            for key, result in first.items():
                if key in second and result.value:
                    moved = abs(second[key].value - result.value) / abs(result.value)
                    floors[key] = max(floors.get(key, 0.0), moved)
    return floors


def compare(title, before_path, after_path, before, after, floors=None):
    # pylint: disable=too-many-arguments,too-many-positional-arguments
    lines = [
        "",
        "kRPC benchmarks - %s, before against after" % title,
        "  before: %s" % before_path,
        "  after:  %s" % after_path,
    ]
    blocks = _blocks(before, after)
    named = len({suite for suite, _ in blocks}) > 1
    for suite, scenario in blocks:
        keys = [k for k in after if k[:2] == (suite, scenario) and k in before]
        if keys:
            lines.append("")
            lines.extend(
                _block(
                    _label(suite, scenario, named),
                    [(before[k], after[k]) for k in keys],
                    floors,
                )
            )
    lines.extend(_unmatched("only in the before run", before, after))
    lines.extend(_unmatched("only in the after run", after, before))
    lines.append("")
    if floors is None:
        lines.append(
            "  * marks a change larger than %d%% and larger than the spread either run "
            "showed;" % (100 * MINIMUM_CHANGE)
        )
        lines.append("    everything else is noise")
        lines.append(
            "  pass --noise with two runs of an unchanged build to measure the threshold"
        )
        lines.append("    rather than take the spread as a stand-in for it")
    else:
        lines.append(
            "  * marks a change larger than the floor column, which is the most this case "
            "moved"
        )
        lines.append(
            "    between the runs given to --noise, or %d%% where that is smaller;"
            % (100 * MINIMUM_CHANGE)
        )
        lines.append("    everything else is noise")
        unmeasured = [k for k in after if k in before and k not in floors]
        if unmeasured:
            lines.append("")
            lines.append(
                "  the runs given to --noise did not measure these, so they are held to the"
            )
            lines.append("  %d%% floor alone:" % (100 * MINIMUM_CHANGE))
            for key in unmeasured:
                lines.append("    %s" % after[key].case)
    return lines


def _label(suite, scenario, named):
    """What a block is called. The scenario alone where a run measured one suite, and the suite
    with it where it measured several: a run of every client is five blocks of round trips, and
    which client each one is, is the whole point of reading them."""
    if not named:
        return scenario
    return "%s - %s" % (suite, scenario) if scenario else suite


def _blocks(before, after):
    seen = []
    for key in list(after) + list(before):
        if key[:2] not in seen:
            seen.append(key[:2])
    return seen


def _block(scenario, pairs, floors=None):
    case_width = max([len("case")] + [len(a.case) for _, a in pairs])
    unit_width = max([len("unit")] + [len(a.unit) for _, a in pairs])
    columns = [
        ("case", case_width, False),
        ("before", 10, True),
        ("after", 10, True),
        ("change", 9, True),
    ]
    if floors is not None:
        columns.append(("floor", 7, True))
    columns += [("", 1, False), ("unit", unit_width, False)]

    header = _row(columns)
    lines = []
    if scenario:
        lines.append("  %s" % scenario)
    lines.append(header)
    lines.append("    " + "-" * (len(header) - 4))
    for old, new in pairs:
        floor = _floor(old, new, floors)
        cells = [
            (new.case, case_width, False),
            (number(old.value), 10, True),
            (number(new.value), 10, True),
            (_change(old.value, new.value), 9, True),
        ]
        if floors is not None:
            cells.append(("%.1f%%" % (100 * floor), 7, True))
        cells += [
            ("*" if _is_result(old, new, floor) else "", 1, False),
            (new.unit, unit_width, False),
        ]
        lines.append(_row(cells))
    return lines


def _change(old, new):
    if old == 0:
        return "" if new == 0 else "n/a"
    return "%+.1f%%" % (100 * (new - old) / old)


def _floor(old, new, floors):
    """How large a change this case has to show before it is worth reading.

    Measured movement where there is any, since that is the question being asked; otherwise
    the spread of the two runs, which is the best that can be said without it.
    """
    if floors is None:
        return max(old.spread, new.spread, MINIMUM_CHANGE)
    return max(floors.get(new.key, 0.0), MINIMUM_CHANGE)


def _is_result(old, new, floor):
    """Whether the two runs differ by more than this machine moves on its own, and by enough
    to be worth looking at either way."""
    if old.value == 0:
        return new.value != 0
    change = abs(new.value - old.value) / abs(old.value)
    return change > floor


def _unmatched(title, results, other):
    missing = [results[key] for key in results if key not in other]
    if not missing:
        return []
    lines = ["", "  %s" % title]
    for result in missing:
        lines.append(
            "    %s%s"
            % (result.case, " (%s)" % result.scenario if result.scenario else "")
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


if __name__ == "__main__":
    sys.exit(main())
