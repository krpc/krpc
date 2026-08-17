"""Sizing and repeating the timed loop the benchmark RPCs run.

A benchmark RPC times a loop server side and reports what one iteration of it cost. How long
that loop should be is the client's decision, and the same one wherever the server is running:
long enough to swamp the clock, short enough not to stall the server for a noticeable time.
The whole loop runs inside one server update, so an over-long chunk blocks the game and
perturbs the server's adaptive rate control, which then feeds back into later measurements.
"""

import statistics

from tools.benchmarks.report import Result

# One chunk is one call to a benchmark RPC, sized to run for about this long.
CHUNK_SECONDS = 0.2

# How many chunks to take. The fastest is the estimate and the spread across them says how
# much the machine got in the way, so a handful is enough for both.
REPEATS = 5

# The chunk that sizes the rest. Small, because nothing is known yet about how expensive one
# operation is.
CALIBRATION_ITERATIONS = 200

# A ceiling on chunk size, for cases so cheap that the target time would ask for hundreds of
# millions of iterations.
MAX_ITERATIONS = 5000000

# A floor on chunk size, for the retry below. Shrinking a chunk to dodge a collection costs
# timing accuracy, so there is a point past which it is not worth trading any more away.
MIN_ITERATIONS = 100


def chunks(
    run, chunk_seconds=CHUNK_SECONDS, repeats=REPEATS, max_iterations=MAX_ITERATIONS
):
    """Take ``repeats`` chunks of a case, each sized to run for about ``chunk_seconds``.

    ``run`` takes an iteration count and returns the metrics for one chunk, which is the shape
    every benchmark RPC has.
    """
    metrics = run(CALIBRATION_ITERATIONS)
    per_op = max(metrics["nanoseconds_per_operation"], 0.1)
    iterations = max(int(min(chunk_seconds * 1e9 / per_op, max_iterations)), 1)
    taken = []
    attempts = 0
    while len(taken) < repeats:
        metrics = run(iterations)
        attempts += 1
        if (
            metrics["collections"] > 0
            and not metrics["exact_allocations"]
            and iterations > MIN_ITERATIONS
            and attempts < 3 * repeats
        ):
            # The heap-size delta is meaningless across a collection, since memory was freed
            # as well as allocated. A shorter chunk is less likely to span one.
            iterations = max(iterations // 4, MIN_ITERATIONS)
            taken = []
            continue
        taken.append(metrics)
    return taken


def result(suite, scenario, case, taken, baseline=None, note="", context=False):
    """Turn the chunks of a case into the record the report is built from."""
    # pylint: disable=too-many-arguments,too-many-positional-arguments
    exact = bool(taken[0]["exact_allocations"])
    collections = int(sum(chunk["collections"] for chunk in taken))
    return Result(
        suite,
        scenario,
        case,
        [chunk["nanoseconds_per_operation"] for chunk in taken],
        baseline=baseline,
        bytes_per_op=_bytes_per_op(taken, exact, collections),
        collections=collections,
        exact_allocations=exact,
        iterations=int(taken[0]["iterations"]),
        note=note,
        context=context,
    )


def _bytes_per_op(taken, exact, collections):
    """What one operation allocated, or None where that cannot be answered.

    Without an exact per-thread counter the figure is the change in the size of the heap, and
    a collection inside the window freed memory as well as allocating it. The difference is
    then not an allocation figure at all - over a case that allocates heavily it comes out
    negative - so say nothing rather than say that. A path that cannot run the loop without
    triggering a collection has already answered the question these figures are for.
    """
    if not exact and collections > 0:
        return None
    return statistics.median([chunk["bytes_per_operation"] for chunk in taken])
