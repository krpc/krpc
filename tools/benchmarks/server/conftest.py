"""Reporting for the in-game benchmark scripts.

The scripts record their measurements in ``harness.RESULTS`` as they run; this prints them as a
table once the run finishes, and writes them to a file when ``--json`` is given, for
``compare.py`` to read.
"""

from tools.benchmarks.report import table, write_json
from tools.benchmarks.server.harness import RESULTS, SUITE


def pytest_addoption(parser):
    parser.addoption(
        "--json",
        action="store",
        default=None,
        metavar="PATH",
        help="write the benchmark results to PATH, to compare against another run",
    )


def pytest_terminal_summary(terminalreporter, exitstatus, config):
    # pylint: disable=unused-argument
    if not RESULTS:
        return
    for line in table(RESULTS, SUITE):
        terminalreporter.write_line(line)
    path = config.getoption("--json")
    if path:
        write_json(RESULTS, path, SUITE)
        terminalreporter.write_line("  written to %s" % path)
