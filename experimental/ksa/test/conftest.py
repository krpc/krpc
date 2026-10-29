import os
from pathlib import Path

import ksatest


def pytest_addoption(parser):
    parser.addoption(
        "--no-launch",
        action="store_true",
        default=False,
        help="run against a game started separately, rather than launching one",
    )


def pytest_configure(config):
    ksatest.auto_launch = not config.getoption("--no-launch")


def pytest_report_header():
    # The real path of the install names the game version
    default = ksatest.ROOT.parent.parent.parent / "KSA"
    return f"KSA: {Path(os.environ.get('KSA_DIR', default)).resolve()}"


def pytest_sessionfinish(session):
    ksatest.stop_game()
